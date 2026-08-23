namespace GestaoFrota.API.Services;

public interface IGoogleAuthService
{
    /// <summary>
    /// Valida o id_token emitido pelo Google Sign-In no app mobile.
    /// Lança UnauthorizedAccessException se o token for inválido/expirado.
    /// </summary>
    Task<GoogleUserInfo> ValidarTokenAsync(string idToken);
}

public record GoogleUserInfo(string GoogleId, string Email, string Nome, string? FotoUrl);
