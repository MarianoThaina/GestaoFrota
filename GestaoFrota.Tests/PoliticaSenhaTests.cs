using GestaoFrota.API.Security;

namespace GestaoFrota.Tests;

/// <summary>Regras de senha e e-mail das contas criadas pelo Administrador.</summary>
public class PoliticaSenhaTests
{
    [Theory]
    [InlineData("frota2026")]
    [InlineData("Caminhao-01")]
    [InlineData("a1b2c3d4")]
    public void Senha_ComOitoOuMaisLetrasENumeros_Serve(string senha)
    {
        Assert.Null(PoliticaSenha.Validar(senha));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc123")]
    public void Senha_Curta_ERecusada(string? senha)
    {
        Assert.Contains("pelo menos 8", PoliticaSenha.Validar(senha));
    }

    [Theory]
    [InlineData("somenteletras")]
    [InlineData("1234567890")]
    public void Senha_SemLetrasOuSemNumeros_ERecusada(string senha)
    {
        Assert.Contains("letras e números", PoliticaSenha.Validar(senha));
    }

    [Fact]
    public void Senha_ComEspacoNasPontas_ERecusada()
    {
        Assert.Contains("espaço", PoliticaSenha.Validar(" frota2026"));
    }

    [Fact]
    public void Senha_LongaDemais_ERecusada()
    {
        Assert.Contains("no máximo", PoliticaSenha.Validar(new string('a', 100) + "1"));
    }

    [Theory]
    [InlineData("  Gestor@GestaoFrota.Demo ", "gestor@gestaofrota.demo")]
    [InlineData("motorista@gmail.com", "motorista@gmail.com")]
    public void Email_Valido_VemNormalizado(string entrada, string esperado)
    {
        Assert.Equal(esperado, PoliticaSenha.NormalizarEmail(entrada));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sem-arroba")]
    [InlineData("nome@semponto")]
    [InlineData("Nome <nome@gmail.com>")]
    public void Email_Invalido_ERecusado(string? entrada)
    {
        Assert.Null(PoliticaSenha.NormalizarEmail(entrada));
    }
}
