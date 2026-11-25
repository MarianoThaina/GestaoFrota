using System.Text.Json;
using Google.Apis.Auth;

namespace GestaoFrota.API.Services;

public class GoogleAuthService : IGoogleAuthService
{
    /// <summary>Endereço de retorno padrão, cadastrado no cliente Web do Google Cloud.</summary>
    public const string RedirectUriPadrao = "https://gestaofrota.runasp.net/api/auth/google/retorno";

    private const string EndpointToken = "https://oauth2.googleapis.com/token";

    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public GoogleAuthService(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
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

    public async Task<string> TrocarCodigoPorIdTokenAsync(string codigo)
    {
        var clientId = _configuration["Authentication:Google:ClientId"]
            ?? throw new InvalidOperationException("Authentication:Google:ClientId não configurado.");
        var clientSecret = _configuration["Authentication:Google:ClientSecret"];
        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException(
                "Authentication:Google:ClientSecret não configurado (variável Authentication__Google__ClientSecret).");
        }

        var redirectUri = _configuration["Authentication:Google:RedirectUri"];
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            redirectUri = RedirectUriPadrao;
        }

        using var conteudo = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = codigo,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        });

        using var resposta = await _httpClient.PostAsync(EndpointToken, conteudo);
        if (!resposta.IsSuccessStatusCode)
        {
            throw new UnauthorizedAccessException("O Google recusou o código de autorização.");
        }

        await using var corpo = await resposta.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(corpo);

        if (!json.RootElement.TryGetProperty("id_token", out var idToken) ||
            string.IsNullOrWhiteSpace(idToken.GetString()))
        {
            throw new UnauthorizedAccessException("O Google não devolveu o id_token.");
        }

        return idToken.GetString()!;
    }
}
