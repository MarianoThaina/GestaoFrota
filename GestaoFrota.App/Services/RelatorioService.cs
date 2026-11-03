using System.Net.Http.Json;
using GestaoFrota.App.Models;

namespace GestaoFrota.App.Services;

public interface IRelatorioService
{
    /// <summary>Fluxo mensal, despesas por categoria e indicadores do período filtrado.</summary>
    Task<RelatorioResumo> ObterResumoAsync(FiltroPeriodo filtro);

    /// <summary>CSV das movimentações do período (US14), pronto para compartilhar.</summary>
    Task<ArquivoExportado> ExportarCsvAsync(FiltroPeriodo filtro);

    /// <summary>Estatísticas de rota e veículo e ranking (US10/US13) das viagens do período.</summary>
    Task<EstatisticasFrota> ObterFrotaAsync(FiltroPeriodo filtro);
}

/// <summary>
/// Consome /api/relatorios. Como o saldo e a projeção, os relatórios exigem
/// conexão: agregações são calculadas no servidor, sobre a base completa.
/// Os três endpoints aceitam o mesmo filtro de período (ver FiltroPeriodo).
/// </summary>
public class RelatorioService : IRelatorioService
{
    private readonly HttpClient _httpClient;
    private readonly IConnectivityService _connectivity;

    public RelatorioService(HttpClient httpClient, IConnectivityService connectivity)
    {
        _httpClient = httpClient;
        _connectivity = connectivity;
    }

    public async Task<RelatorioResumo> ObterResumoAsync(FiltroPeriodo filtro)
    {
        ExigirConexao();

        var resposta = await _httpClient.GetAsync($"relatorios/resumo?{filtro.ParaQuery()}");
        await resposta.GarantirSucessoAsync();

        return await resposta.Content.ReadFromJsonAsync<RelatorioResumo>() ?? new RelatorioResumo();
    }

    public async Task<ArquivoExportado> ExportarCsvAsync(FiltroPeriodo filtro)
    {
        ExigirConexao();

        var resposta = await _httpClient.GetAsync($"relatorios/exportar?{filtro.ParaQuery()}");
        await resposta.GarantirSucessoAsync();

        var conteudo = await resposta.Content.ReadAsByteArrayAsync();
        var nome = resposta.Content.Headers.ContentDisposition?.FileNameStar
                   ?? resposta.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                   ?? $"relatorio-financeiro-{DateTime.Now:yyyyMMdd}.csv";
        var tipo = resposta.Content.Headers.ContentType?.MediaType ?? "text/csv";

        return new ArquivoExportado(nome, conteudo, tipo);
    }

    public async Task<EstatisticasFrota> ObterFrotaAsync(FiltroPeriodo filtro)
    {
        ExigirConexao();

        var resposta = await _httpClient.GetAsync($"relatorios/frota?{filtro.ParaQuery()}");
        await resposta.GarantirSucessoAsync();

        return await resposta.Content.ReadFromJsonAsync<EstatisticasFrota>() ?? new EstatisticasFrota();
    }

    private void ExigirConexao()
    {
        if (!_connectivity.EstaOnline())
        {
            throw new InvalidOperationException("Os relatórios exigem conexão com a internet.");
        }
    }
}
