using System.Net.Http.Json;
using GestaoFrota.App.Models;

namespace GestaoFrota.App.Services;

public class NovaDivida
{
    public string Descricao { get; set; } = string.Empty;
    public decimal ValorParcela { get; set; }
    public int NumeroParcelas { get; set; }
    public DateTime DataPrimeiroVencimento { get; set; }
    public decimal? ValorQuitacaoAntecipada { get; set; }
    public Guid CategoriaId { get; set; }
    public Guid FormaPagamentoId { get; set; }
}

public interface IDividaService
{
    Task<List<Divida>> ListarAsync();

    Task<DividaDetalhe> ObterAsync(Guid id);

    /// <summary>
    /// POST /dividas: a API grava a dívida e gera todas as parcelas pendentes
    /// em uma única transação (rollback se qualquer parcela falhar).
    /// </summary>
    Task<DividaDetalhe> CriarAsync(NovaDivida dados);
}

public class DividaService : IDividaService
{
    private readonly HttpClient _httpClient;
    private readonly IConnectivityService _connectivity;

    public DividaService(HttpClient httpClient, IConnectivityService connectivity)
    {
        _httpClient = httpClient;
        _connectivity = connectivity;
    }

    public async Task<List<Divida>> ListarAsync()
    {
        ExigirConexao("Consultar dívidas");

        var resposta = await _httpClient.GetAsync("dividas");
        await resposta.GarantirSucessoAsync();

        return await resposta.Content.ReadFromJsonAsync<List<Divida>>() ?? new List<Divida>();
    }

    public async Task<DividaDetalhe> ObterAsync(Guid id)
    {
        ExigirConexao("Consultar dívidas");

        var resposta = await _httpClient.GetAsync($"dividas/{id}");
        await resposta.GarantirSucessoAsync();

        return await resposta.Content.ReadFromJsonAsync<DividaDetalhe>()
            ?? throw new InvalidOperationException("Resposta inválida ao carregar a dívida.");
    }

    public async Task<DividaDetalhe> CriarAsync(NovaDivida dados)
    {
        ExigirConexao("Cadastrar dívidas");

        var resposta = await _httpClient.PostAsJsonAsync("dividas", new
        {
            descricao = dados.Descricao,
            valorParcela = dados.ValorParcela,
            numeroParcelas = dados.NumeroParcelas,
            dataPrimeiroVencimento = dados.DataPrimeiroVencimento.Date,
            valorQuitacaoAntecipada = dados.ValorQuitacaoAntecipada,
            categoriaId = dados.CategoriaId,
            formaPagamentoId = dados.FormaPagamentoId
        });

        await resposta.GarantirSucessoAsync();

        return await resposta.Content.ReadFromJsonAsync<DividaDetalhe>()
            ?? throw new InvalidOperationException("Resposta inválida ao cadastrar a dívida.");
    }

    private void ExigirConexao(string acao)
    {
        if (!_connectivity.EstaOnline())
        {
            throw new InvalidOperationException($"{acao} exige conexão com a internet.");
        }
    }
}
