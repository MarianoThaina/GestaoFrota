using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Web;
using GestaoFrota.App.Models;

namespace GestaoFrota.App.Services;

public interface IAuthService
{
    Task<UsuarioLogado> LoginComGoogleAsync();
    Task<UsuarioLogado?> ObterUsuarioLogadoAsync();
    Task<bool> EstaAutenticadoAsync();

    /// <summary>
    /// Valida a sessão salva chamando GET /api/auth/me (Documentação Técnica,
    /// seção 8, item 7) — confirma no backend que o token ainda é válido e
    /// que o usuário continua ativo, sem exigir novo login. Se a API estiver
    /// inacessível (offline), mantém a sessão local (comportamento
    /// offline-first já adotado no restante do app).
    /// </summary>
    Task<bool> ValidarSessaoAsync();

    Task LogoutAsync();
}

/// <summary>
/// Fluxo: abre o navegador do sistema (WebAuthenticator) para o usuário
/// autenticar no Google, obtém o id_token, envia para
/// POST /api/auth/google (backend) e recebe o JWT da própria API,
/// que fica salvo no TokenStore.
/// </summary>
public class GoogleAuthService : IAuthService
{
    private readonly HttpClient _httpClient; // client "público", sem AuthHeaderHandler
    private readonly ITokenStore _tokenStore;

    public GoogleAuthService(HttpClient httpClient, ITokenStore tokenStore)
    {
        _httpClient = httpClient;
        _tokenStore = tokenStore;
    }

    public async Task<UsuarioLogado> LoginComGoogleAsync()
    {
        var authUrl =
            "https://accounts.google.com/o/oauth2/v2/auth" +
            $"?client_id={Uri.EscapeDataString(AppConfig.GoogleClientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(AppConfig.GoogleRedirectUri)}" +
            "&response_type=id_token" +
            "&scope=openid%20email%20profile" +
            $"&nonce={Guid.NewGuid()}";

        var callbackUri = new Uri(AppConfig.GoogleRedirectUri);

        var resultado = await WebAuthenticator.Default.AuthenticateAsync(
            new Uri(authUrl), callbackUri);

        if (!resultado.Properties.TryGetValue("id_token", out var idToken) || string.IsNullOrWhiteSpace(idToken))
        {
            throw new InvalidOperationException("Não foi possível obter o id_token do Google.");
        }

        var response = await _httpClient.PostAsJsonAsync("auth/google", new { idToken });
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new InvalidOperationException("Resposta de autenticação inválida da API.");

        await _tokenStore.SalvarSessaoAsync(auth);

        return new UsuarioLogado
        {
            Id = auth.UsuarioId,
            Nome = auth.Nome,
            Email = auth.Email,
            FotoUrl = auth.FotoUrl,
            Perfil = auth.Perfil
        };
    }

    public Task<UsuarioLogado?> ObterUsuarioLogadoAsync() => _tokenStore.ObterUsuarioAsync();

    public async Task<bool> EstaAutenticadoAsync()
    {
        var token = await _tokenStore.ObterTokenAsync();
        return !string.IsNullOrWhiteSpace(token);
    }

    public async Task<bool> ValidarSessaoAsync()
    {
        var token = await _tokenStore.ObterTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Token expirado ou usuário desativado no backend:
                // encerra a sessão local para forçar novo login.
                await _tokenStore.LimparSessaoAsync();
                return false;
            }

            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (HttpRequestException)
        {
            // API inacessível (ex.: sem conexão no primeiro uso do dia):
            // mantém a sessão local salva em SecureStorage/Preferences.
            return true;
        }
    }

    public Task LogoutAsync() => _tokenStore.LimparSessaoAsync();
}
