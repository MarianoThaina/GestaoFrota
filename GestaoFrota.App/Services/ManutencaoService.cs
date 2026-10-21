using System.Net.Http.Json;
using GestaoFrota.App.Models;

namespace GestaoFrota.App.Services;

public class NovaManutencao
{
    public Guid VeiculoId { get; set; }
    public DateTime Data { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal ValorTotal { get; set; }
    public Guid FormaPagamentoId { get; set; }
    public bool Pago { get; set; }
    public DateTime? DataPagamento { get; set; }
}

public interface IManutencaoService
{
    Task<List<Manutencao>> ListarAsync(Guid? veiculoId = null, bool? abertas = null);

    /// <summary>Registra a manutenção; a API gera a despesa e coloca o veículo em manutenção.</summary>
    Task<Manutencao> CriarAsync(NovaManutencao dados);

    Task ConcluirAsync(Guid id);
}

public class ManutencaoService : IManutencaoService
{
    private readonly HttpClient _httpClient;
    private readonly IConnectivityService _connectivity;

    public ManutencaoService(HttpClient httpClient, IConnectivityService connectivity)
    {
        _httpClient = httpClient;
        _connectivity = connectivity;
    }

    public async Task<List<Manutencao>> ListarAsync(Guid? veiculoId = null, bool? abertas = null)
    {
        ExigirConexao("Consultar manutenções");

        var filtros = new List<string>();
        if (veiculoId.HasValue) filtros.Add($"veiculoId={veiculoId.Value}");
        if (abertas.HasValue) filtros.Add($"abertas={(abertas.Value ? "true" : "false")}");

        var url = filtros.Count == 0 ? "manutencoes" : $"manutencoes?{string.Join("&", filtros)}";
        return await _httpClient.GetFromJsonAsync<List<Manutencao>>(url) ?? new List<Manutencao>();
    }

    public async Task<Manutencao> CriarAsync(NovaManutencao dados)
    {
        ExigirConexao("Registrar manutenções");

        var resposta = await _httpClient.PostAsJsonAsync("manutencoes", new
        {
            veiculoId = dados.VeiculoId,
            data = dados.Data.Date,
            descricao = dados.Descricao,
            valorTotal = dados.ValorTotal,
            formaPagamentoId = dados.FormaPagamentoId,
            pago = dados.Pago,
            dataPagamento = dados.Pago ? dados.DataPagamento?.Date : null
        });

        await resposta.GarantirSucessoAsync();

        return await resposta.Content.ReadFromJsonAsync<Manutencao>()
            ?? throw new InvalidOperationException("Resposta inválida ao registrar a manutenção.");
    }

    public async Task ConcluirAsync(Guid id)
    {
        ExigirConexao("Concluir manutenções");

        using var requisicao = new HttpRequestMessage(HttpMethod.Patch, $"manutencoes/{id}/concluir");
        var resposta = await _httpClient.SendAsync(requisicao);
        await resposta.GarantirSucessoAsync();
    }

    private void ExigirConexao(string acao)
    {
        // A manutenção gera despesa no caixa: sem fila offline para operações
        // financeiras, falha de forma explícita em vez de fingir que salvou.
        if (!_connectivity.EstaOnline())
        {
            throw new InvalidOperationException($"{acao} exige conexão com a internet.");
        }
    }
}
