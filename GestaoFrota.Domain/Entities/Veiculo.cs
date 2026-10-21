using GestaoFrota.Domain.Common;
using GestaoFrota.Domain.Enums;

namespace GestaoFrota.Domain.Entities;

/// <summary>
/// Veículo da frota (US06). A placa é guardada normalizada (maiúscula, sem
/// hífen) e validada no formato antigo (AAA9999) ou Mercosul (AAA9A99) pela
/// <see cref="Services.ValidadorPlaca"/>; o banco garante que é única.
/// </summary>
public class Veiculo : EntidadeBase
{
    public string Placa { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public string Marca { get; set; } = string.Empty;

    public int Ano { get; set; }

    /// <summary>Capacidade de carga em toneladas (US06).</summary>
    public decimal CapacidadeCargaToneladas { get; set; }

    public StatusVeiculo Status { get; set; } = StatusVeiculo.Disponivel;

    /// <summary>Último hodômetro conhecido; atualizado ao encerrar uma viagem.</summary>
    public decimal KmAtual { get; set; }

    /// <summary>Data da última manutenção registrada (espelho da entidade Manutencao).</summary>
    public DateTime? UltimaManutencao { get; set; }

    public string? ObservacoesManutencao { get; set; }

    public bool Ativo { get; set; } = true;

    // Navegação
    public ICollection<Viagem> Viagens { get; set; } = new List<Viagem>();
    public ICollection<Manutencao> Manutencoes { get; set; } = new List<Manutencao>();
}
