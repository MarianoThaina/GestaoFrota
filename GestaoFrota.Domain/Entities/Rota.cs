using GestaoFrota.Domain.Common;

namespace GestaoFrota.Domain.Entities;

/// <summary>
/// Rota cadastrada, usada para planejar e comparar
/// viagens (custo por km, rentabilidade, etc.).
/// </summary>
public class Rota : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;

    public string Origem { get; set; } = string.Empty;

    public string Destino { get; set; } = string.Empty;

    public decimal DistanciaEstimadaKm { get; set; }

    public bool Ativa { get; set; } = true;

    // Navegação
    public ICollection<Viagem> Viagens { get; set; } = new List<Viagem>();
}
