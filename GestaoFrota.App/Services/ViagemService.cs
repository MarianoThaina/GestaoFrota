using System.Net.Http.Json;
using GestaoFrota.App.Models;

namespace GestaoFrota.App.Services;

public class NovaViagem
{
    public Veiculo Veiculo { get; set; } = new();
    public Rota Rota { get; set; } = new();
    public Guid MotoristaId { get; set; }
    public string MotoristaNome { get; set; } = string.Empty;
    public decimal HodometroInicial { get; set; }
    public string? Observacoes { get; set; }
}

public class Motorista
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public PerfilUsuario Perfil { get; set; }

    public override string ToString() => Nome;
}

public interface IViagemService
{
    /// <summary>
    /// Online: envia primeiro a fila offline, depois baixa a lista da API e
    /// atualiza o cache. Offline (ou API fora do ar): devolve o cache local,
    /// que inclui as viagens ainda não enviadas.
    /// </summary>
    Task<List<Viagem>> ListarAsync();

    Task<Viagem?> ObterLocalAsync(Guid id);

    /// <summary>Lista de usuários para o gestor escolher o motorista (exige conexão).</summary>
    Task<List<Motorista>> ListarMotoristasAsync();

    /// <summary>
    /// Abre a viagem. Sem internet, grava no aparelho com AberturaPendente e
    /// devolve normalmente: o motorista continua o trabalho (US08, offline-first).
    /// </summary>
    Task<Viagem> AbrirAsync(NovaViagem dados);

    /// <summary>
    /// Encerra a viagem. Sem internet, grava o hodômetro final no aparelho com
    /// EncerramentoPendente; a quilometragem oficial é calculada pelo servidor
    /// no reenvio (o app mostra a prévia local até lá).
    /// </summary>
    Task EncerrarAsync(Guid id, decimal hodometroFinal, string? observacoes);

    Task CancelarAsync(Guid id);

    /// <summary>Reenvia aberturas e encerramentos pendentes. Devolve quantos foram confirmados.</summary>
    Task<int> SincronizarPendentesAsync();
}

