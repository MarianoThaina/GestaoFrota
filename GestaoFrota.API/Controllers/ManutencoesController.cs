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
/// Registro de manutenção (US06-b). Ao salvar, dentro de uma transação:
/// 1) grava a manutenção; 2) coloca o veículo "Em Manutenção";
/// 3) gera automaticamente uma movimentação de SAÍDA na categoria Manutenção.
/// Qualquer falha desfaz tudo. Leitura: perfis financeiros e gestores;
/// escrita: Administrador e Gestor de Frota.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Perfis.LeituraFinanceira)]
public class ManutencoesController : ControllerBase
{
    /// <summary>Id da categoria "Manutenção" semeada pela migration InitialCreate.</summary>
    private static readonly Guid CategoriaManutencaoPadrao = Guid.Parse("00000000-0000-0000-0001-000000000004");

    private readonly AppDbContext _context;
    private readonly ILogger<ManutencoesController> _logger;

    public ManutencoesController(AppDbContext context, ILogger<ManutencoesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private static readonly Expression<Func<Manutencao, ManutencaoResponse>> Projecao = m =>
        new ManutencaoResponse(
            m.Id,
            m.VeiculoId, m.Veiculo!.Placa, m.Veiculo.Marca + " " + m.Veiculo.Modelo,
            m.Data, m.Descricao, m.ValorTotal,
            m.DataConclusao, m.DataConclusao != null,
            m.MovimentacaoId, m.Movimentacao != null ? (StatusMovimentacao?)m.Movimentacao.Status : null);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ManutencaoResponse>>> Listar(
        [FromQuery] Guid? veiculoId,
        [FromQuery] bool? abertas,
        [FromQuery] int limite = 200)
    {
        limite = Math.Clamp(limite, 1, 1000);

        var query = _context.Manutencoes.AsNoTracking().AsQueryable();

        if (veiculoId.HasValue) query = query.Where(m => m.VeiculoId == veiculoId.Value);
        if (abertas == true) query = query.Where(m => m.DataConclusao == null);
        if (abertas == false) query = query.Where(m => m.DataConclusao != null);

        var lista = await query
            .OrderByDescending(m => m.DataConclusao == null)
            .ThenByDescending(m => m.Data)
            .Take(limite)
            .Select(Projecao)
            .ToListAsync();

        return Ok(lista);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ManutencaoResponse>> ObterPorId(Guid id)
    {
        var manutencao = await _context.Manutencoes.AsNoTracking()
            .Where(m => m.Id == id)
            .Select(Projecao)
            .FirstOrDefaultAsync();

        return manutencao is null
            ? NotFound(new { mensagem = "Manutenção não encontrada." })
            : Ok(manutencao);
    }

    [HttpPost]
    [Authorize(Roles = Perfis.GestaoFrota)]
    public async Task<ActionResult<ManutencaoResponse>> Criar([FromBody] ManutencaoRequest request)
    {
        var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(usuarioIdClaim, out var usuarioId))
        {
            return Unauthorized();
        }

        var veiculo = await _context.Veiculos.FirstOrDefaultAsync(v => v.Id == request.VeiculoId!.Value);
        if (veiculo is null || !veiculo.Ativo)
        {
            return BadRequest(new { mensagem = "Veículo inválido ou desativado." });
        }

        if (veiculo.Status == StatusVeiculo.EmViagem)
        {
            return Conflict(new
            {
                mensagem = $"O veículo {ValidadorPlaca.Formatar(veiculo.Placa)} está em viagem. Encerre a viagem antes de registrar a manutenção."
            });
        }

        var categoriaId = request.CategoriaId ?? CategoriaManutencaoPadrao;
        var categoria = await _context.Categorias.AsNoTracking().FirstOrDefaultAsync(c => c.Id == categoriaId);
        if (categoria is null && request.CategoriaId is null)
        {
            // Seed ausente ou apagado: procura pelo nome antes de desistir.
            categoria = await _context.Categorias.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Nome == "Manutenção" && c.Tipo == TipoCategoria.Despesa && c.Ativo);
        }

        if (categoria is null || !categoria.Ativo || categoria.Tipo != TipoCategoria.Despesa)
        {
            return BadRequest(new { mensagem = "Cadastre (ou informe) uma categoria de despesa ativa para a manutenção." });
        }

        var forma = await _context.FormasPagamento.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == request.FormaPagamentoId!.Value);
        if (forma is null || !forma.Ativo)
        {
            return BadRequest(new { mensagem = "Forma de pagamento inválida ou desativada." });
        }

