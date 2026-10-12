using System.Net.Http.Headers;
using System.Net.Http.Json;
using GestaoFrota.App.Models;

namespace GestaoFrota.App.Services;

public class NovaMovimentacao
{
    public TipoMovimentacao Tipo { get; set; }
    public decimal Valor { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public Guid CategoriaId { get; set; }
    public Guid FormaPagamentoId { get; set; }
    public DateTime DataVencimento { get; set; }
    public bool Pago { get; set; }
    public DateTime? DataPagamento { get; set; }
}

public interface IMovimentacaoService
{
    /// <summary>
    /// Online: busca na API e atualiza o cache local. Offline (ou API fora do ar):
    /// devolve o último histórico salvo no aparelho.
    /// </summary>
    Task<List<Movimentacao>> ListarAsync();

    Task<Movimentacao> CriarAsync(NovaMovimentacao dados);

    Task PagarAsync(Guid id, DateTime dataPagamento);

    Task EstornarAsync(Guid id);

    Task EnviarComprovanteAsync(Guid id, string caminhoArquivo);

    Task<byte[]> ObterComprovanteAsync(Guid id);
}

public class MovimentacaoService : IMovimentacaoService
{
    public const long TamanhoMaximoComprovanteBytes = 5 * 1024 * 1024;

    private readonly HttpClient _httpClient;
    private readonly ILocalDatabaseService _localDb;
    private readonly IConnectivityService _connectivity;

    public MovimentacaoService(HttpClient httpClient, ILocalDatabaseService localDb, IConnectivityService connectivity)
    {
        _httpClient = httpClient;
        _localDb = localDb;
        _connectivity = connectivity;
    }

    public async Task<List<Movimentacao>> ListarAsync()
    {
        if (_connectivity.EstaOnline())
        {
            try
            {
                var daApi = await _httpClient.GetFromJsonAsync<List<Movimentacao>>("movimentacoes?limite=500");
                if (daApi is not null)
                {
                    await _localDb.SubstituirMovimentacoesAsync(daApi);
                }
            }
            catch (HttpRequestException)
            {
                // API fora do ar: segue com o cache local.
            }
        }

        return await _localDb.ListarMovimentacoesAsync();
    }

    public async Task<Movimentacao> CriarAsync(NovaMovimentacao dados)
    {
        ExigirConexao("Registrar movimentações");

        var resposta = await _httpClient.PostAsJsonAsync("movimentacoes", new
        {
            tipo = dados.Tipo,
            valor = dados.Valor,
            descricao = dados.Descricao,
            categoriaId = dados.CategoriaId,
            formaPagamentoId = dados.FormaPagamentoId,
            dataVencimento = dados.DataVencimento.Date,
            status = dados.Pago ? StatusMovimentacao.Pago : StatusMovimentacao.Pendente,
            dataPagamento = dados.Pago ? dados.DataPagamento?.Date : null
        });

        await resposta.GarantirSucessoAsync();

        var criada = await resposta.Content.ReadFromJsonAsync<Movimentacao>()
            ?? throw new InvalidOperationException("Resposta inválida ao criar a movimentação.");

        await _localDb.SalvarMovimentacaoLocalAsync(criada);
        return criada;
    }

    public async Task PagarAsync(Guid id, DateTime dataPagamento)
    {
        ExigirConexao("Marcar como pago");

        using var requisicao = new HttpRequestMessage(HttpMethod.Patch, $"movimentacoes/{id}/pagar")
        {
            Content = JsonContent.Create(new { dataPagamento = dataPagamento.Date })
        };

        var resposta = await _httpClient.SendAsync(requisicao);
        await resposta.GarantirSucessoAsync();
    }

    public async Task EstornarAsync(Guid id)
    {
        ExigirConexao("Estornar");

        using var requisicao = new HttpRequestMessage(HttpMethod.Patch, $"movimentacoes/{id}/estornar");
        var resposta = await _httpClient.SendAsync(requisicao);
        await resposta.GarantirSucessoAsync();
    }

    public async Task EnviarComprovanteAsync(Guid id, string caminhoArquivo)
    {
        ExigirConexao("Anexar comprovantes");

        var info = new FileInfo(caminhoArquivo);
        if (!info.Exists)
        {
            throw new InvalidOperationException("O arquivo do comprovante não foi encontrado.");
        }

        if (info.Length > TamanhoMaximoComprovanteBytes)
        {
            throw new InvalidOperationException("O comprovante deve ter no máximo 5 MB.");
        }

        await using var fluxo = System.IO.File.OpenRead(caminhoArquivo);
        using var conteudo = new MultipartFormDataContent();

        var arquivo = new StreamContent(fluxo);
        arquivo.Headers.ContentType = new MediaTypeHeaderValue(ContentTypePorExtensao(caminhoArquivo));
        conteudo.Add(arquivo, "arquivo", Path.GetFileName(caminhoArquivo));

        var resposta = await _httpClient.PostAsync($"movimentacoes/{id}/comprovante", conteudo);
        await resposta.GarantirSucessoAsync();
    }

    public async Task<byte[]> ObterComprovanteAsync(Guid id)
    {
        ExigirConexao("Visualizar comprovantes");

        var resposta = await _httpClient.GetAsync($"movimentacoes/{id}/comprovante");
        await resposta.GarantirSucessoAsync();

        return await resposta.Content.ReadAsByteArrayAsync();
    }

    private void ExigirConexao(string acao)
    {
        // Mesmo critério adotado em Categorias (editar/desativar): sem fila de
        // escrita offline para operações financeiras, falha de forma explícita
        // em vez de fingir que salvou.
        if (!_connectivity.EstaOnline())
        {
            throw new InvalidOperationException($"{acao} exige conexão com a internet.");
        }
    }

    private static string ContentTypePorExtensao(string caminho) =>
        Path.GetExtension(caminho).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".heic" or ".heif" => "image/heic",
            _ => "image/jpeg"
        };
}
