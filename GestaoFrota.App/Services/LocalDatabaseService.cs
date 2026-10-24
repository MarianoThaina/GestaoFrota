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

    // ---------- Frota (US06/US07/US08) ----------
    Task<List<Veiculo>> ListarVeiculosAsync();
    Task SubstituirVeiculosAsync(IEnumerable<Veiculo> veiculos);
    Task<int> SalvarVeiculoLocalAsync(Veiculo veiculo);

    Task<List<Rota>> ListarRotasAsync();
    Task SubstituirRotasAsync(IEnumerable<Rota> rotas);
    Task<int> SalvarRotaLocalAsync(Rota rota);

    Task<List<Viagem>> ListarViagensAsync();
    Task<Viagem?> ObterViagemAsync(Guid id);
    Task<List<Viagem>> ListarViagensPendentesAsync();

    /// <summary>
    /// Troca o cache pelo que veio do servidor, mas PRESERVA as viagens que
    /// ainda estão na fila offline (abertura ou encerramento pendente).
    /// </summary>
    Task SubstituirViagensAsync(IEnumerable<Viagem> viagens);
    Task<int> SalvarViagemLocalAsync(Viagem viagem);

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

        // CreateTableAsync também acrescenta colunas novas em tabelas já existentes
        // (ex.: ViagemId e QuantidadeLitros em Movimentacao), sem apagar dados.
        await _conexao.CreateTableAsync<Categoria>();
        await _conexao.CreateTableAsync<FormaPagamento>();
        await _conexao.CreateTableAsync<Movimentacao>();
        await _conexao.CreateTableAsync<Veiculo>();
        await _conexao.CreateTableAsync<Rota>();
        await _conexao.CreateTableAsync<Viagem>();

        return _conexao;
    }

    public async Task InicializarAsync() => await ObterConexaoAsync();

    // ---------- Categorias ----------

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

    // ---------- Formas de pagamento ----------

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

    // ---------- Movimentações ----------

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

    // ---------- Veículos ----------

    public async Task<List<Veiculo>> ListarVeiculosAsync()
    {
        var conexao = await ObterConexaoAsync();
        return await conexao.Table<Veiculo>().OrderBy(v => v.Marca).ThenBy(v => v.Modelo).ToListAsync();
    }

    public async Task SubstituirVeiculosAsync(IEnumerable<Veiculo> veiculos)
    {
        var conexao = await ObterConexaoAsync();
        await conexao.RunInTransactionAsync(tran =>
        {
            tran.DeleteAll<Veiculo>();
            foreach (var veiculo in veiculos)
            {
                tran.InsertOrReplace(veiculo);
            }
        });
    }

    public async Task<int> SalvarVeiculoLocalAsync(Veiculo veiculo)
    {
        var conexao = await ObterConexaoAsync();
        return await conexao.InsertOrReplaceAsync(veiculo);
    }

    // ---------- Rotas ----------

    public async Task<List<Rota>> ListarRotasAsync()
    {
        var conexao = await ObterConexaoAsync();
        return await conexao.Table<Rota>().OrderBy(r => r.Nome).ToListAsync();
    }

    public async Task SubstituirRotasAsync(IEnumerable<Rota> rotas)
    {
        var conexao = await ObterConexaoAsync();
        await conexao.RunInTransactionAsync(tran =>
        {
            tran.DeleteAll<Rota>();
            foreach (var rota in rotas)
            {
                tran.InsertOrReplace(rota);
            }
        });
    }

    public async Task<int> SalvarRotaLocalAsync(Rota rota)
    {
        var conexao = await ObterConexaoAsync();
        return await conexao.InsertOrReplaceAsync(rota);
    }

    // ---------- Viagens (com fila offline) ----------

    public async Task<List<Viagem>> ListarViagensAsync()
    {
        var conexao = await ObterConexaoAsync();
        var viagens = await conexao.Table<Viagem>().ToListAsync();

        // Em andamento primeiro, depois as mais recentes.
        return viagens
            .OrderByDescending(v => v.Status == StatusViagem.EmAndamento)
            .ThenByDescending(v => v.DataInicio)
            .ToList();
    }

    public async Task<Viagem?> ObterViagemAsync(Guid id)
    {
        var conexao = await ObterConexaoAsync();
        return await conexao.Table<Viagem>().Where(v => v.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<Viagem>> ListarViagensPendentesAsync()
    {
        var conexao = await ObterConexaoAsync();
        return await conexao.Table<Viagem>()
            .Where(v => v.AberturaPendente || v.EncerramentoPendente)
            .OrderBy(v => v.DataInicio)
            .ToListAsync();
    }

    public async Task SubstituirViagensAsync(IEnumerable<Viagem> viagens)
    {
        var conexao = await ObterConexaoAsync();
        var pendentes = await ListarViagensPendentesAsync();
        var idsPendentes = pendentes.Select(p => p.Id).ToHashSet();

        await conexao.RunInTransactionAsync(tran =>
        {
            // Apaga só o que o servidor já conhece; a fila offline fica intacta.
            tran.Table<Viagem>().Delete(v => !v.AberturaPendente && !v.EncerramentoPendente);

            foreach (var viagem in viagens)
            {
                if (!idsPendentes.Contains(viagem.Id))
                {
                    tran.InsertOrReplace(viagem);
                }
            }
        });
    }

    public async Task<int> SalvarViagemLocalAsync(Viagem viagem)
    {
        var conexao = await ObterConexaoAsync();
        return await conexao.InsertOrReplaceAsync(viagem);
    }

    public async Task LimparCacheFinanceiroAsync()
    {
        var conexao = await ObterConexaoAsync();
        await conexao.DeleteAllAsync<Movimentacao>();
    }
}
