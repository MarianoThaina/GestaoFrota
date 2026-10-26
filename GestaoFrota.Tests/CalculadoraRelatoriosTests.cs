using GestaoFrota.Domain.Enums;
using GestaoFrota.Domain.Services;

namespace GestaoFrota.Tests;

/// <summary>US11 (indicadores) e US12 (fluxo mensal e despesas por categoria).</summary>
public class CalculadoraRelatoriosTests
{
    private static readonly DateTime Hoje = new(2026, 10, 20);

    private static LancamentoRelatorio Receita(decimal valor, string data, bool pago = true, string categoria = "Frete") =>
        new(TipoMovimentacao.Receita, pago ? StatusMovimentacao.Pago : StatusMovimentacao.Pendente, valor, DateTime.Parse(data), categoria);

    private static LancamentoRelatorio Despesa(decimal valor, string data, bool pago = true, string categoria = "Combustível") =>
        new(TipoMovimentacao.Despesa, pago ? StatusMovimentacao.Pago : StatusMovimentacao.Pendente, valor, DateTime.Parse(data), categoria);

    // ---------- Período ----------

    [Theory]
    [InlineData(1, 2026, 10)]
    [InlineData(6, 2026, 5)]
    [InlineData(12, 2025, 11)]
    public void InicioDoPeriodo_ContaNMesesIncluindoOAtual(int meses, int anoEsperado, int mesEsperado)
    {
        var inicio = CalculadoraRelatorios.InicioDoPeriodo(Hoje, meses);

        Assert.Equal(new DateTime(anoEsperado, mesEsperado, 1), inicio);
    }

    [Fact]
    public void InicioDoPeriodo_MesesMenorQueUm_TrataComoUm()
    {
        Assert.Equal(new DateTime(2026, 10, 1), CalculadoraRelatorios.InicioDoPeriodo(Hoje, 0));
        Assert.Equal(new DateTime(2026, 10, 1), CalculadoraRelatorios.InicioDoPeriodo(Hoje, -5));
    }

    [Fact]
    public void FimDoPeriodo_EhOPrimeiroDiaDoMesSeguinte()
    {
        Assert.Equal(new DateTime(2026, 11, 1), CalculadoraRelatorios.FimDoPeriodo(Hoje));
        Assert.Equal(new DateTime(2027, 1, 1), CalculadoraRelatorios.FimDoPeriodo(new DateTime(2026, 12, 31)));
    }

    // ---------- Fluxo por mês ----------

    [Fact]
    public void FluxoPorMes_DevolveExatamenteNMesesComOsVaziosZerados()
    {
        var lancamentos = new[] { Receita(100m, "2026-10-05") };

        var fluxo = CalculadoraRelatorios.FluxoPorMes(lancamentos, Hoje, 6);

        Assert.Equal(6, fluxo.Count);
        Assert.Equal((2026, 5), (fluxo[0].Ano, fluxo[0].Mes));
        Assert.Equal((2026, 10), (fluxo[5].Ano, fluxo[5].Mes));
        Assert.All(fluxo.Take(5), f => Assert.Equal(0m, f.Entradas + f.Saidas));
        Assert.Equal(100m, fluxo[5].Entradas);
    }

    [Fact]
    public void FluxoPorMes_SeparaPrevistoDeRealizado()
    {
        var lancamentos = new[]
        {
            Receita(1000m, "2026-10-05", pago: true),
            Receita(500m, "2026-10-06", pago: false),
            Despesa(200m, "2026-10-07", pago: true),
            Despesa(300m, "2026-10-08", pago: false)
        };

        var outubro = CalculadoraRelatorios.FluxoPorMes(lancamentos, Hoje, 1).Single();

        Assert.Equal(1500m, outubro.Entradas);
        Assert.Equal(500m, outubro.Saidas);
        Assert.Equal(1000m, outubro.SaldoDoMes);
        Assert.Equal(1000m, outubro.EntradasRealizadas);
        Assert.Equal(200m, outubro.SaidasRealizadas);
    }

    [Fact]
    public void FluxoPorMes_IgnoraLancamentosForaDoPeriodo()
    {
        var lancamentos = new[]
        {
            Receita(100m, "2026-04-30"), // antes do período de 6 meses
            Receita(100m, "2026-11-01")  // mês seguinte
        };

        var fluxo = CalculadoraRelatorios.FluxoPorMes(lancamentos, Hoje, 6);

        Assert.Equal(0m, fluxo.Sum(f => f.Entradas));
    }

    // ---------- Despesas por categoria ----------

    [Fact]
    public void DespesasPorCategoria_OrdenaDaMaiorParaAMenorEFecha100PorCento()
    {
        var lancamentos = new[]
        {
            Despesa(300m, "2026-10-01", categoria: "Combustível"),
            Despesa(600m, "2026-09-01", categoria: "Manutenção"),
            Despesa(100m, "2026-08-01", categoria: "Pedágio"),
            Receita(5000m, "2026-10-01")
        };

        var categorias = CalculadoraRelatorios.DespesasPorCategoria(lancamentos, Hoje, 6);

        Assert.Equal(new[] { "Manutenção", "Combustível", "Pedágio" }, categorias.Select(c => c.Categoria));
        Assert.Equal(60m, categorias[0].Percentual);
        Assert.Equal(30m, categorias[1].Percentual);
        Assert.Equal(10m, categorias[2].Percentual);
        Assert.Equal(100m, categorias.Sum(c => c.Percentual));
    }