public class ViagemService : IViagemService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalDatabaseService _localDb;
    private readonly IConnectivityService _connectivity;

    public ViagemService(HttpClient httpClient, ILocalDatabaseService localDb, IConnectivityService connectivity)
    {
        _httpClient = httpClient;
        _localDb = localDb;
        _connectivity = connectivity;
    }

    public async Task<List<Viagem>> ListarAsync()
    {
        if (_connectivity.EstaOnline())
        {
            try
            {
                await SincronizarPendentesAsync();

                var daApi = await _httpClient.GetFromJsonAsync<List<Viagem>>("viagens?limite=500");
                if (daApi is not null)
                {
                    await _localDb.SubstituirViagensAsync(daApi);
                }
            }
            catch (HttpRequestException)
            {
                // API fora do ar: segue com o cache local.
            }
        }

        return await _localDb.ListarViagensAsync();
    }

    public Task<Viagem?> ObterLocalAsync(Guid id) => _localDb.ObterViagemAsync(id);

    public async Task<List<Motorista>> ListarMotoristasAsync()
    {
        if (!_connectivity.EstaOnline())
        {
            return new List<Motorista>();
        }

        try
        {
            return await _httpClient.GetFromJsonAsync<List<Motorista>>("viagens/motoristas") ?? new List<Motorista>();
        }
        catch (HttpRequestException)
        {
            return new List<Motorista>();
        }
    }

    public async Task<Viagem> AbrirAsync(NovaViagem dados)
    {
        // O Id nasce no aparelho: é o que torna o reenvio idempotente.
        var viagem = new Viagem
        {
            Id = Guid.NewGuid(),
            VeiculoId = dados.Veiculo.Id,
            VeiculoPlaca = dados.Veiculo.Placa,
            VeiculoDescricao = dados.Veiculo.Titulo,
            RotaId = dados.Rota.Id,
            RotaNome = dados.Rota.Nome,
            RotaOrigem = dados.Rota.Origem,
            RotaDestino = dados.Rota.Destino,
            MotoristaId = dados.MotoristaId,
            MotoristaNome = dados.MotoristaNome,
            DataInicio = DateTime.UtcNow,
            HodometroInicial = dados.HodometroInicial,
            Status = StatusViagem.EmAndamento,
            Observacoes = dados.Observacoes
        };

        if (_connectivity.EstaOnline())
        {
            var resposta = await _httpClient.PostAsJsonAsync("viagens", CorpoAbertura(viagem, criadaOffline: false));
            await resposta.GarantirSucessoAsync();

            var criada = await resposta.Content.ReadFromJsonAsync<Viagem>()
                ?? throw new InvalidOperationException("Resposta inválida ao abrir a viagem.");

            await _localDb.SalvarViagemLocalAsync(criada);
            await MarcarVeiculoAsync(criada.VeiculoId, StatusVeiculo.EmViagem);
            return criada;
        }

        // Offline-first (US08): fica no aparelho e sobe quando a rede voltar.
        viagem.AberturaPendente = true;
        viagem.SincronizadoOffline = true;
        await _localDb.SalvarViagemLocalAsync(viagem);
        await MarcarVeiculoAsync(viagem.VeiculoId, StatusVeiculo.EmViagem);
        return viagem;
    }

    public async Task EncerrarAsync(Guid id, decimal hodometroFinal, string? observacoes)
    {
        var local = await _localDb.ObterViagemAsync(id)
            ?? throw new InvalidOperationException("Viagem não encontrada no aparelho.");

        if (hodometroFinal <= local.HodometroInicial)
        {
            throw new InvalidOperationException("O hodômetro final deve ser maior que o hodômetro inicial.");
        }

        // Se a abertura ainda não subiu, o encerramento também espera na fila.
        if (_connectivity.EstaOnline() && !local.AberturaPendente)
        {
            var atualizada = await EnviarEncerramentoAsync(id, hodometroFinal, observacoes);
            await _localDb.SalvarViagemLocalAsync(atualizada);
            await MarcarVeiculoAsync(atualizada.VeiculoId, StatusVeiculo.Disponivel, hodometroFinal);
            return;
        }

        local.HodometroFinal = hodometroFinal;
        local.QuilometragemRodadaKm = Math.Round(hodometroFinal - local.HodometroInicial, 2); // prévia; o servidor recalcula
        local.DataFim = DateTime.UtcNow;
        local.Status = StatusViagem.Concluida;
        local.EncerramentoPendente = true;
        if (!string.IsNullOrWhiteSpace(observacoes))
        {
            local.Observacoes = observacoes.Trim();
        }

        await _localDb.SalvarViagemLocalAsync(local);
        await MarcarVeiculoAsync(local.VeiculoId, StatusVeiculo.Disponivel, hodometroFinal);
    }

    public async Task CancelarAsync(Guid id)
    {
        if (!_connectivity.EstaOnline())
        {
            throw new InvalidOperationException("Cancelar uma viagem exige conexão com a internet.");
        }

        using var requisicao = new HttpRequestMessage(HttpMethod.Patch, $"viagens/{id}/cancelar");
        var resposta = await _httpClient.SendAsync(requisicao);
        await resposta.GarantirSucessoAsync();
    }

    public async Task<int> SincronizarPendentesAsync()
    {
        if (!_connectivity.EstaOnline())
        {
            return 0;
        }

        var confirmadas = 0;

        foreach (var pendente in await _localDb.ListarViagensPendentesAsync())
        {
            try
            {
                if (pendente.AberturaPendente)
                {
                    var resposta = await _httpClient.PostAsJsonAsync("viagens", CorpoAbertura(pendente, criadaOffline: true));
                    await resposta.GarantirSucessoAsync();

                    var doServidor = await resposta.Content.ReadFromJsonAsync<Viagem>()
                        ?? throw new InvalidOperationException("Resposta inválida ao sincronizar a viagem.");

                    // Preserva o encerramento local que ainda vai subir no passo seguinte.
                    doServidor.EncerramentoPendente = pendente.EncerramentoPendente;
                    if (pendente.EncerramentoPendente)
                    {
                        doServidor.HodometroFinal = pendente.HodometroFinal;
                        doServidor.QuilometragemRodadaKm = pendente.QuilometragemRodadaKm;
                        doServidor.DataFim = pendente.DataFim;
                        doServidor.Status = StatusViagem.Concluida;
                        doServidor.Observacoes = pendente.Observacoes;
                    }

                    pendente.AberturaPendente = false;
                    pendente.ErroSincronizacao = null;
                    await _localDb.SalvarViagemLocalAsync(doServidor);
                    confirmadas++;
                }

                if (pendente.EncerramentoPendente && pendente.HodometroFinal.HasValue)
                {
                    var atualizada = await EnviarEncerramentoAsync(pendente.Id, pendente.HodometroFinal.Value, pendente.Observacoes);
                    await _localDb.SalvarViagemLocalAsync(atualizada);
                    confirmadas++;
                }
            }
            catch (ApiException ex)
            {
                // Erro de negócio (ex.: veículo já estava em viagem): guarda a
                // mensagem para a tela mostrar e segue para a próxima.
                pendente.ErroSincronizacao = ex.Message;
                await _localDb.SalvarViagemLocalAsync(pendente);
            }
            catch (HttpRequestException)
            {
                // Conexão caiu no meio: tenta de novo na próxima abertura da tela.
                break;
            }
        }

        return confirmadas;
    }

    // ---------------- Auxiliares ----------------

    private async Task<Viagem> EnviarEncerramentoAsync(Guid id, decimal hodometroFinal, string? observacoes)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Patch, $"viagens/{id}/encerrar")
        {
            Content = JsonContent.Create(new
            {
                hodometroFinal,
                observacoes = string.IsNullOrWhiteSpace(observacoes) ? null : observacoes.Trim()
            })
        };

        var resposta = await _httpClient.SendAsync(requisicao);
        await resposta.GarantirSucessoAsync();

        return await resposta.Content.ReadFromJsonAsync<Viagem>()
            ?? throw new InvalidOperationException("Resposta inválida ao encerrar a viagem.");
    }

    private static object CorpoAbertura(Viagem viagem, bool criadaOffline) => new
    {
        id = viagem.Id,
        veiculoId = viagem.VeiculoId,
        rotaId = viagem.RotaId,
        motoristaId = viagem.MotoristaId,
        dataInicio = viagem.DataInicio,
        hodometroInicial = viagem.HodometroInicial,
        observacoes = viagem.Observacoes,
        criadaOffline
    };

    /// <summary>Reflete no cache o status que o servidor vai aplicar ao veículo.</summary>
    private async Task MarcarVeiculoAsync(Guid veiculoId, StatusVeiculo status, decimal? kmAtual = null)
    {
        var veiculos = await _localDb.ListarVeiculosAsync();
        var veiculo = veiculos.FirstOrDefault(v => v.Id == veiculoId);
        if (veiculo is null)
        {
            return;
        }

        veiculo.Status = status;
        if (kmAtual.HasValue && kmAtual.Value > veiculo.KmAtual)
        {
            veiculo.KmAtual = kmAtual.Value;
        }

        await _localDb.SalvarVeiculoLocalAsync(veiculo);
    }
}
