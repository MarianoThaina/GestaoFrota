using GestaoFrota.Domain.Common;

namespace GestaoFrota.Domain.Entities;

/// <summary>
/// Frete: carga/serviço de transporte vinculado a uma viagem,
/// usado para calcular rentabilidade e custo por km.
/// </summary>
public class Frete : EntidadeBase
{
    public Guid ViagemId { get; set; }
    public Viagem? Viagem { get; set; }

    public string Cliente { get; set; } = string.Empty;

    public string? DescricaoCarga { get; set; }

    public decimal PesoCargaKg { get; set; }

    public decimal ValorFrete { get; set; }
}
