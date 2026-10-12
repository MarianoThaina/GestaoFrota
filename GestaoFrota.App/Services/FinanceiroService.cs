using System.Net.Http.Json;
using GestaoFrota.App.Models;

namespace GestaoFrota.App.Services;

public interface IFinanceiroService
{
    /// <summary>Saldo atual: entradas - saídas com status Pago/Recebido.</summary>
    Task<SaldoResumo> ObterSaldoAsync();

    /// <summary>Saldo atual + pendentes agrupados por mês de vencimento.</summary>
    Task<ProjecaoResposta> ObterProjecaoAsync(int meses = 12);
}

public class FinanceiroService : IFinanceiroService
{
    private readonly HttpClient _httpClient;
    private readonly IConnectivityService _connectivity;

    public FinanceiroService(HttpClient httpClient, IConnectivityService connectivity)
    {
        _httpClient = httpClient;
        _connectivity = connectivity;
    }

    public async Task<SaldoResumo> ObterSaldoAsync()
    {
        ExigirConexao();

        var resposta = await _httpClient.GetAsync("financeiro/saldo");
        await resposta.GarantirSucessoAsync();

        return await resposta.Content.ReadFromJsonAsync<SaldoResumo>() ?? new SaldoResumo();
    }

    public async Task<ProjecaoResposta> ObterProjecaoAsync(int meses = 12)
    {
        ExigirConexao();

        var resposta = await _httpClient.GetAsync($"financeiro/projecao?meses={meses}");
        await resposta.GarantirSucessoAsync();

        return await resposta.Content.ReadFromJsonAsync<ProjecaoResposta>() ?? new ProjecaoResposta();
    }

    private void ExigirConexao()
    {
        if (!_connectivity.EstaOnline())
        {
            throw new InvalidOperationException("Saldo e projeção exigem conexão com a internet.");
        }
    }
}
