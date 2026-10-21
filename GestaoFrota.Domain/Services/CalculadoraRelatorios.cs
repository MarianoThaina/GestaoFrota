using GestaoFrota.Domain.Enums;

namespace GestaoFrota.Domain.Services;

/// <summary>
/// Lançamento mínimo usado pelos relatórios. A data de referência é a data de
/// vencimento (competência), a mesma que a listagem de movimentações filtra.
/// </summary>
public readonly record struct LancamentoRelatorio(
    TipoMovimentacao Tipo,
    StatusMovimentacao Status,
    decimal Valor,
    DateTime DataReferencia,
    string Categoria);

/// <summary>Entradas e saídas de um mês (US12).</summary>
public record FluxoMensal(
    int Ano,
    int Mes,
    decimal Entradas,
    decimal Saidas,
    decimal SaldoDoMes,
    decimal EntradasRealizadas,
    decimal SaidasRealizadas);

/// <summary>Participação de uma categoria no total de despesas do período (US12).</summary>
public record DespesaPorCategoria(string Categoria, decimal Total, decimal Percentual);

/// <summary>Cards do painel de indicadores (US11).</summary>
public record IndicadoresPeriodo(
    decimal TotalEntradas,
    decimal TotalSaidas,
    decimal Resultado,
    decimal AReceberNoMes,
    decimal APagarNoMes,
    decimal EmAtraso,
    string? MaiorDespesaCategoria,
    decimal MaiorDespesaValor,
    decimal? VariacaoSaidasPercentual,
    int QuantidadeLancamentos);

/// <summary>
/// Agregações dos relatórios (US11, US12 e US14). Funções puras, sem acesso a
/// banco, no mesmo espírito da <see cref="CalculadoraFinanceira"/>.
/// Quem chama já deve ter removido as movimentações estornadas.
/// </summary>
public static class CalculadoraRelatorios
{
    /// <summary>
    /// Primeiro dia do período de N meses que termina no mês de <paramref name="hoje"/>.
    /// Ex.: meses = 6 em 20/10/2026 → 01/05/2026.
    /// </summary>
    public static DateTime InicioDoPeriodo(DateTime hoje, int meses)
    {
        meses = Math.Max(1, meses);
        var mesAtual = new DateTime(hoje.Year, hoje.Month, 1);
        return mesAtual.AddMonths(-(meses - 1));
    }

    /// <summary>Primeiro dia do mês seguinte ao de <paramref name="hoje"/> (limite exclusivo).</summary>
    public static DateTime FimDoPeriodo(DateTime hoje)
        => new DateTime(hoje.Year, hoje.Month, 1).AddMonths(1);

    /// <summary>
    /// Entradas e saídas mês a mês. Meses sem lançamento entram zerados para o
    /// gráfico manter a linha do tempo contínua. "Realizadas" = só Pago/Recebido.
    /// </summary>
    public static IReadOnlyList<FluxoMensal> FluxoPorMes(
        IEnumerable<LancamentoRelatorio> lancamentos,
        DateTime hoje,
        int meses)
        => FluxoPorMes(lancamentos, InicioDoPeriodo(hoje, meses), FimDoPeriodo(hoje));

    /// <summary>
    /// Mesmo cálculo com período explícito: <paramref name="inicio"/> inclusivo e
    /// <paramref name="fim"/> exclusivo, em qualquer dia (filtro por mês, ano ou
    /// período livre da US11/US12). Os meses do gráfico vão do mês de início
    /// até o mês que contém o último dia do período.
    /// </summary>
    public static IReadOnlyList<FluxoMensal> FluxoPorMes(
        IEnumerable<LancamentoRelatorio> lancamentos,
        DateTime inicio,
        DateTime fim)
    {
        if (fim <= inicio)
        {
            return Array.Empty<FluxoMensal>();
        }

        var porMes = lancamentos
            .Where(l => l.DataReferencia >= inicio && l.DataReferencia < fim)
            .GroupBy(l => new DateTime(l.DataReferencia.Year, l.DataReferencia.Month, 1))
            .ToDictionary(g => g.Key, g => g.ToList());

        var resultado = new List<FluxoMensal>();
        var primeiroMes = new DateTime(inicio.Year, inicio.Month, 1);

        for (var mes = primeiroMes; mes < fim; mes = mes.AddMonths(1))
        {
            decimal entradas = 0m, saidas = 0m, entradasPagas = 0m, saidasPagas = 0m;

            if (porMes.TryGetValue(mes, out var itens))
            {
                foreach (var item in itens)
                {
                    var pago = item.Status == StatusMovimentacao.Pago;

                    if (item.Tipo == TipoMovimentacao.Receita)
                    {
                        entradas += item.Valor;
                        if (pago) entradasPagas += item.Valor;
                    }
                    else
                    {
                        saidas += item.Valor;
                        if (pago) saidasPagas += item.Valor;
                    }
                }
            }

            resultado.Add(new FluxoMensal(
                mes.Year, mes.Month, entradas, saidas, entradas - saidas, entradasPagas, saidasPagas));
        }

        return resultado;
    }

