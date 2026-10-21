using GestaoFrota.Domain.Common;
using GestaoFrota.Domain.Enums;

namespace GestaoFrota.Domain.Entities;

/// <summary>
/// Viagem realizada por um motorista, em um veículo, seguindo uma rota (US08).
/// O hodômetro final deve ser sempre maior que o inicial (validado na API e por
/// check constraint). A quilometragem rodada é calculada e GRAVADA no
/// encerramento, para que os relatórios da US10 somem direto no banco.
/// Suporta registro offline pelo app: o Id nasce no aparelho e o envio é
/// idempotente (ver ViagensController).
/// </summary>
public class Viagem : EntidadeBase
{
    public Guid VeiculoId { get; set; }
    public Veiculo? Veiculo { get; set; }

    public Guid RotaId { get; set; }
    public Rota? Rota { get; set; }

    public Guid MotoristaId { get; set; }
    public Usuario? Motorista { get; set; }

    public DateTime DataInicio { get; set; }
    public DateTime? DataFim { get; set; }

    public decimal HodometroInicial { get; set; }
    public decimal? HodometroFinal { get; set; }

    /// <summary>
    /// Hodômetro final menos inicial, persistido no encerramento (US08).
    /// Nulo enquanto a viagem está em andamento ou foi cancelada.
    /// </summary>
    public decimal? QuilometragemRodadaKm { get; set; }

    public StatusViagem Status { get; set; } = StatusViagem.EmAndamento;

    /// <summary>
    /// True quando o registro foi criado no aparelho sem conexão e chegou
    /// depois pela fila de sincronização do app.
    /// </summary>
    public bool SincronizadoOffline { get; set; }

    public string? Observacoes { get; set; }

    // Navegação
    public Frete? Frete { get; set; }
    public ICollection<Movimentacao> Movimentacoes { get; set; } = new List<Movimentacao>();
}
