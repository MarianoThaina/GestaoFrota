using System.Net.Http.Json;
using GestaoFrota.App.Models;

namespace GestaoFrota.App.Services;

public interface IFormaPagamentoService
{
    Task<List<FormaPagamento>> ListarAsync();
    Task<FormaPagamento> CriarAsync(string nome);
}

public class FormaPagamentoService : IFormaPagamentoService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalDatabaseService _localDb;
    private readonly IConnectivityService _connectivity;

    public FormaPagamentoService(HttpClient httpClient, ILocalDatabaseService localDb, IConnectivityService connectivity)
    {
        _httpClient = httpClient;
        _localDb = localDb;
        _connectivity = connectivity;
    }

    public async Task<List<FormaPagamento>> ListarAsync()
    {
        if (_connectivity.EstaOnline())
        {
            try
            {
                var formasApi = await _httpClient.GetFromJsonAsync<List<FormaPagamento>>("formaspagamento");
                if (formasApi is not null)
                {
                    await _localDb.SalvarFormasPagamentoAsync(formasApi);
                }
            }
            catch (HttpRequestException)
            {
                // Segue com o cache local.
            }
        }

        return await _localDb.ListarFormasPagamentoAsync();
    }

    public async Task<FormaPagamento> CriarAsync(string nome)
    {
        if (_connectivity.EstaOnline())
        {
            var response = await _httpClient.PostAsJsonAsync("formaspagamento", new { nome, ativo = true });
            response.EnsureSuccessStatusCode();

            var formaApi = await response.Content.ReadFromJsonAsync<FormaPagamento>()
                ?? throw new InvalidOperationException("Resposta inválida ao criar forma de pagamento.");

            await _localDb.SalvarFormaPagamentoLocalAsync(formaApi);
            return formaApi;
        }

        var formaLocal = new FormaPagamento
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Ativo = true,
            PendenteSincronizacao = true
        };

        await _localDb.SalvarFormaPagamentoLocalAsync(formaLocal);
        return formaLocal;
    }
}
