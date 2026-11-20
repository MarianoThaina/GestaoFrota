using Google.Apis.Auth;

namespace GestaoFrota.API.Services;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly IConfiguration _configuration;

    public GoogleAuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<GoogleUserInfo> ValidarTokenAsync(string idToken)
    {
        var clientId = _configuration["Authentication:Google:ClientId"]
            ?? throw new InvalidOperationException("Authentication:Google:ClientId não configurado.");

        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            // O seed de Administrador confia no e-mail; exige que o Google o tenha verificado.
            if (!payload.EmailVerified)
            {
                throw new UnauthorizedAccessException("E-mail da conta Google não verificado.");
            }

            return new GoogleUserInfo(
                GoogleId: payload.Subject,
                Email: payload.Email,
                Nome: payload.Name ?? payload.Email,
                FotoUrl: payload.Picture);
        }
        catch (InvalidJwtException ex)
        {
            throw new UnauthorizedAccessException("Token do Google inválido ou expirado.", ex);
        }
    }
}
