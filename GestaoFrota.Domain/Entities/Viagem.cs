using GestaoFrota.Domain.Common;
using GestaoFrota.Domain.Enums;

namespace GestaoFrota.Domain.Entities;

/// <summary>
/// Viagem realizada por um motorista, em um veículo, seguindo uma rota.
/// O hodômetro final deve ser sempre maior que o inicial (validado na API).
/// Suporta registro offline pelo app, sincronizado posteriormente.
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

    public StatusViagem Status { get; set; } = StatusViagem.EmAndamento;

    /// <summary>
    /// Indica se o registro foi criado offline e ainda aguarda sincronização.
    /// </summary>
    public bool SincronizadoOffline { get; set; } = true;

    public string? Observacoes { get; set; }

    // Navegação
    public Frete? Frete { get; set; }
    public ICollection<Movimentacao> Movimentacoes { get; set; } = new List<Movimentacao>();

    /// <summary>
    /// Distância percorrida na viagem (km). Nulo enquanto em andamento.
    /// </summary>
    public decimal? DistanciaPercorridaKm =>
        HodometroFinal.HasValue ? HodometroFinal.Value - HodometroInicial : null;
}
