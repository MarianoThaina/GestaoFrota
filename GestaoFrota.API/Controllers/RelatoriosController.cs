using System.Globalization;
using System.Text;
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
/// Relatórios financeiros (US11 painel de indicadores, US12 gráficos de fluxo
/// de caixa e US14 exportação). O período é "os últimos N meses, incluindo o
/// atual", agrupado pela data de vencimento, como a listagem de movimentações.
/// Movimentações estornadas nunca entram. Leitura: Administrador, Gestor de
/// Frota e Financeiro; o Motorista recebe 403.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Perfis.LeituraFinanceira)]
public class RelatoriosController : ControllerBase
{
    private const int MesesPadrao = 6;
    private const int MesesMaximo = 36;

    private static readonly CultureInfo PtBr = new("pt-BR");

    private readonly AppDbContext _context;

    public RelatoriosController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Fluxo mensal, despesas por categoria e indicadores em uma única resposta.</summary>
    /// <param name="meses">Quantidade de meses do período, incluindo o atual (1 a 36).</param>
    [HttpGet("resumo")]
    public async Task<ActionResult<RelatorioResumoResponse>> ObterResumo([FromQuery] int meses = MesesPadrao)
    {
        meses = Math.Clamp(meses, 1, MesesMaximo);
        var hoje = DateTime.UtcNow;
        var inicio = CalculadoraRelatorios.InicioDoPeriodo(hoje, meses);
        var fim = CalculadoraRelatorios.FimDoPeriodo(hoje);
        var inicioComMesAnterior = inicio.AddMonths(-1);

        // Carrega também o mês anterior ao período (variação de saídas) e os
        // pendentes vencidos de qualquer data (indicador "em atraso").
        var brutos = await _context.Movimentacoes.AsNoTracking()
            .Where(m => !m.Estornada
                        && ((m.DataVencimento >= inicioComMesAnterior && m.DataVencimento < fim)
                            || (m.Status == StatusMovimentacao.Pendente && m.DataVencimento < hoje)))
            .Select(m => new { m.Tipo, m.Status, m.Valor, m.DataVencimento, Categoria = m.Categoria!.Nome })
            .ToListAsync();

        var lancamentos = brutos
            .Select(m => new LancamentoRelatorio(m.Tipo, m.Status, m.Valor, m.DataVencimento, m.Categoria))
            .ToList();

        var fluxo = CalculadoraRelatorios.FluxoPorMes(lancamentos, hoje, meses)
            .Select(f => new FluxoMensalResponse(
                f.Ano, f.Mes, $"{f.Mes:00}/{f.Ano}",
                f.Entradas, f.Saidas, f.SaldoDoMes, f.EntradasRealizadas, f.SaidasRealizadas))
            .ToList();

        var categorias = CalculadoraRelatorios.DespesasPorCategoria(lancamentos, hoje, meses)
            .Select(c => new DespesaCategoriaResponse(c.Categoria, c.Total, c.Percentual))
            .ToList();

        var i = CalculadoraRelatorios.Indicadores(lancamentos, hoje, meses);
        var indicadores = new IndicadoresResponse(
            i.TotalEntradas, i.TotalSaidas, i.Resultado, i.AReceberNoMes, i.APagarNoMes, i.EmAtraso,
            i.MaiorDespesaCategoria, i.MaiorDespesaValor, i.VariacaoSaidasPercentual, i.QuantidadeLancamentos);

        return Ok(new RelatorioResumoResponse(
            meses, inicio, fim.AddDays(-1), indicadores, fluxo, categorias, hoje));
    }

    /// <summary>
    /// Exporta as movimentações do período em CSV (US14). Separador ";" e BOM
    /// UTF-8 para o Excel em português abrir sem configuração.
    /// </summary>
    [HttpGet("exportar")]
    public async Task<IActionResult> Exportar([FromQuery] int meses = MesesPadrao)
    {
        meses = Math.Clamp(meses, 1, MesesMaximo);
        var hoje = DateTime.UtcNow;
        var inicio = CalculadoraRelatorios.InicioDoPeriodo(hoje, meses);
        var fim = CalculadoraRelatorios.FimDoPeriodo(hoje);

        var linhas = await _context.Movimentacoes.AsNoTracking()
            .Where(m => !m.Estornada && m.DataVencimento >= inicio && m.DataVencimento < fim)
            .OrderBy(m => m.DataVencimento)
            .ThenBy(m => m.DataCriacao)
            .Select(m => new
            {
                m.DataVencimento,
                m.Tipo,
                m.Descricao,
                Categoria = m.Categoria!.Nome,
                FormaPagamento = m.FormaPagamento!.Nome,
                m.Status,
                m.DataPagamento,
                m.Valor
            })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Vencimento;Tipo;Descricao;Categoria;Forma de pagamento;Situacao;Pagamento;Valor");

        foreach (var l in linhas)
        {
            sb.Append(l.DataVencimento.ToString("dd/MM/yyyy", PtBr)).Append(';')
              .Append(l.Tipo == TipoMovimentacao.Receita ? "Receita" : "Despesa").Append(';')
              .Append(Csv(l.Descricao)).Append(';')
              .Append(Csv(l.Categoria)).Append(';')
              .Append(Csv(l.FormaPagamento)).Append(';')
              .Append(l.Status == StatusMovimentacao.Pago ? "Pago/Recebido" : "Pendente").Append(';')
              .Append(l.DataPagamento.HasValue ? l.DataPagamento.Value.ToString("dd/MM/yyyy", PtBr) : string.Empty).Append(';')
              .Append(l.Valor.ToString("F2", PtBr))
              .AppendLine();
        }

        var totalEntradas = linhas.Where(l => l.Tipo == TipoMovimentacao.Receita).Sum(l => l.Valor);
        var totalSaidas = linhas.Where(l => l.Tipo == TipoMovimentacao.Despesa).Sum(l => l.Valor);
        sb.AppendLine();
        sb.Append("Total de entradas;;;;;;;").Append(totalEntradas.ToString("F2", PtBr)).AppendLine();
        sb.Append("Total de saidas;;;;;;;").Append(totalSaidas.ToString("F2", PtBr)).AppendLine();
        sb.Append("Resultado;;;;;;;").Append((totalEntradas - totalSaidas).ToString("F2", PtBr)).AppendLine();

        // BOM UTF-8: sem ele o Excel (Windows, pt-BR) troca os acentos por símbolos.
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var nomeArquivo = $"relatorio-financeiro-{inicio:yyyyMM}-{fim.AddDays(-1):yyyyMM}.csv";

        return File(bytes, "text/csv; charset=utf-8", nomeArquivo);
    }

    /// <summary>Protege o campo contra ";" e aspas, que quebrariam a coluna.</summary>
    private static string Csv(string? valor)
    {
        if (string.IsNullOrEmpty(valor))
        {
            return string.Empty;
        }

        var precisaAspas = valor.Contains(';') || valor.Contains('"') || valor.Contains('\n');
        var texto = valor.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ");
        return precisaAspas ? $"\"{texto}\"" : texto;
    }
}
