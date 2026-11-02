using GestaoFrota.Domain.Services;

namespace GestaoFrota.Tests;

/// <summary>US06: placa no formato antigo (AAA9999) ou Mercosul (AAA9A99), normalizada e única.</summary>
public class ValidadorPlacaTests
{
    [Theory]
    [InlineData("abc-1234", "ABC1234")]
    [InlineData(" ABC 1234 ", "ABC1234")]
    [InlineData("abc1d23", "ABC1D23")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void Normalizar_DeixaMaiusculaSemHifenNemEspaco(string? entrada, string esperado)
    {
        Assert.Equal(esperado, ValidadorPlaca.Normalizar(entrada));
    }

    [Theory]
    [InlineData("ABC1234")]   // antiga
    [InlineData("abc-1234")]  // antiga, minúscula com hífen
    [InlineData("ABC1D23")]   // Mercosul
    [InlineData("rta9f12")]   // Mercosul, minúscula
    public void EhValida_AceitaAntigaEMercosul(string placa)
    {
        Assert.True(ValidadorPlaca.EhValida(placa));
    }

    [Theory]
    [InlineData("ABC123")]     // curta
    [InlineData("ABC12345")]   // longa
    [InlineData("AB12345")]    // só duas letras
    [InlineData("ABCD123")]    // quatro letras
    [InlineData("ABC12D3")]    // letra na posição errada
    [InlineData("1234ABC")]    // invertida
    [InlineData("ABC 1D2")]    // Mercosul curta
    [InlineData("")]
    [InlineData(null)]
    public void EhValida_RecusaFormatosErrados(string? placa)
    {
        Assert.False(ValidadorPlaca.EhValida(placa));
    }

    [Fact]
    public void EhValida_HifensEEspacosExtrasNaoInvalidam()
    {
        // "ABC-12-34" normaliza para "ABC1234": a regra olha só letras e dígitos.
        Assert.True(ValidadorPlaca.EhValida("ABC-12-34"));
        Assert.True(ValidadorPlaca.EhValida("A B C 1 2 3 4"));
    }

    [Fact]
    public void EhMercosul_DistingueOsDoisFormatos()
    {
        Assert.True(ValidadorPlaca.EhMercosul("ABC1D23"));
        Assert.False(ValidadorPlaca.EhMercosul("ABC1234"));
    }

    [Theory]
    [InlineData("ABC1234", "ABC-1234")]
    [InlineData("abc-1234", "ABC-1234")]
    [InlineData("ABC1D23", "ABC1D23")]
    [InlineData("rta9f12", "RTA9F12")]
    [InlineData("XYZ", "XYZ")] // inválida: devolve só normalizada, sem quebrar
    public void Formatar_PoeHifenSoNaAntiga(string entrada, string esperado)
    {
        Assert.Equal(esperado, ValidadorPlaca.Formatar(entrada));
    }

    [Fact]
    public void Normalizar_GaranteQueVariacoesDaMesmaPlacaColidemNoIndiceUnico()
    {
        // "ABC-1234" e "abc1234" precisam virar a mesma string no banco.
        Assert.Equal(ValidadorPlaca.Normalizar("ABC-1234"), ValidadorPlaca.Normalizar("abc1234"));
    }
}