        var data = request.Data!.Value.Date;
        var valor = Math.Round(request.ValorTotal, 2);
        var descricao = request.Descricao.Trim();

        var despesa = new Movimentacao
        {
            Tipo = TipoMovimentacao.Despesa,
            Valor = valor,
            Descricao = $"Manutenção {ValidadorPlaca.Formatar(veiculo.Placa)} - {descricao}",
            Status = request.Pago ? StatusMovimentacao.Pago : StatusMovimentacao.Pendente,
            Data = DateTime.UtcNow,
            DataVencimento = data,
            DataPagamento = request.Pago ? request.DataPagamento!.Value.Date : null,
            CategoriaId = categoria.Id,
            FormaPagamentoId = forma.Id,
            UsuarioId = usuarioId,
            SincronizadoOffline = false
        };

        if (despesa.Descricao.Length > 300)
        {
            despesa.Descricao = despesa.Descricao[..300];
        }

        var manutencao = new Manutencao
        {
            VeiculoId = veiculo.Id,
            Data = data,
            Descricao = descricao,
            ValorTotal = valor,
            MovimentacaoId = despesa.Id
        };

        // Manutenção + despesa + status do veículo: tudo ou nada (US06-b).
        await using var transacao = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Movimentacoes.Add(despesa);
            _context.Manutencoes.Add(manutencao);

            veiculo.Status = StatusVeiculo.EmManutencao;
            veiculo.UltimaManutencao = data;
            veiculo.ObservacoesManutencao = descricao;

            await _context.SaveChangesAsync();
            await transacao.CommitAsync();
        }
        catch (Exception ex)
        {
            await transacao.RollbackAsync();
            _context.ChangeTracker.Clear();

            _logger.LogError(ex, "Falha ao registrar a manutenção do veículo {Placa}. Operação desfeita (rollback).", veiculo.Placa);

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensagem = "Não foi possível registrar a manutenção. Nenhuma alteração foi salva."
            });
        }

        var response = await _context.Manutencoes.AsNoTracking()
            .Where(m => m.Id == manutencao.Id)
            .Select(Projecao)
            .FirstAsync();

        return CreatedAtAction(nameof(ObterPorId), new { id = manutencao.Id }, response);
    }

    /// <summary>Marca a manutenção como concluída e devolve o veículo a "Disponível".</summary>
    [HttpPatch("{id:guid}/concluir")]
    [Authorize(Roles = Perfis.GestaoFrota)]
    public async Task<IActionResult> Concluir(Guid id)
    {
        var manutencao = await _context.Manutencoes.Include(m => m.Veiculo).FirstOrDefaultAsync(m => m.Id == id);
        if (manutencao is null)
        {
            return NotFound(new { mensagem = "Manutenção não encontrada." });
        }

        if (manutencao.DataConclusao.HasValue)
        {
            return Conflict(new { mensagem = "Esta manutenção já foi concluída." });
        }

        manutencao.DataConclusao = DateTime.UtcNow;

        if (manutencao.Veiculo is not null)
        {
            var outraAberta = await _context.Manutencoes
                .AnyAsync(m => m.VeiculoId == manutencao.VeiculoId && m.Id != id && m.DataConclusao == null);

            if (!outraAberta && manutencao.Veiculo.Status == StatusVeiculo.EmManutencao)
            {
                manutencao.Veiculo.Status = StatusVeiculo.Disponivel;
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }
}
