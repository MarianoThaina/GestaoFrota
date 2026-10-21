using System.Net.Http.Json;
using GestaoFrota.App.Models;

namespace GestaoFrota.App.Services;

public class DadosVeiculo
{
    public string Placa { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int Ano { get; set; }
    public decimal CapacidadeCargaToneladas { get; set; }
    public decimal KmAtual { get; set; }
    public bool Ativo { get; set; } = true;
}

public interface IVeiculoService
{
    /// <summary>Online: atualiza o cache pela API. Offline: devolve o cache (o motorista precisa escolher o veículo sem rede).</summary>
    Task<List<Veiculo>> ListarAsync();

    Task<Veiculo?> ObterLocalAsync(Guid id);

    Task<Veiculo> CriarAsync(DadosVeiculo dados);

    Task AtualizarAsync(Guid id, DadosVeiculo dados);

    Task DesativarAsync(Guid id);
}

public class VeiculoService : IVeiculoService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalDatabaseService _localDb;
    private readonly IConnectivityService _connectivity;

    public VeiculoService(HttpClient httpClient, ILocalDatabaseService localDb, IConnectivityService connectivity)
    {
        _httpClient = httpClient;
        _localDb = localDb;
        _connectivity = connectivity;
    }

    public async Task<List<Veiculo>> ListarAsync()
    {
        if (_connectivity.EstaOnline())
        {
            try
            {
                var daApi = await _httpClient.GetFromJsonAsync<List<Veiculo>>("veiculos");
                if (daApi is not null)
                {
                    await _localDb.SubstituirVeiculosAsync(daApi);
                }
            }
            catch (HttpRequestException)
            {
                // API fora do ar: segue com o cache local.
            }
        }

        return await _localDb.ListarVeiculosAsync();
    }

    public async Task<Veiculo?> ObterLocalAsync(Guid id)
    {
        var veiculos = await _localDb.ListarVeiculosAsync();
        return veiculos.FirstOrDefault(v => v.Id == id);
    }

    public async Task<Veiculo> CriarAsync(DadosVeiculo dados)
    {
        ExigirConexao("Cadastrar veículos");

        var resposta = await _httpClient.PostAsJsonAsync("veiculos", Corpo(dados));
        await resposta.GarantirSucessoAsync();

        var criado = await resposta.Content.ReadFromJsonAsync<Veiculo>()
            ?? throw new InvalidOperationException("Resposta inválida ao cadastrar o veículo.");

        await _localDb.SalvarVeiculoLocalAsync(criado);
        return criado;
    }

    public async Task AtualizarAsync(Guid id, DadosVeiculo dados)
    {
        ExigirConexao("Editar veículos");

        var resposta = await _httpClient.PutAsJsonAsync($"veiculos/{id}", Corpo(dados));
        await resposta.GarantirSucessoAsync();
    }

    public async Task DesativarAsync(Guid id)
    {
        ExigirConexao("Desativar veículos");

        var resposta = await _httpClient.DeleteAsync($"veiculos/{id}");
        await resposta.GarantirSucessoAsync();
    }

    private static object Corpo(DadosVeiculo dados) => new
    {
        placa = dados.Placa,
        marca = dados.Marca,
        modelo = dados.Modelo,
        ano = dados.Ano,
        capacidadeCargaToneladas = dados.CapacidadeCargaToneladas,
        kmAtual = dados.KmAtual,
        ativo = dados.Ativo
    };

    private void ExigirConexao(string acao)
    {
        if (!_connectivity.EstaOnline())
        {
            throw new InvalidOperationException($"{acao} exige conexão com a internet.");
        }
    }
}