    [Fact]
    public void DespesasPorCategoria_SemDespesa_DevolveListaVazia()
    {
        var lancamentos = new[] { Receita(100m, "2026-10-01") };

        Assert.Empty(CalculadoraRelatorios.DespesasPorCategoria(lancamentos, Hoje, 6));
    }

    [Fact]
    public void DespesasPorCategoria_CategoriaVaziaViraSemCategoria()
    {
        var lancamentos = new[] { Despesa(50m, "2026-10-01", categoria: "  ") };

        var categorias = CalculadoraRelatorios.DespesasPorCategoria(lancamentos, Hoje, 1);

        Assert.Equal("Sem categoria", categorias.Single().Categoria);
    }

    // ---------- Indicadores ----------

    [Fact]
    public void Indicadores_ResultadoEhEntradasMenosSaidasDoPeriodo()
    {
        var lancamentos = new[]
        {
            Receita(4200m, "2026-10-10"),
            Despesa(350.90m, "2026-10-12"),
            Despesa(999m, "2026-01-01") // fora do período de 6 meses
        };

        var i = CalculadoraRelatorios.Indicadores(lancamentos, Hoje, 6);

        Assert.Equal(4200m, i.TotalEntradas);
        Assert.Equal(350.90m, i.TotalSaidas);
        Assert.Equal(3849.10m, i.Resultado);
        Assert.Equal(2, i.QuantidadeLancamentos);
    }

    [Fact]
    public void Indicadores_AReceberEAPagarConsideramSoPendentesDoMesAtual()
    {
        var lancamentos = new[]
        {
            Receita(1000m, "2026-10-25", pago: false),
            Receita(999m, "2026-10-01", pago: true),     // pago não conta
            Despesa(400m, "2026-10-28", pago: false),
            Despesa(777m, "2026-09-28", pago: false)      // mês anterior não conta
        };

        var i = CalculadoraRelatorios.Indicadores(lancamentos, Hoje, 6);

        Assert.Equal(1000m, i.AReceberNoMes);
        Assert.Equal(400m, i.APagarNoMes);
    }

    [Fact]
    public void Indicadores_EmAtrasoSomaPendentesVencidosDeQualquerMes()
    {
        var lancamentos = new[]
        {
            Despesa(100m, "2026-10-19", pago: false), // ontem: atrasado
            Despesa(200m, "2025-01-10", pago: false), // muito antigo: atrasado
            Despesa(300m, "2026-10-20", pago: false), // hoje: ainda não
            Despesa(400m, "2026-10-21", pago: false), // amanhã: não
            Despesa(500m, "2026-10-01", pago: true)   // pago: não
        };

        var i = CalculadoraRelatorios.Indicadores(lancamentos, Hoje, 6);

        Assert.Equal(300m, i.EmAtraso);
    }

    [Fact]
    public void Indicadores_VariacaoDeSaidasComparaComOMesAnterior()
    {
        var lancamentos = new[]
        {
            Despesa(1000m, "2026-09-10"),
            Despesa(1250m, "2026-10-10")
        };

        var i = CalculadoraRelatorios.Indicadores(lancamentos, Hoje, 6);

        Assert.Equal(25m, i.VariacaoSaidasPercentual);
    }

    [Fact]
    public void Indicadores_VariacaoFicaNulaSemSaidaNoMesAnterior()
    {
        var lancamentos = new[] { Despesa(1250m, "2026-10-10") };

        var i = CalculadoraRelatorios.Indicadores(lancamentos, Hoje, 6);

        Assert.Null(i.VariacaoSaidasPercentual);
    }

    [Fact]
    public void Indicadores_ApontaAMaiorCategoriaDeDespesa()
    {
        var lancamentos = new[]
        {
            Despesa(300m, "2026-10-01", categoria: "Combustível"),
            Despesa(200m, "2026-10-02", categoria: "Combustível"),
            Despesa(450m, "2026-10-03", categoria: "Manutenção")
        };

        var i = CalculadoraRelatorios.Indicadores(lancamentos, Hoje, 6);

        Assert.Equal("Combustível", i.MaiorDespesaCategoria);
        Assert.Equal(500m, i.MaiorDespesaValor);
    }

    [Fact]
    public void Indicadores_SemLancamentos_TudoZeradoESemCategoria()
    {
        var i = CalculadoraRelatorios.Indicadores(Array.Empty<LancamentoRelatorio>(), Hoje, 6);

        Assert.Equal(0m, i.Resultado);
        Assert.Null(i.MaiorDespesaCategoria);
        Assert.Null(i.VariacaoSaidasPercentual);
        Assert.Equal(0, i.QuantidadeLancamentos);
    }
}
