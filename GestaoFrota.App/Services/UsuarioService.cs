using System.Net.Http.Json;
using GestaoFrota.App.Models;

namespace GestaoFrota.App.Services;

public interface IUsuarioService
{
    /// <summary>Todos os usuários, pendentes de aprovação primeiro.</summary>
    Task<List<Usuario>> ListarAsync();

    Task AlterarPerfilAsync(Guid usuarioId, PerfilUsuario perfil);

    /// <summary>Libera um cadastro "Aguardando Aprovação" (US16), com perfil opcional.</summary>
    Task AprovarAsync(Guid usuarioId, PerfilUsuario? perfil);

    Task AlterarSituacaoAsync(Guid usuarioId, SituacaoUsuario situacao);
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
        var usuarios = await _httpClient.GetFromJsonAsync<List<Usuario>>("usuarios");
        return usuarios ?? new List<Usuario>();
    }

    public async Task AlterarPerfilAsync(Guid usuarioId, PerfilUsuario perfil)
    {
        var resposta = await _httpClient.PutAsJsonAsync($"usuarios/{usuarioId}/perfil", new { perfil });
        await resposta.GarantirSucessoAsync();
    }

    public async Task AprovarAsync(Guid usuarioId, PerfilUsuario? perfil)
    {
        var resposta = await _httpClient.PutAsJsonAsync($"usuarios/{usuarioId}/aprovar", new { perfil });
        await resposta.GarantirSucessoAsync();
    }

    public async Task AlterarSituacaoAsync(Guid usuarioId, SituacaoUsuario situacao)
    {
        var resposta = await _httpClient.PutAsJsonAsync($"usuarios/{usuarioId}/situacao", new { situacao });
        await resposta.GarantirSucessoAsync();
    }
}
