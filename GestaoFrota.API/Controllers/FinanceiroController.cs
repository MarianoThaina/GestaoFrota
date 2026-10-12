using GestaoFrota.API.DTOs;
using GestaoFrota.API.Security;
using GestaoFrota.Domain.Enums;
using GestaoFrota.Domain.Services;
using GestaoFrota.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFrota.API.Controllers;

/// <summary>
/// Motor de saldo e projeção de caixa (US05).
/// Saldo atual: entradas - saídas com status estritamente Pago/Recebido.
/// Projeção: saldo atual + lançamentos Pendentes agrupados por mês de vencimento.
/// Movimentações estornadas nunca entram nos cálculos.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Perfis.LeituraFinanceira)]
public class FinanceiroController : ControllerBase
{
    private readonly AppDbContext _context;

    public FinanceiroController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("saldo")]
    public async Task<ActionResult<SaldoResponse>> ObterSaldo()
        => Ok(await CalcularSaldoAsync());

    private async Task<SaldoResponse> CalcularSaldoAsync()
    {
        var totais = await _context.Movimentacoes.AsNoTracking()
            .Where(m => !m.Estornada)
            .GroupBy(m => new { m.Status, m.Tipo })
            .Select(g => new { g.Key.Status, g.Key.Tipo, Total = g.Sum(m => m.Valor) })
            .ToListAsync();

        decimal Soma(StatusMovimentacao status, TipoMovimentacao tipo) =>
            totais.Where(t => t.Status == status && t.Tipo == tipo).Sum(t => t.Total);

        var entradasPagas = Soma(StatusMovimentacao.Pago, TipoMovimentacao.Receita);
        var saidasPagas = Soma(StatusMovimentacao.Pago, TipoMovimentacao.Despesa);

        var saldoAtual = CalculadoraFinanceira.CalcularSaldoAtual(new[]
        {
            new LancamentoFinanceiro(TipoMovimentacao.Receita, entradasPagas, DateTime.UtcNow),
            new LancamentoFinanceiro(TipoMovimentacao.Despesa, saidasPagas, DateTime.UtcNow)
        });

        return new SaldoResponse(
            saldoAtual,
            entradasPagas,
            saidasPagas,
            Soma(StatusMovimentacao.Pendente, TipoMovimentacao.Receita),
            Soma(StatusMovimentacao.Pendente, TipoMovimentacao.Despesa),
            DateTime.UtcNow);
    }

    /// <param name="meses">Quantidade máxima de meses retornados (1 a 60).</param>
    [HttpGet("projecao")]
    public async Task<ActionResult<ProjecaoResponse>> ObterProjecao([FromQuery] int meses = 12)
    {
        meses = Math.Clamp(meses, 1, 60);

        var saldo = (await CalcularSaldoAsync()).SaldoAtual;

        var pendentes = await _context.Movimentacoes.AsNoTracking()
            .Where(m => !m.Estornada && m.Status == StatusMovimentacao.Pendente)
            .Select(m => new { m.Tipo, m.Valor, m.DataVencimento })
            .ToListAsync();

        var lancamentos = pendentes
            .Select(p => new LancamentoFinanceiro(p.Tipo, p.Valor, p.DataVencimento));

        var projecao = CalculadoraFinanceira.ProjetarCaixa(saldo, lancamentos, DateTime.UtcNow)
            .Take(meses)
            .Select(p => new ProjecaoMesResponse(
                p.Ano, p.Mes, $"{p.Mes:00}/{p.Ano}",
                p.EntradasPrevistas, p.SaidasPrevistas, p.SaldoDoMes, p.SaldoProjetado))
            .ToList();

        return Ok(new ProjecaoResponse(saldo, projecao));
    }
}
