using GestaoFrota.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace GestaoFrota.API.Services;

/// <summary>
/// Hash e conferência de senha com o PasswordHasher do ASP.NET Core (PBKDF2
/// com sal aleatório). A senha nunca é gravada nem registrada em log.
/// </summary>
public interface ISenhaService
{
    string GerarHash(Usuario usuario, string senha);

    bool Conferir(Usuario usuario, string senha);

    /// <summary>
    /// Gasta o mesmo tempo de uma conferência quando o e-mail não existe, para o
    /// tempo de resposta não revelar quais e-mails têm conta.
    /// </summary>
    void ConferirSemConta(string senha);
}

public class SenhaService : ISenhaService
{
    private readonly PasswordHasher<Usuario> _hasher = new();
    private readonly string _hashFicticio;

    public SenhaService()
    {
        _hashFicticio = _hasher.HashPassword(new Usuario(), Guid.NewGuid().ToString());
    }

    public string GerarHash(Usuario usuario, string senha) => _hasher.HashPassword(usuario, senha);

    public bool Conferir(Usuario usuario, string senha)
    {
        if (string.IsNullOrEmpty(usuario.SenhaHash))
        {
            ConferirSemConta(senha);
            return false;
        }

        var resultado = _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha);
        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.SenhaHash = _hasher.HashPassword(usuario, senha);
        }

        return resultado != PasswordVerificationResult.Failed;
    }

    public void ConferirSemConta(string senha) =>
        _hasher.VerifyHashedPassword(new Usuario(), _hashFicticio, senha);
}
