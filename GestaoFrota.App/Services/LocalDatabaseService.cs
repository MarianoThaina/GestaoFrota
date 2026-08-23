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
}
