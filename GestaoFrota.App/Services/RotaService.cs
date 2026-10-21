using System.Net.Http.Json;
using GestaoFrota.App.Models;

namespace GestaoFrota.App.Services;

public class DadosRota
{
    public string Nome { get; set; } = string.Empty;
    public string Origem { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public decimal DistanciaEstimadaKm { get; set; }
    public bool Ativa { get; set; } = true;
}

public interface IRotaService
{
    /// <summary>Online: atualiza o cache pela API. Offline: devolve o cache.</summary>
    Task<List<Rota>> ListarAsync();

    Task<Rota> CriarAsync(DadosRota dados);

    Task AtualizarAsync(Guid id, DadosRota dados);

    Task DesativarAsync(Guid id);
}

public class RotaService : IRotaService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalDatabaseService _localDb;
    private readonly IConnectivityService _connectivity;

    public RotaService(HttpClient httpClient, ILocalDatabaseService localDb, IConnectivityService connectivity)
    {
        _httpClient = httpClient;
        _localDb = localDb;
        _connectivity = connectivity;
    }

    public async Task<List<Rota>> ListarAsync()
    {
        if (_connectivity.EstaOnline())
        {
            try
            {
                var daApi = await _httpClient.GetFromJsonAsync<List<Rota>>("rotas");
                if (daApi is not null)
                {
                    await _localDb.SubstituirRotasAsync(daApi);
                }
            }
            catch (HttpRequestException)
            {
                // API fora do ar: segue com o cache local.
            }
        }

        return await _localDb.ListarRotasAsync();
    }

    public async Task<Rota> CriarAsync(DadosRota dados)
    {
        ExigirConexao("Cadastrar rotas");

        var resposta = await _httpClient.PostAsJsonAsync("rotas", Corpo(dados));
        await resposta.GarantirSucessoAsync();

        var criada = await resposta.Content.ReadFromJsonAsync<Rota>()
            ?? throw new InvalidOperationException("Resposta inválida ao cadastrar a rota.");

        await _localDb.SalvarRotaLocalAsync(criada);
        return criada;
    }

    public async Task AtualizarAsync(Guid id, DadosRota dados)
    {
        ExigirConexao("Editar rotas");

        var resposta = await _httpClient.PutAsJsonAsync($"rotas/{id}", Corpo(dados));
        await resposta.GarantirSucessoAsync();

        await _localDb.SalvarRotaLocalAsync(new Rota
        {
            Id = id,
            Nome = dados.Nome,
            Origem = dados.Origem,
            Destino = dados.Destino,
            DistanciaEstimadaKm = dados.DistanciaEstimadaKm,
            Ativa = dados.Ativa
        });
    }

    public async Task DesativarAsync(Guid id)
    {
        ExigirConexao("Desativar rotas");

        var resposta = await _httpClient.DeleteAsync($"rotas/{id}");
        await resposta.GarantirSucessoAsync();
    }

    private static object Corpo(DadosRota dados) => new
    {
        nome = dados.Nome,
        origem = dados.Origem,
        destino = dados.Destino,
        distanciaEstimadaKm = dados.DistanciaEstimadaKm,
        ativa = dados.Ativa
    };

    private void ExigirConexao(string acao)
    {
        if (!_connectivity.EstaOnline())
        {
            throw new InvalidOperationException($"{acao} exige conexão com a internet.");
        }
    }
}
