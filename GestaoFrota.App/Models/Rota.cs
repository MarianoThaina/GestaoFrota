using GestaoFrota.App.Helpers;
using SQLite;

namespace GestaoFrota.App.Models;

/// <summary>
/// Rota cadastrada (US07). Desserializada da API e usada como tabela de cache
/// local (SQLite) para abrir viagens sem internet.
/// </summary>
public class Rota
{
    [PrimaryKey]
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Origem { get; set; } = string.Empty;

    public string Destino { get; set; } = string.Empty;

    public decimal DistanciaEstimadaKm { get; set; }

    public bool Ativa { get; set; } = true;

    // ---------- Apresentação ----------

    [Ignore]
    public string TrajetoTexto => $"{Origem} → {Destino}";

    [Ignore]
    public string DistanciaTexto => $"{Formatacao.Km(DistanciaEstimadaKm)} estimados";

    /// <summary>Usado pelo Picker ao escolher a rota de uma viagem.</summary>
    public override string ToString() => $"{Nome} ({Origem} → {Destino})";
}
