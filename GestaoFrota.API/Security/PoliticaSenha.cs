using System.Net.Mail;

namespace GestaoFrota.API.Security;

/// <summary>
/// Regras da senha e do e-mail das contas criadas pelo Administrador. Fica sem
/// dependências para ser testada direto (GestaoFrota.Tests linka este arquivo).
/// </summary>
public static class PoliticaSenha
{
    public const int TamanhoMinimo = 8;
    public const int TamanhoMaximo = 100;

    /// <summary>Erros de senha seguidos que bloqueiam o login por senha.</summary>
    public const int TentativasAteBloquear = 5;

    /// <summary>Tempo do bloqueio depois de <see cref="TentativasAteBloquear"/> erros.</summary>
    public static readonly TimeSpan TempoBloqueio = TimeSpan.FromMinutes(15);

    /// <summary>Mensagem do problema da senha, ou null quando ela serve.</summary>
    public static string? Validar(string? senha)
    {
        if (string.IsNullOrEmpty(senha) || senha.Length < TamanhoMinimo)
        {
            return $"A senha precisa ter pelo menos {TamanhoMinimo} caracteres.";
        }

        if (senha.Length > TamanhoMaximo)
        {
            return $"A senha pode ter no máximo {TamanhoMaximo} caracteres.";
        }

        if (!senha.Any(char.IsLetter) || !senha.Any(char.IsDigit))
        {
            return "A senha precisa ter letras e números.";
        }

        if (senha.Trim().Length != senha.Length)
        {
            return "A senha não pode começar nem terminar com espaço.";
        }

        return null;
    }

    /// <summary>E-mail em minúsculas e sem espaços, ou null quando o formato não serve.</summary>
    public static string? NormalizarEmail(string? email)
    {
        var limpo = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(limpo) || limpo.Length > 200
            || !MailAddress.TryCreate(limpo, out var endereco)
            || endereco.Address != limpo
            || !limpo.Contains('.', StringComparison.Ordinal))
        {
            return null;
        }

        return limpo;
    }
}
