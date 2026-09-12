using System.Net.Http.Json;
using GestaoFrota.App.Models;

namespace GestaoFrota.App.Services;

public interface ICategoriaService
{
    /// <summary>
    /// Retorna sempre do cache local imediatamente; se online, atualiza
    /// o cache em segundo plano com os dados mais recentes da API.
    /// </summary>
    Task<List<Categoria>> ListarAsync();

    Task<Categoria> CriarAsync(string nome, TipoCategoria tipo);

    /// <summary>Chama PUT /categorias/{id} — endpoint já existente na API.</summary>
    Task<Categoria> AtualizarAsync(Guid id, string nome, TipoCategoria tipo, bool ativo);

    /// <summary>
    /// Soft delete: chama DELETE /categorias/{id}, que marca Ativo=false
    /// no servidor em vez de remover o registro (preserva o histórico
    /// financeiro que aponta para essa categoria).
    /// </summary>
    Task DesativarAsync(Guid id);
}

public class CategoriaService : ICategoriaService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalDatabaseService _localDb;
    private readonly IConnectivityService _connectivity;

    public CategoriaService(HttpClient httpClient, ILocalDatabaseService localDb, IConnectivityService connectivity)
    {
        _httpClient = httpClient;
        _localDb = localDb;
        _connectivity = connectivity;
    }

    public async Task<List<Categoria>> ListarAsync()
    {
        if (_connectivity.EstaOnline())
        {
            try
            {
                var categoriasApi = await _httpClient.GetFromJsonAsync<List<Categoria>>("categorias");
                if (categoriasApi is not null)
                {
                    await _localDb.SalvarCategoriasAsync(categoriasApi);
                }
            }
            catch (HttpRequestException)
            {
                // Sem conexão real ou API fora do ar: segue com o cache local.
            }
        }

        return await _localDb.ListarCategoriasAsync();
    }

    public async Task<Categoria> CriarAsync(string nome, TipoCategoria tipo)
    {
        if (_connectivity.EstaOnline())
        {
            var response = await _httpClient.PostAsJsonAsync("categorias", new { nome, tipo, ativo = true });
            response.EnsureSuccessStatusCode();

            var categoriaApi = await response.Content.ReadFromJsonAsync<Categoria>()
                ?? throw new InvalidOperationException("Resposta inválida ao criar categoria.");

            await _localDb.SalvarCategoriaLocalAsync(categoriaApi);
            return categoriaApi;
        }

        // Offline: salva localmente marcada como pendente; sincronizada
        // quando a conexão voltar (ver rotina de sincronização em background).
        var categoriaLocal = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Tipo = tipo,
            Ativo = true,
            PendenteSincronizacao = true
        };

        await _localDb.SalvarCategoriaLocalAsync(categoriaLocal);
        return categoriaLocal;
    }

    public async Task<Categoria> AtualizarAsync(Guid id, string nome, TipoCategoria tipo, bool ativo)
    {
        // Editar exige conexão: sem endpoint de fila de edição offline
        // implementado ainda, então falha explicitamente se estiver offline
        // em vez de fingir que salvou.
        if (!_connectivity.EstaOnline())
        {
            throw new InvalidOperationException("Editar categorias exige conexão com a internet.");
        }

        var response = await _httpClient.PutAsJsonAsync($"categorias/{id}", new { nome, tipo, ativo });
        response.EnsureSuccessStatusCode();

        var categoriaAtualizada = new Categoria { Id = id, Nome = nome, Tipo = tipo, Ativo = ativo };
        await _localDb.SalvarCategoriaLocalAsync(categoriaAtualizada);
        return categoriaAtualizada;
    }

    public async Task DesativarAsync(Guid id)
    {
        if (!_connectivity.EstaOnline())
        {
            throw new InvalidOperationException("Desativar categorias exige conexão com a internet.");
        }

        var response = await _httpClient.DeleteAsync($"categorias/{id}");
        response.EnsureSuccessStatusCode();

        // Reflete o soft delete no cache local (Ativo=false), sem apagar
        // a linha, espelhando o que a API faz.
        var categorias = await _localDb.ListarCategoriasAsync();
        var categoria = categorias.FirstOrDefault(c => c.Id == id);
        if (categoria is not null)
        {
            categoria.Ativo = false;
            await _localDb.SalvarCategoriaLocalAsync(categoria);
        }
    }
}
