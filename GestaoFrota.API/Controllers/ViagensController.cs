using System.Linq.Expressions;
using System.Security.Claims;
using GestaoFrota.API.DTOs;
using GestaoFrota.API.Security;
using GestaoFrota.Domain.Entities;
using GestaoFrota.Domain.Enums;
using GestaoFrota.Domain.Services;
using GestaoFrota.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFrota.API.Controllers;

/// <summary>
/// Viagens e apontamento de hodômetro (US08).
/// - Abrir: Administrador, Gestor de Frota e Motorista (o Motorista só para si).
/// - Encerrar: o motorista da viagem ou um gestor. Exige hodômetro final maior
///   que o inicial; calcula e GRAVA a quilometragem rodada; atualiza o KmAtual
///   do veículo e o devolve a "Disponível".
/// - Listar: o Motorista só enxerga as próprias viagens (US18); o Financeiro
///   consulta para vincular lançamentos (US09).
/// - Offline-first: o app manda o Id gerado no aparelho; se a viagem já existe,
///   a API devolve a existente em vez de duplicar (reenvio idempotente).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Perfis.Todos)]
public class ViagensController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ViagensController> _logger;

    public ViagensController(AppDbContext context, ILogger<ViagensController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private static readonly Expression<Func<Viagem, ViagemResponse>> Projecao = v =>
        new ViagemResponse(
            v.Id,
            v.VeiculoId, v.Veiculo!.Placa, v.Veiculo.Marca + " " + v.Veiculo.Modelo,
            v.RotaId, v.Rota!.Nome, v.Rota.Origem, v.Rota.Destino,
            v.MotoristaId, v.Motorista!.Nome,
            v.DataInicio, v.DataFim,
            v.HodometroInicial, v.HodometroFinal, v.QuilometragemRodadaKm,
            v.Status, v.Observacoes, v.SincronizadoOffline);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ViagemResponse>>> Listar(
        [FromQuery] StatusViagem? status,
        [FromQuery] Guid? veiculoId,
        [FromQuery] Guid? motoristaId,
        [FromQuery] Guid? rotaId,
        [FromQuery] DateTime? de,
        [FromQuery] DateTime? ate,
        [FromQuery] int limite = 200)
    {
        if (!TentarObterUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        limite = Math.Clamp(limite, 1, 1000);

        var query = _context.Viagens.AsNoTracking().AsQueryable();

        // US18: o Motorista só vê as próprias viagens, independentemente do filtro.
        if (EhMotorista())
        {
            query = query.Where(v => v.MotoristaId == usuarioId);
        }
        else if (motoristaId.HasValue)
        {
            query = query.Where(v => v.MotoristaId == motoristaId.Value);
        }

        if (status.HasValue) query = query.Where(v => v.Status == status.Value);
        if (veiculoId.HasValue) query = query.Where(v => v.VeiculoId == veiculoId.Value);
        if (rotaId.HasValue) query = query.Where(v => v.RotaId == rotaId.Value);
        if (de.HasValue) query = query.Where(v => v.DataInicio >= de.Value.Date);
        if (ate.HasValue) query = query.Where(v => v.DataInicio < ate.Value.Date.AddDays(1));

        var viagens = await query
            .OrderByDescending(v => v.Status == StatusViagem.EmAndamento)
            .ThenByDescending(v => v.DataInicio)
            .Take(limite)
            .Select(Projecao)
            .ToListAsync();

        return Ok(viagens);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ViagemResponse>> ObterPorId(Guid id)
    {
        if (!TentarObterUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var viagem = await _context.Viagens.AsNoTracking()
            .Where(v => v.Id == id)
            .Select(Projecao)
            .FirstOrDefaultAsync();

        if (viagem is null)
        {
            return NotFound(new { mensagem = "Viagem não encontrada." });
        }

        if (EhMotorista() && viagem.MotoristaId != usuarioId)
        {
            return Forbid();
        }

        return Ok(viagem);
    }

    /// <summary>
    /// Usuários ativos que podem ser escolhidos como motorista ao abrir uma
    /// viagem em nome de outra pessoa (só gestores precisam disso).
    /// </summary>
    [HttpGet("motoristas")]
    [Authorize(Roles = Perfis.GestaoFrota)]
    public async Task<ActionResult<IEnumerable<MotoristaResponse>>> ListarMotoristas()
    {
        var motoristas = await _context.Usuarios.AsNoTracking()
            .Where(u => u.Ativo)
            .OrderByDescending(u => u.Perfil == PerfilUsuario.OperadorMotorista)
            .ThenBy(u => u.Nome)
            .Select(u => new MotoristaResponse(u.Id, u.Nome, u.Perfil))
            .ToListAsync();

        return Ok(motoristas);
    }

    [HttpPost]
    [Authorize(Roles = Perfis.OperacaoViagens)]
    public async Task<ActionResult<ViagemResponse>> Abrir([FromBody] AbrirViagemRequest request)
    {
        if (!TentarObterUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        // Reenvio da fila offline: a viagem já chegou antes, devolve a existente.
        if (request.Id.HasValue)
        {
            var existente = await _context.Viagens.AsNoTracking()
                .Where(v => v.Id == request.Id.Value)
                .Select(Projecao)
                .FirstOrDefaultAsync();

            if (existente is not null)
            {
                return Ok(existente);
            }
        }

        var motoristaId = request.MotoristaId ?? usuarioId;
        if (EhMotorista() && motoristaId != usuarioId)
        {
            return Forbid();
        }

        var motorista = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == motoristaId);
        if (motorista is null || !motorista.Ativo)
        {
            return BadRequest(new { mensagem = "Motorista inválido ou desativado." });
        }

        var veiculo = await _context.Veiculos.FirstOrDefaultAsync(v => v.Id == request.VeiculoId!.Value);
        if (veiculo is null || !veiculo.Ativo)
        {
            return BadRequest(new { mensagem = "Veículo inválido ou desativado." });
        }

        if (veiculo.Status == StatusVeiculo.EmManutencao)
        {
            return Conflict(new { mensagem = $"O veículo {ValidadorPlaca.Formatar(veiculo.Placa)} está em manutenção." });
        }

        var jaEmViagem = await _context.Viagens
            .AnyAsync(v => v.VeiculoId == veiculo.Id && v.Status == StatusViagem.EmAndamento);
        if (jaEmViagem || veiculo.Status == StatusVeiculo.EmViagem)
        {
            return Conflict(new { mensagem = $"O veículo {ValidadorPlaca.Formatar(veiculo.Placa)} já está em viagem." });
        }

        var rota = await _context.Rotas.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.RotaId!.Value);
        if (rota is null || !rota.Ativa)
        {
            return BadRequest(new { mensagem = "Rota inválida ou desativada." });
        }

        var viagem = new Viagem
        {
            Id = request.Id ?? Guid.NewGuid(),
            VeiculoId = veiculo.Id,
            RotaId = rota.Id,
            MotoristaId = motoristaId,
            DataInicio = request.DataInicio ?? DateTime.UtcNow,
            HodometroInicial = Math.Round(request.HodometroInicial, 2),
            Status = StatusViagem.EmAndamento,
            Observacoes = string.IsNullOrWhiteSpace(request.Observacoes) ? null : request.Observacoes.Trim(),
            SincronizadoOffline = request.CriadaOffline
        };

        // Viagem + status do veículo mudam juntos.
        await using var transacao = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Viagens.Add(viagem);
            veiculo.Status = StatusVeiculo.EmViagem;
            if (viagem.HodometroInicial > veiculo.KmAtual)
            {
                veiculo.KmAtual = viagem.HodometroInicial;
            }

            await _context.SaveChangesAsync();
            await transacao.CommitAsync();
        }
        catch (Exception ex)
        {
            await transacao.RollbackAsync();
            _context.ChangeTracker.Clear();
            _logger.LogError(ex, "Falha ao abrir a viagem {ViagemId}. Operação desfeita.", viagem.Id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensagem = "Não foi possível abrir a viagem. Nenhuma alteração foi salva."
            });
        }

        var response = await _context.Viagens.AsNoTracking()
            .Where(v => v.Id == viagem.Id)
            .Select(Projecao)
            .FirstAsync();

        return CreatedAtAction(nameof(ObterPorId), new { id = viagem.Id }, response);
    }

    /// <summary>
    /// Encerra a viagem: valida o hodômetro final, grava a quilometragem rodada,
    /// atualiza o veículo. Reenvio idempotente: se já está concluída com o mesmo
    /// hodômetro final, responde 200 em vez de 409.
    /// </summary>
    [HttpPatch("{id:guid}/encerrar")]
    [Authorize(Roles = Perfis.OperacaoViagens)]
    public async Task<ActionResult<ViagemResponse>> Encerrar(Guid id, [FromBody] EncerrarViagemRequest request)
    {
        if (!TentarObterUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var viagem = await _context.Viagens.Include(v => v.Veiculo).FirstOrDefaultAsync(v => v.Id == id);
        if (viagem is null)
        {
            return NotFound(new { mensagem = "Viagem não encontrada." });
        }

        if (EhMotorista() && viagem.MotoristaId != usuarioId)
        {
            return Forbid();
        }

        var hodometroFinal = Math.Round(request.HodometroFinal, 2);

        if (viagem.Status == StatusViagem.Concluida)
        {
            if (viagem.HodometroFinal == hodometroFinal)
            {
                return Ok(await ProjetarAsync(viagem.Id));
            }

            return Conflict(new { mensagem = "Esta viagem já foi encerrada." });
        }

        if (viagem.Status == StatusViagem.Cancelada)
        {
            return Conflict(new { mensagem = "Esta viagem foi cancelada e não pode ser encerrada." });
        }

        var erro = RegrasViagem.ValidarEncerramento(viagem.HodometroInicial, hodometroFinal);
        if (erro is not null)
        {
            return BadRequest(new { mensagem = erro });
        }

        var dataFim = request.DataFim ?? DateTime.UtcNow;
        if (dataFim < viagem.DataInicio)
        {
            return BadRequest(new { mensagem = "A data de encerramento não pode ser anterior ao início da viagem." });
        }

        await using var transacao = await _context.Database.BeginTransactionAsync();
        try
        {
            viagem.HodometroFinal = hodometroFinal;
            viagem.QuilometragemRodadaKm = RegrasViagem.QuilometragemRodada(viagem.HodometroInicial, hodometroFinal);
            viagem.DataFim = dataFim;
            viagem.Status = StatusViagem.Concluida;
            if (!string.IsNullOrWhiteSpace(request.Observacoes))
            {
                viagem.Observacoes = request.Observacoes.Trim();
            }

            if (viagem.Veiculo is not null)
            {
                if (hodometroFinal > viagem.Veiculo.KmAtual)
                {
                    viagem.Veiculo.KmAtual = hodometroFinal;
                }

                viagem.Veiculo.Status = await StatusAposLiberarAsync(viagem.Veiculo.Id);
            }

            await _context.SaveChangesAsync();
            await transacao.CommitAsync();
        }
        catch (Exception ex)
        {
            await transacao.RollbackAsync();
            _context.ChangeTracker.Clear();
            _logger.LogError(ex, "Falha ao encerrar a viagem {ViagemId}. Operação desfeita.", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensagem = "Não foi possível encerrar a viagem. Nenhuma alteração foi salva."
            });
        }

        return Ok(await ProjetarAsync(viagem.Id));
    }

    /// <summary>Cancela uma viagem em andamento e libera o veículo. Só gestores.</summary>
    [HttpPatch("{id:guid}/cancelar")]
    [Authorize(Roles = Perfis.GestaoFrota)]
    public async Task<IActionResult> Cancelar(Guid id)
    {
        var viagem = await _context.Viagens.Include(v => v.Veiculo).FirstOrDefaultAsync(v => v.Id == id);
        if (viagem is null)
        {
            return NotFound(new { mensagem = "Viagem não encontrada." });
        }

        if (viagem.Status != StatusViagem.EmAndamento)
        {
            return Conflict(new { mensagem = "Só viagens em andamento podem ser canceladas." });
        }

        viagem.Status = StatusViagem.Cancelada;
        viagem.DataFim = DateTime.UtcNow;

        if (viagem.Veiculo is not null)
        {
            viagem.Veiculo.Status = await StatusAposLiberarAsync(viagem.Veiculo.Id);
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ---------------- Auxiliares ----------------

    private bool TentarObterUsuarioId(out Guid usuarioId)
    {
        var valor = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(valor, out usuarioId);
    }

    private bool EhMotorista() => User.IsInRole(Perfis.OperadorMotorista);

    /// <summary>
    /// Ao liberar o veículo de uma viagem: volta a "Disponível", a menos que
    /// exista manutenção aberta registrada para ele.
    /// </summary>
    private async Task<StatusVeiculo> StatusAposLiberarAsync(Guid veiculoId)
    {
        var manutencaoAberta = await _context.Manutencoes
            .AnyAsync(m => m.VeiculoId == veiculoId && m.DataConclusao == null);

        return manutencaoAberta ? StatusVeiculo.EmManutencao : StatusVeiculo.Disponivel;
    }

    private async Task<ViagemResponse> ProjetarAsync(Guid id) =>
        await _context.Viagens.AsNoTracking()
            .Where(v => v.Id == id)
            .Select(Projecao)
            .FirstAsync();
}
