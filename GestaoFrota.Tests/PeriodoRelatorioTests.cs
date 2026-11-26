using GestaoFrota.API.Services;

namespace GestaoFrota.Tests;

/// <summary>Filtro "Tudo" dos relatórios (US11/US12).</summary>
public class PeriodoRelatorioTests
{
    private static readonly DateTime Hoje = new(2026, 11, 26);

    private static PeriodoRelatorio Tudo(DateTime? primeiro, DateTime? ultimo) =>
        PeriodoRelatorio.Resolver(Hoje, null, null, null, null, null, true, primeiro, ultimo);

    [Fact]
    public void Tudo_ComHistoricoPassado_VaiDoPrimeiroMesAteOMesAtual()
    {
        var p = Tudo(new DateTime(2026, 3, 10), new DateTime(2026, 11, 5));

        Assert.Equal(new DateTime(2026, 3, 1), p.Inicio);
        Assert.Equal(new DateTime(2026, 12, 1), p.Fim);
    }

    [Fact]
    public void Tudo_SoComLancamentosFuturos_NaoInverteOPeriodo()
    {
        // Financiamento novo: 1ª parcela em dez/2026, última em nov/2027.
        var p = Tudo(new DateTime(2026, 12, 25), new DateTime(2027, 11, 25));

        Assert.Equal(new DateTime(2026, 11, 1), p.Inicio);
        Assert.Equal(new DateTime(2027, 12, 1), p.Fim);
        Assert.True(p.Inicio < p.Fim);
    }

    [Fact]
    public void Tudo_IncluiParcelasFuturasNoFim()
    {
        var p = Tudo(new DateTime(2026, 6, 1), new DateTime(2027, 2, 25));

        Assert.Equal(new DateTime(2026, 6, 1), p.Inicio);
        Assert.Equal(new DateTime(2027, 3, 1), p.Fim);
    }

    [Fact]
    public void Tudo_SemLancamentos_FicaNoMesAtual()
    {
        var p = Tudo(null, null);

        Assert.Equal(new DateTime(2026, 11, 1), p.Inicio);
        Assert.Equal(new DateTime(2026, 12, 1), p.Fim);
    }

    [Fact]
    public void Tudo_SemUltimoRegistro_MantemComportamentoAntigo()
    {
        // A tela de frota ainda chama sem a data final.
        var p = PeriodoRelatorio.Resolver(Hoje, null, null, null, null, null, true, new DateTime(2026, 8, 3));

        Assert.Equal(new DateTime(2026, 8, 1), p.Inicio);
        Assert.Equal(new DateTime(2026, 12, 1), p.Fim);
    }
}
