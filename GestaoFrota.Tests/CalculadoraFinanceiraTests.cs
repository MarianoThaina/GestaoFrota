using GestaoFrota.Domain.Enums;
using GestaoFrota.Domain.Services;

namespace GestaoFrota.Tests;

/// <summary>US04 (parcelas) e US05 (saldo e projeção de caixa).</summary>
public class CalculadoraFinanceiraTests
{
    private static LancamentoFinanceiro Receita(decimal valor, string vencimento) =>
        new(TipoMovimentacao.Receita, valor, DateTime.Parse(vencimento));

    private static LancamentoFinanceiro Despesa(decimal valor, string vencimento) =>
        new(TipoMovimentacao.Despesa, valor, DateTime.Parse(vencimento));

    // ---------- Saldo atual ----------

    [Fact]
    public void SaldoAtual_SemLancamentos_EhZero()
    {
        Assert.Equal(0m, CalculadoraFinanceira.CalcularSaldoAtual(Array.Empty<LancamentoFinanceiro>()));
    }

    [Fact]
    public void SaldoAtual_SomaReceitasESubtraiDespesas()
    {
        var pagos = new[]
        {
            Receita(4200m, "2026-10-10"),
            Despesa(350.90m, "2026-10-12"),
            Despesa(1000m, "2026-09-01")
        };

        Assert.Equal(2849.10m, CalculadoraFinanceira.CalcularSaldoAtual(pagos));
    }

    [Fact]
    public void SaldoAtual_PodeFicarNegativo()
    {
        var pagos = new[] { Receita(100m, "2026-10-01"), Despesa(250m, "2026-10-02") };

        Assert.Equal(-150m, CalculadoraFinanceira.CalcularSaldoAtual(pagos));
    }

    // ---------- Projeção de caixa ----------

    [Fact]
    public void Projecao_SemPendentes_DevolveListaVazia()
    {
        var projecao = CalculadoraFinanceira.ProjetarCaixa(1000m, Array.Empty<LancamentoFinanceiro>(), new DateTime(2026, 10, 20));

        Assert.Empty(projecao);
    }

    [Fact]
    public void Projecao_AcumulaSaldoMesAMesAPartirDoSaldoAtual()
    {
        var pendentes = new[]
        {
            Receita(1000m, "2026-11-05"),
            Despesa(300m, "2026-11-20"),
            Despesa(500m, "2026-12-10")
        };

        var projecao = CalculadoraFinanceira.ProjetarCaixa(2000m, pendentes, new DateTime(2026, 11, 1));

        Assert.Equal(2, projecao.Count);

        Assert.Equal((2026, 11), (projecao[0].Ano, projecao[0].Mes));
        Assert.Equal(1000m, projecao[0].EntradasPrevistas);
        Assert.Equal(300m, projecao[0].SaidasPrevistas);
        Assert.Equal(700m, projecao[0].SaldoDoMes);
        Assert.Equal(2700m, projecao[0].SaldoProjetado);

        Assert.Equal((2026, 12), (projecao[1].Ano, projecao[1].Mes));
        Assert.Equal(-500m, projecao[1].SaldoDoMes);
        Assert.Equal(2200m, projecao[1].SaldoProjetado);
    }

    [Fact]
    public void Projecao_MesesSemLancamentoEntramZeradosParaManterALinhaDoTempo()
    {
        var pendentes = new[] { Despesa(100m, "2026-10-15"), Despesa(100m, "2027-01-15") };

        var projecao = CalculadoraFinanceira.ProjetarCaixa(0m, pendentes, new DateTime(2026, 10, 20));

        Assert.Equal(4, projecao.Count); // out, nov, dez, jan
        Assert.Equal(0m, projecao[1].SaidasPrevistas);
        Assert.Equal(0m, projecao[2].SaidasPrevistas);
        Assert.Equal(-100m, projecao[1].SaldoProjetado);
        Assert.Equal(-200m, projecao[3].SaldoProjetado);
    }

    [Fact]
    public void Projecao_IncluiOMesAtualMesmoQuandoTodosOsPendentesSaoFuturos()
    {
        var pendentes = new[] { Receita(100m, "2027-02-01") };

        var projecao = CalculadoraFinanceira.ProjetarCaixa(50m, pendentes, new DateTime(2026, 12, 3));

        Assert.Equal((2026, 12), (projecao[0].Ano, projecao[0].Mes));
        Assert.Equal(50m, projecao[0].SaldoProjetado);
        Assert.Equal((2027, 2), (projecao[^1].Ano, projecao[^1].Mes));
        Assert.Equal(150m, projecao[^1].SaldoProjetado);
    }

    // ---------- Vencimentos das parcelas (US04) ----------

    [Fact]
    public void Vencimentos_GeraUmaDataPorParcelaIncrementandoOMes()
    {
        var vencimentos = CalculadoraFinanceira.GerarVencimentos(new DateTime(2026, 11, 5), 12);

        Assert.Equal(12, vencimentos.Count);
        Assert.Equal(new DateTime(2026, 11, 5), vencimentos[0]);
        Assert.Equal(new DateTime(2026, 12, 5), vencimentos[1]);
        Assert.Equal(new DateTime(2027, 10, 5), vencimentos[11]);
    }

    [Fact]
    public void Vencimentos_NaoDerivamQuandoOPrimeiroEhDia31()
    {
        // 31/01 → 28/02 → 31/03: soma meses sobre a data original, não sobre a anterior.
        var vencimentos = CalculadoraFinanceira.GerarVencimentos(new DateTime(2026, 1, 31), 3);

        Assert.Equal(new DateTime(2026, 1, 31), vencimentos[0]);
        Assert.Equal(new DateTime(2026, 2, 28), vencimentos[1]);
        Assert.Equal(new DateTime(2026, 3, 31), vencimentos[2]);
    }

    [Fact]
    public void Vencimentos_IgnoraAHoraDoPrimeiroVencimento()
    {
        var vencimentos = CalculadoraFinanceira.GerarVencimentos(new DateTime(2026, 5, 10, 15, 30, 0), 1);

        Assert.Equal(new DateTime(2026, 5, 10), vencimentos[0]);
    }

    [Fact]
    public void Vencimentos_ZeroParcelas_DevolveListaVazia()
    {
        Assert.Empty(CalculadoraFinanceira.GerarVencimentos(new DateTime(2026, 5, 10), 0));
    }
}
