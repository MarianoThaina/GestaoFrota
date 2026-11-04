using System.Net.Http.Json;
using GestaoFrota.App.Models;

namespace GestaoFrota.App.Services;

public interface IUsuarioService
{
    Task<List<Usuario>> ListarAsync();

    Task AlterarPerfilAsync(
        Guid usuarioId,
        PerfilUsuario perfil);

    Task AlterarStatusAsync(
        Guid usuarioId,
        bool ativo);
}

public class UsuarioService : IUsuarioService
{
    private readonly HttpClient _httpClient;

    public UsuarioService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Usuario>> ListarAsync()
    {
        var usuarios =
            await _httpClient.GetFromJsonAsync<List<Usuario>>("usuarios");

        return usuarios ?? new List<Usuario>();
    }

    public async Task AlterarPerfilAsync(
        Guid usuarioId,
        PerfilUsuario perfil)
    {
        var response = await _httpClient.PutAsJsonAsync(
            $"usuarios/{usuarioId}/perfil",
            new
            {
                perfil
            });

        response.EnsureSuccessStatusCode();
    }

    public async Task AlterarStatusAsync(
        Guid usuarioId,
        bool ativo)
    {
        var response = await _httpClient.PutAsJsonAsync(
            $"usuarios/{usuarioId}/status",
            new
            {
                ativo
            });

        response.EnsureSuccessStatusCode();
    }
}