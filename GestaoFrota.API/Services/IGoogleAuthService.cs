namespace GestaoFrota.API.Services;

public interface IGoogleAuthService
{
    /// <summary>
    /// Valida o id_token emitido pelo Google Sign-In no app mobile.
    /// Lança UnauthorizedAccessException se o token for inválido/expirado.
    /// </summary>
    Task<GoogleUserInfo> ValidarTokenAsync(string idToken);

    /// <summary>
    /// Troca o código de autorização devolvido pelo Google (fluxo do app
    /// Android, que volta por /api/auth/google/retorno) pelo id_token.
    /// Usa o Client Secret, que só existe no servidor.
    /// Lança UnauthorizedAccessException se o Google recusar o código.
    /// </summary>
    Task<string> TrocarCodigoPorIdTokenAsync(string codigo);
}

public record GoogleUserInfo(string GoogleId, string Email, string Nome, string? FotoUrl);
