using GestaoFrota.Domain.Common;
using GestaoFrota.Domain.Enums;

namespace GestaoFrota.Domain.Entities;

/// <summary>
/// Veículo da frota. A placa segue validação de formato
/// (padrão antigo ou Mercosul) na camada de aplicação/API.
/// </summary>
public class Veiculo : EntidadeBase
{
    public string Placa { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public string Marca { get; set; } = string.Empty;

    public int Ano { get; set; }

    public StatusVeiculo Status { get; set; } = StatusVeiculo.Disponivel;

    public decimal KmAtual { get; set; }

    public DateTime? UltimaManutencao { get; set; }

    public string? ObservacoesManutencao { get; set; }

    public bool Ativo { get; set; } = true;

    // Navegação
    public ICollection<Viagem> Viagens { get; set; } = new List<Viagem>();
}
