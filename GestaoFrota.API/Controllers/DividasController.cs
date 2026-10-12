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
/// Dívidas e financiamentos (US04). Ao cadastrar uma dívida, gera
/// automaticamente N movimentações pendentes (uma por mês) dentro de uma
/// transação: se a inserção de qualquer parcela falhar, tudo é desfeito.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Perfis.LeituraFinanceira)]
public class DividasController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<DividasController> _logger;

    public DividasController(AppDbContext context, ILogger<DividasController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DividaResponse>>> Listar()
    {
        var dividas = await _context.Dividas.AsNoTracking()
            .OrderByDescending(d => d.DataCriacao)
            .ToListAsync();

        return Ok(dividas.Select(DividaResponse.De));
    }

    /// <summary>Dívida com todas as parcelas geradas, ordenadas por vencimento.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DividaDetalheResponse>> ObterPorId(Guid id)
    {
        var divida = await _context.Dividas.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (divida is null)
        {
            return NotFound(new { mensagem = "Dívida não encontrada." });
        }

        var parcelas = await _context.Movimentacoes.AsNoTracking()
            .Where(m => m.DividaId == id)
            .OrderBy(m => m.DataVencimento)
            .Select(m => new MovimentacaoResponse(
                m.Id, m.Tipo, m.Valor, m.Descricao, m.Status, m.Data, m.DataVencimento, m.DataPagamento,
                m.CategoriaId, m.Categoria!.Nome, m.FormaPagamentoId, m.FormaPagamento!.Nome,
                m.DividaId, m.ComprovanteUrl != null, m.ComprovanteUrl, m.Estornada, m.UsuarioId))
            .ToListAsync();

        return Ok(new DividaDetalheResponse(DividaResponse.De(divida), parcelas));
    }

    [HttpPost]
    [Authorize(Roles = Perfis.GestaoFinanceira)]
    public async Task<ActionResult<DividaDetalheResponse>> Criar([FromBody] DividaRequest request)
    {
        var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(usuarioIdClaim, out var usuarioId))
        {
            return Unauthorized();
        }

        var categoria = await _context.Categorias.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CategoriaId);
        if (categoria is null || !categoria.Ativo || categoria.Tipo != TipoCategoria.Despesa)
        {
            return BadRequest(new { mensagem = "Escolha uma categoria de despesa ativa para as parcelas." });
        }

        var forma = await _context.FormasPagamento.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == request.FormaPagamentoId);
        if (forma is null || !forma.Ativo)
        {
            return BadRequest(new { mensagem = "Forma de pagamento inválida ou desativada." });
        }

        var valorParcela = Math.Round(request.ValorParcela, 2);

        var divida = new Divida
        {
            Descricao = request.Descricao.Trim(),
            ValorParcela = valorParcela,
            NumeroParcelas = request.NumeroParcelas,
            ValorTotal = valorParcela * request.NumeroParcelas,
            ParcelasPagas = 0,
            DataInicio = DateTime.UtcNow.Date,
            DataPrimeiroVencimento = request.DataPrimeiroVencimento!.Value.Date,
            ValorQuitacaoAntecipada = request.ValorQuitacaoAntecipada is { } q ? Math.Round(q, 2) : null,
            Status = StatusDivida.EmAberto
        };

        var vencimentos = CalculadoraFinanceira.GerarVencimentos(divida.DataPrimeiroVencimento, divida.NumeroParcelas);

        // Controle transacional: dívida + todas as parcelas são confirmadas
        // juntas. Qualquer falha desfaz tudo (rollback) para não corromper
        // o histórico financeiro.
        await using var transacao = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Dividas.Add(divida);
            await _context.SaveChangesAsync();

            for (var i = 0; i < vencimentos.Count; i++)
            {
                var parcela = new Movimentacao
                {
                    Tipo = TipoMovimentacao.Despesa,
                    Valor = valorParcela,
                    Descricao = $"{divida.Descricao} - Parcela {i + 1}/{divida.NumeroParcelas}",
                    Status = StatusMovimentacao.Pendente,
                    Data = DateTime.UtcNow,
                    DataVencimento = vencimentos[i],
                    CategoriaId = categoria.Id,
                    FormaPagamentoId = forma.Id,
                    UsuarioId = usuarioId,
                    DividaId = divida.Id,
                    SincronizadoOffline = false
                };

                _context.Movimentacoes.Add(parcela);
                await _context.SaveChangesAsync();
            }

            await transacao.CommitAsync();
        }
        catch (Exception ex)
        {
            await transacao.RollbackAsync();
            _context.ChangeTracker.Clear();

            _logger.LogError(ex, "Falha ao gerar as parcelas da dívida '{Descricao}'. Operação desfeita (rollback).", request.Descricao);

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensagem = "Não foi possível gerar as parcelas. Nenhuma alteração foi salva."
            });
        }

        var detalhe = await ObterPorId(divida.Id);
        var corpo = (detalhe.Result as OkObjectResult)?.Value as DividaDetalheResponse;

        return CreatedAtAction(nameof(ObterPorId), new { id = divida.Id }, corpo);
    }
}
