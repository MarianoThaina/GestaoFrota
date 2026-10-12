using GestaoFrota.App.Models;
using SQLite;

namespace GestaoFrota.App.Services;

public interface ILocalDatabaseService
{
    Task InicializarAsync();

    Task<List<Categoria>> ListarCategoriasAsync();
    Task SalvarCategoriasAsync(IEnumerable<Categoria> categorias);
    Task<int> SalvarCategoriaLocalAsync(Categoria categoria);

    Task<List<FormaPagamento>> ListarFormasPagamentoAsync();
    Task SalvarFormasPagamentoAsync(IEnumerable<FormaPagamento> formas);
    Task<int> SalvarFormaPagamentoLocalAsync(FormaPagamento forma);

    Task<List<Movimentacao>> ListarMovimentacoesAsync();
    Task SubstituirMovimentacoesAsync(IEnumerable<Movimentacao> movimentacoes);
    Task<int> SalvarMovimentacaoLocalAsync(Movimentacao movimentacao);

    /// <summary>Apaga os dados financeiros em cache (chamado no logout).</summary>
    Task LimparCacheFinanceiroAsync();
}

/// <summary>
/// Persistência local usada como cache "source of truth" para a UI:
/// as telas sempre leem daqui; a sincronização com a API roda em segundo
/// plano e atualiza este banco (arquitetura Offline-First, requisito
/// não funcional do documento de especificação).
/// </summary>
public class LocalDatabaseService : ILocalDatabaseService
{
    private SQLiteAsyncConnection? _conexao;

    private async Task<SQLiteAsyncConnection> ObterConexaoAsync()
    {
        if (_conexao is not null)
        {
            return _conexao;
        }

        var caminhoBanco = Path.Combine(FileSystem.AppDataDirectory, "gestaofrota_local.db3");
        _conexao = new SQLiteAsyncConnection(caminhoBanco);

        await _conexao.CreateTableAsync<Categoria>();
        await _conexao.CreateTableAsync<FormaPagamento>();
        await _conexao.CreateTableAsync<Movimentacao>();

        return _conexao;
    }

    public async Task InicializarAsync() => await ObterConexaoAsync();

    public async Task<List<Categoria>> ListarCategoriasAsync()
    {
        var conexao = await ObterConexaoAsync();
        return await conexao.Table<Categoria>().OrderBy(c => c.Nome).ToListAsync();
    }

    public async Task SalvarCategoriasAsync(IEnumerable<Categoria> categorias)
    {
        var conexao = await ObterConexaoAsync();
        await conexao.RunInTransactionAsync(tran =>
        {
            foreach (var categoria in categorias)
            {
                tran.InsertOrReplace(categoria);
            }
        });
    }

    public async Task<int> SalvarCategoriaLocalAsync(Categoria categoria)
    {
        var conexao = await ObterConexaoAsync();
        return await conexao.InsertOrReplaceAsync(categoria);
    }

    public async Task<List<FormaPagamento>> ListarFormasPagamentoAsync()
    {
        var conexao = await ObterConexaoAsync();
        return await conexao.Table<FormaPagamento>().OrderBy(f => f.Nome).ToListAsync();
    }

    public async Task SalvarFormasPagamentoAsync(IEnumerable<FormaPagamento> formas)
    {
        var conexao = await ObterConexaoAsync();
        await conexao.RunInTransactionAsync(tran =>
        {
            foreach (var forma in formas)
            {
                tran.InsertOrReplace(forma);
            }
        });
    }

    public async Task<int> SalvarFormaPagamentoLocalAsync(FormaPagamento forma)
    {
        var conexao = await ObterConexaoAsync();
        return await conexao.InsertOrReplaceAsync(forma);
    }

    public async Task<List<Movimentacao>> ListarMovimentacoesAsync()
    {
        var conexao = await ObterConexaoAsync();
        return await conexao.Table<Movimentacao>().OrderByDescending(m => m.DataVencimento).ToListAsync();
    }

    public async Task SubstituirMovimentacoesAsync(IEnumerable<Movimentacao> movimentacoes)
    {
        var conexao = await ObterConexaoAsync();
        await conexao.RunInTransactionAsync(tran =>
        {
            // O servidor é a fonte da verdade: troca o cache inteiro de uma vez.
            tran.DeleteAll<Movimentacao>();
            foreach (var movimentacao in movimentacoes)
            {
                tran.InsertOrReplace(movimentacao);
            }
        });
    }

    public async Task<int> SalvarMovimentacaoLocalAsync(Movimentacao movimentacao)
    {
        var conexao = await ObterConexaoAsync();
        return await conexao.InsertOrReplaceAsync(movimentacao);
    }

    public async Task LimparCacheFinanceiroAsync()
    {
        var conexao = await ObterConexaoAsync();
        await conexao.DeleteAllAsync<Movimentacao>();
    }
}
