using GestaoFrota.Domain.Services;

namespace GestaoFrota.Tests;

/// <summary>US08: o hodômetro final deve ser maior que o inicial; a quilometragem rodada é a diferença.</summary>
public class RegrasViagemTests
{
    [Theory]
    [InlineData(184320, 184735)]
    [InlineData(0, 0.5)]
    [InlineData(100, 100.01)]
    public void ValidarEncerramento_AceitaFinalMaiorQueInicial(decimal inicial, decimal final)
    {
        Assert.Null(RegrasViagem.ValidarEncerramento(inicial, final));
    }

    [Theory]
    [InlineData(184320, 184320)] // igual
    [InlineData(184320, 183900)] // menor (o caso da tela do protótipo)
    public void ValidarEncerramento_RecusaFinalMenorOuIgual(decimal inicial, decimal final)
    {
        var erro = RegrasViagem.ValidarEncerramento(inicial, final);

        Assert.NotNull(erro);
        Assert.Contains("maior que o hodômetro inicial", erro);
    }

    [Fact]
    public void ValidarEncerramento_RecusaFinalNegativo()
    {
        var erro = RegrasViagem.ValidarEncerramento(0m, -1m);

        Assert.NotNull(erro);
        Assert.Contains("negativo", erro);
    }

    [Fact]
    public void QuilometragemRodada_EhADiferencaArredondadaEmDuasCasas()
    {
        Assert.Equal(415m, RegrasViagem.QuilometragemRodada(184320m, 184735m));
        Assert.Equal(0.33m, RegrasViagem.QuilometragemRodada(100m, 100.333m));
    }
}