    /// <summary>
    /// Despesas do período agrupadas por categoria, da maior para a menor, com
    /// o percentual sobre o total de despesas. Lista vazia quando não há despesa.
    /// </summary>
    public static IReadOnlyList<DespesaPorCategoria> DespesasPorCategoria(
        IEnumerable<LancamentoRelatorio> lancamentos,
        DateTime hoje,
        int meses)
        => DespesasPorCategoria(lancamentos, InicioDoPeriodo(hoje, meses), FimDoPeriodo(hoje));

    /// <summary>Mesmo cálculo com período explícito (início inclusivo, fim exclusivo).</summary>
    public static IReadOnlyList<DespesaPorCategoria> DespesasPorCategoria(
        IEnumerable<LancamentoRelatorio> lancamentos,
        DateTime inicio,
        DateTime fim)
    {
        var despesas = lancamentos
            .Where(l => l.Tipo == TipoMovimentacao.Despesa
                        && l.DataReferencia >= inicio
                        && l.DataReferencia < fim)
            .ToList();

        var total = despesas.Sum(d => d.Valor);
        if (total <= 0m)
        {
            return Array.Empty<DespesaPorCategoria>();
        }

        return despesas
            .GroupBy(d => string.IsNullOrWhiteSpace(d.Categoria) ? "Sem categoria" : d.Categoria)
            .Select(g => new DespesaPorCategoria(
                g.Key,
                g.Sum(d => d.Valor),
                Math.Round(g.Sum(d => d.Valor) / total * 100m, 1)))
            .OrderByDescending(c => c.Total)
            .ThenBy(c => c.Categoria)
            .ToList();
    }

    /// <summary>
    /// Indicadores do período e do mês corrente. A variação de saídas compara o
    /// mês atual com o anterior e fica nula quando o mês anterior não teve saída.
    /// "Em atraso" soma os pendentes com vencimento anterior a hoje, de qualquer mês.
    /// </summary>
    public static IndicadoresPeriodo Indicadores(
        IEnumerable<LancamentoRelatorio> lancamentos,
        DateTime hoje,
        int meses)
        => Indicadores(lancamentos, hoje, InicioDoPeriodo(hoje, meses), FimDoPeriodo(hoje));

    /// <summary>
    /// Mesmo cálculo com período explícito. Os totais e a maior despesa seguem
    /// o período; "a receber/a pagar no mês", "em atraso" e a variação de
    /// saídas continuam olhando o mês de <paramref name="hoje"/>, porque são
    /// indicadores do momento, não do filtro.
    /// </summary>
    public static IndicadoresPeriodo Indicadores(
        IEnumerable<LancamentoRelatorio> lancamentos,
        DateTime hoje,
        DateTime inicio,
        DateTime fim)
    {
        var lista = lancamentos.ToList();
        var mesAtual = new DateTime(hoje.Year, hoje.Month, 1);
        var fimMesAtual = mesAtual.AddMonths(1);
        var mesAnterior = mesAtual.AddMonths(-1);

        var periodo = lista.Where(l => l.DataReferencia >= inicio && l.DataReferencia < fim).ToList();

        var totalEntradas = periodo.Where(l => l.Tipo == TipoMovimentacao.Receita).Sum(l => l.Valor);
        var totalSaidas = periodo.Where(l => l.Tipo == TipoMovimentacao.Despesa).Sum(l => l.Valor);

        var noMes = lista.Where(l => l.DataReferencia >= mesAtual && l.DataReferencia < fimMesAtual).ToList();

        var aReceber = noMes
            .Where(l => l.Status == StatusMovimentacao.Pendente && l.Tipo == TipoMovimentacao.Receita)
            .Sum(l => l.Valor);

        var aPagar = noMes
            .Where(l => l.Status == StatusMovimentacao.Pendente && l.Tipo == TipoMovimentacao.Despesa)
            .Sum(l => l.Valor);

        var emAtraso = lista
            .Where(l => l.Status == StatusMovimentacao.Pendente && l.DataReferencia.Date < hoje.Date)
            .Sum(l => l.Valor);

        var maiorDespesa = periodo
            .Where(l => l.Tipo == TipoMovimentacao.Despesa)
            .GroupBy(l => string.IsNullOrWhiteSpace(l.Categoria) ? "Sem categoria" : l.Categoria)
            .Select(g => new { Categoria = g.Key, Total = g.Sum(l => l.Valor) })
            .OrderByDescending(g => g.Total)
            .FirstOrDefault();

        var saidasMesAtual = noMes.Where(l => l.Tipo == TipoMovimentacao.Despesa).Sum(l => l.Valor);
        var saidasMesAnterior = lista
            .Where(l => l.Tipo == TipoMovimentacao.Despesa
                        && l.DataReferencia >= mesAnterior
                        && l.DataReferencia < mesAtual)
            .Sum(l => l.Valor);

        decimal? variacao = saidasMesAnterior > 0m
            ? Math.Round((saidasMesAtual - saidasMesAnterior) / saidasMesAnterior * 100m, 1)
            : null;

        return new IndicadoresPeriodo(
            totalEntradas,
            totalSaidas,
            totalEntradas - totalSaidas,
            aReceber,
            aPagar,
            emAtraso,
            maiorDespesa?.Categoria,
            maiorDespesa?.Total ?? 0m,
            variacao,
            periodo.Count);
    }
}
