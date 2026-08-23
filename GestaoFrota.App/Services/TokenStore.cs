using System.Text.Json;
using GestaoFrota.App.Models;

namespace GestaoFrota.App.Services;

public interface ITokenStore
{
    Task SalvarSessaoAsync(AuthResponse auth);
    Task<string?> ObterTokenAsync();
    Task<UsuarioLogado?> ObterUsuarioAsync();
    Task LimparSessaoAsync();
}

/// <summary>
/// Usa SecureStorage (Keychain no iOS / Keystore no Android) para o token,
/// que é sensível, e Preferences para os dados de perfil (não sensíveis),
/// evitando releituras de SecureStorage a cada verificação de RBAC.
/// </summary>
public class TokenStore : ITokenStore
{
    private const string ChaveToken = "gestaofrota_jwt";
    private const string ChaveUsuario = "gestaofrota_usuario";

    public async Task SalvarSessaoAsync(AuthResponse auth)
    {
        await SecureStorage.Default.SetAsync(ChaveToken, auth.Token);

        var usuario = new UsuarioLogado
        {
            Id = auth.UsuarioId,
            Nome = auth.Nome,
            Email = auth.Email,
            FotoUrl = auth.FotoUrl,
            Perfil = auth.Perfil
        };

        Preferences.Default.Set(ChaveUsuario, JsonSerializer.Serialize(usuario));
    }

    public Task<string?> ObterTokenAsync() => SecureStorage.Default.GetAsync(ChaveToken);

    public Task<UsuarioLogado?> ObterUsuarioAsync()
    {
        var json = Preferences.Default.Get<string?>(ChaveUsuario, null);
        var usuario = json is null ? null : JsonSerializer.Deserialize<UsuarioLogado>(json);
        return Task.FromResult(usuario);
    }

    public Task LimparSessaoAsync()
    {
        SecureStorage.Default.Remove(ChaveToken);
        Preferences.Default.Remove(ChaveUsuario);
        return Task.CompletedTask;
    }
}
