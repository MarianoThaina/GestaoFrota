using GestaoFrota.App.Helpers;
using SQLite;

namespace GestaoFrota.App.Models;

/// <summary>Espelha GestaoFrota.Domain.Enums.StatusViagem.</summary>
public enum StatusViagem
{
    EmAndamento = 1,
    Concluida = 2,
    Cancelada = 3
}

/// <summary>
/// Viagem (US08). A mesma classe é desserializada da API e usada como tabela
/// local (SQLite). É a única entidade com fila de escrita offline: uma viagem
/// aberta ou encerrada sem internet fica marcada aqui e é reenviada pelo
/// ViagemService quando a conexão volta.
/// </summary>
public class Viagem
{
    [PrimaryKey]
    public Guid Id { get; set; }

    public Guid VeiculoId { get; set; }

    public string VeiculoPlaca { get; set; } = string.Empty;

    public string VeiculoDescricao { get; set; } = string.Empty;

    public Guid RotaId { get; set; }

    public string RotaNome { get; set; } = string.Empty;

    public string RotaOrigem { get; set; } = string.Empty;

    public string RotaDestino { get; set; } = string.Empty;

    public Guid MotoristaId { get; set; }

    public string MotoristaNome { get; set; } = string.Empty;

    [Indexed]
    public DateTime DataInicio { get; set; }

    public DateTime? DataFim { get; set; }

    public decimal HodometroInicial { get; set; }

    public decimal? HodometroFinal { get; set; }

    public decimal? QuilometragemRodadaKm { get; set; }

    public StatusViagem Status { get; set; }

    public string? Observacoes { get; set; }

    public bool SincronizadoOffline { get; set; }

    // ---------- Fila offline (só no aparelho) ----------

    /// <summary>True: a abertura ainda não chegou ao servidor.</summary>
    [Indexed]
    public bool AberturaPendente { get; set; }

    /// <summary>True: o encerramento (hodômetro final) ainda não chegou ao servidor.</summary>
    [Indexed]
    public bool EncerramentoPendente { get; set; }

    /// <summary>Último erro de negócio devolvido pela API ao tentar sincronizar.</summary>
    public string? ErroSincronizacao { get; set; }

    // ---------- Apresentação ----------

    [Ignore]
    public bool PendenteSincronizacao => AberturaPendente || EncerramentoPendente;

    [Ignore]
    public bool EmAndamento => Status == StatusViagem.EmAndamento;

    [Ignore]
    public string TrajetoTexto => $"{RotaOrigem} → {RotaDestino}";

    [Ignore]
    public string VeiculoTexto => $"{VeiculoDescricao} · {VeiculoPlaca.FormatarPlaca()}";

    [Ignore]
    public string DataTexto => DataFim.HasValue && Status != StatusViagem.EmAndamento
        ? $"{Formatacao.Data(DataInicio)} → {Formatacao.Data(DataFim.Value)}"
        : $"Iniciada em {Formatacao.Data(DataInicio)}";

    [Ignore]
    public string HodometroInicialTexto => Formatacao.Km(HodometroInicial);

    [Ignore]
    public string HodometroFinalTexto => HodometroFinal.HasValue ? Formatacao.Km(HodometroFinal.Value) : "—";

    [Ignore]
    public string RodadoTexto => QuilometragemRodadaKm.HasValue ? Formatacao.Km(QuilometragemRodadaKm.Value) : "—";

    [Ignore]
    public string StatusTexto => PendenteSincronizacao
        ? (EncerramentoPendente ? "Encerrada · aguardando envio" : "Aberta · aguardando envio")
        : Status switch
        {
            StatusViagem.EmAndamento => "Em andamento",
            StatusViagem.Concluida => "Concluída",
            _ => "Cancelada"
        };

    [Ignore]
    public Color CorStatus => PendenteSincronizacao
        ? Color.FromArgb("#B8860B")
        : Status switch
        {
            StatusViagem.EmAndamento => Color.FromArgb("#E8710A"),
            StatusViagem.Concluida => Color.FromArgb("#1E8E3E"),
            _ => Colors.Gray
        };

    /// <summary>Pode encerrar: em andamento e sem encerramento já na fila.</summary>
    [Ignore]
    public bool PodeEncerrar => Status == StatusViagem.EmAndamento && !EncerramentoPendente;

    [Ignore]
    public bool PodeCancelar => Status == StatusViagem.EmAndamento && !PendenteSincronizacao;

    [Ignore]
    public bool TemErroSincronizacao => !string.IsNullOrWhiteSpace(ErroSincronizacao);

    /// <summary>Usado pelo Picker "Vincular à viagem" do formulário de movimentação.</summary>
    public override string ToString() =>
        $"{Formatacao.Data(DataInicio)} · {RotaOrigem} → {RotaDestino} · {VeiculoPlaca.FormatarPlaca()}";
}
