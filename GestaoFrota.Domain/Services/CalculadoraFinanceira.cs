using GestaoFrota.Domain.Enums;

namespace GestaoFrota.Domain.Services;

/// <summary>Valor mínimo de uma movimentação para os cálculos financeiros.</summary>
public readonly record struct LancamentoFinanceiro(
    TipoMovimentacao Tipo,
    decimal Valor,
    DateTime DataVencimento);

/// <summary>Resumo de um mês na projeção de caixa.</summary>
public record ProjecaoMes(
    int Ano,
    int Mes,
    decimal EntradasPrevistas,
    decimal SaidasPrevistas,
    decimal SaldoDoMes,
    decimal SaldoProjetado);

/// <summary>
/// Motor de saldo e projeção de caixa (US05). Funções puras, sem acesso a
/// banco, para serem testáveis de forma isolada.
/// </summary>
public static class CalculadoraFinanceira
{
    /// <summary>
    /// Saldo atual = entradas pagas - saídas pagas. Recebe apenas os
    /// lançamentos com status Pago/Recebido e não estornados.
    /// </summary>
    public static decimal CalcularSaldoAtual(IEnumerable<LancamentoFinanceiro> pagos)
    {
        decimal saldo = 0m;
        foreach (var l in pagos)
        {
            saldo += l.Tipo == TipoMovimentacao.Receita ? l.Valor : -l.Valor;
        }

        return saldo;
    }

    /// <summary>
    /// Projeção: parte do saldo atual e soma, mês a mês (pelo vencimento),
    /// os lançamentos Pendentes. Meses sem lançamentos entram zerados para
    /// manter a linha do tempo contínua; o saldo projetado é acumulado.
    /// </summary>
    public static IReadOnlyList<ProjecaoMes> ProjetarCaixa(
        decimal saldoAtual,
        IEnumerable<LancamentoFinanceiro> pendentes,
        DateTime hoje)
    {
        var lista = pendentes.ToList();
        if (lista.Count == 0)
        {
            return Array.Empty<ProjecaoMes>();
        }

        var porMes = lista
            .GroupBy(l => new DateTime(l.DataVencimento.Year, l.DataVencimento.Month, 1))
            .ToDictionary(g => g.Key, g => g.ToList());

        var inicio = porMes.Keys.Min();
        var fim = porMes.Keys.Max();

        var mesAtual = new DateTime(hoje.Year, hoje.Month, 1);
        if (mesAtual < inicio) inicio = mesAtual;
        if (mesAtual > fim) fim = mesAtual;

        var resultado = new List<ProjecaoMes>();
        var acumulado = saldoAtual;

        for (var mes = inicio; mes <= fim; mes = mes.AddMonths(1))
        {
            decimal entradas = 0m, saidas = 0m;

            if (porMes.TryGetValue(mes, out var itens))
            {
                entradas = itens.Where(i => i.Tipo == TipoMovimentacao.Receita).Sum(i => i.Valor);
                saidas = itens.Where(i => i.Tipo == TipoMovimentacao.Despesa).Sum(i => i.Valor);
            }

            var saldoMes = entradas - saidas;
            acumulado += saldoMes;

            resultado.Add(new ProjecaoMes(mes.Year, mes.Month, entradas, saidas, saldoMes, acumulado));
        }

        return resultado;
    }

    /// <summary>
    /// Gera os vencimentos das N parcelas incrementando os meses a partir
    /// do primeiro vencimento. Sempre soma meses sobre a data original
    /// (e não sobre a anterior) para não "derivar": 31/01 -> 28/02 -> 31/03.
    /// </summary>
    public static IReadOnlyList<DateTime> GerarVencimentos(DateTime primeiroVencimento, int numeroParcelas)
    {
        var vencimentos = new List<DateTime>(numeroParcelas);
        for (var i = 0; i < numeroParcelas; i++)
        {
            vencimentos.Add(primeiroVencimento.Date.AddMonths(i));
        }

        return vencimentos;
    }
}
