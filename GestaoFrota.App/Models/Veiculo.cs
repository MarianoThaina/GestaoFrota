using GestaoFrota.App.Helpers;
using SQLite;

namespace GestaoFrota.App.Models;

/// <summary>Espelha GestaoFrota.Domain.Enums.StatusVeiculo.</summary>
public enum StatusVeiculo
{
    Disponivel = 1,
    EmViagem = 2,
    EmManutencao = 3
}

/// <summary>
/// Veículo da frota (US06). Desserializado da API e usado como tabela de cache
/// local (SQLite) para o motorista escolher o veículo mesmo sem internet.
/// </summary>
public class Veiculo
{
    [PrimaryKey]
    public Guid Id { get; set; }

    [Indexed]
    public string Placa { get; set; } = string.Empty;

    public string PlacaFormatada { get; set; } = string.Empty;

    public string Marca { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public int Ano { get; set; }

    public decimal CapacidadeCargaToneladas { get; set; }

    public StatusVeiculo Status { get; set; }

    public decimal KmAtual { get; set; }

    public DateTime? UltimaManutencao { get; set; }

    public string? ObservacoesManutencao { get; set; }

    public bool Ativo { get; set; } = true;

    // ---------- Apresentação ----------

    [Ignore]
    public string Titulo => $"{Marca} {Modelo}".Trim();

    [Ignore]
    public string PlacaTexto => string.IsNullOrWhiteSpace(PlacaFormatada) ? Placa.FormatarPlaca() : PlacaFormatada;

    [Ignore]
    public string DetalheTexto => $"{Ano} · {Formatacao.Toneladas(CapacidadeCargaToneladas)} · {Formatacao.Km(KmAtual)}";

    [Ignore]
    public string StatusTexto => Status switch
    {
        StatusVeiculo.EmViagem => "Em viagem",
        StatusVeiculo.EmManutencao => "Em manutenção",
        _ => Ativo ? "Disponível" : "Desativado"
    };

    [Ignore]
    public Color CorStatus => !Ativo
        ? Colors.Gray
        : Status switch
        {
            StatusVeiculo.EmViagem => Color.FromArgb("#E8710A"),
            StatusVeiculo.EmManutencao => Color.FromArgb("#D93025"),
            _ => Color.FromArgb("#1E8E3E")
        };

    [Ignore]
    public bool Disponivel => Ativo && Status == StatusVeiculo.Disponivel;

    [Ignore]
    public bool EmManutencao => Status == StatusVeiculo.EmManutencao;

    [Ignore]
    public bool PodeRegistrarManutencao => Ativo && Status != StatusVeiculo.EmViagem && Status != StatusVeiculo.EmManutencao;

    [Ignore]
    public string? ManutencaoTexto => UltimaManutencao.HasValue
        ? $"Última manutenção em {Formatacao.Data(UltimaManutencao.Value)}" +
          (string.IsNullOrWhiteSpace(ObservacoesManutencao) ? string.Empty : $": {ObservacoesManutencao}")
        : null;

    [Ignore]
    public bool TemManutencao => UltimaManutencao.HasValue;

    /// <summary>Usado pelo Picker ao escolher o veículo de uma viagem.</summary>
    public override string ToString() => $"{PlacaTexto} · {Titulo}";
}
