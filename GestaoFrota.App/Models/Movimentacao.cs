using GestaoFrota.App.Helpers;
using SQLite;

namespace GestaoFrota.App.Models;

public enum TipoMovimentacao
{
    Receita = 1,
    Despesa = 2
}

public enum StatusMovimentacao
{
    Pendente = 1,
    Pago = 2
}

/// <summary>
/// Movimentação financeira. A mesma classe é desserializada da API e usada
/// como tabela de cache local (SQLite), permitindo consultar o histórico offline.
/// As propriedades de apresentação são somente leitura e marcadas com [Ignore].
/// </summary>
public class Movimentacao
{
    [PrimaryKey]
    public Guid Id { get; set; }

    public TipoMovimentacao Tipo { get; set; }

    public decimal Valor { get; set; }

    public string? Descricao { get; set; }

    public StatusMovimentacao Status { get; set; }

    public DateTime Data { get; set; }

    [Indexed]
    public DateTime DataVencimento { get; set; }

    public DateTime? DataPagamento { get; set; }

    public Guid CategoriaId { get; set; }

    public string? CategoriaNome { get; set; }

    public Guid FormaPagamentoId { get; set; }

    public string? FormaPagamentoNome { get; set; }

    public Guid? DividaId { get; set; }

    public bool TemComprovante { get; set; }

    public string? ComprovanteUrl { get; set; }

    public bool Estornada { get; set; }

    /// <summary>Viagem vinculada (US09), quando houver.</summary>
    public Guid? ViagemId { get; set; }

    /// <summary>Litros abastecidos (US09), só em despesas de combustível.</summary>
    public decimal? QuantidadeLitros { get; set; }

    // ---------- Apresentação ----------

    [Ignore]
    public string ValorFormatado =>
        (Tipo == TipoMovimentacao.Despesa ? "- " : "+ ") + Formatacao.Moeda(Valor);

    [Ignore]
    public Color CorValor => Estornada
        ? Colors.Gray
        : Tipo == TipoMovimentacao.Receita ? Color.FromArgb("#1E8E3E") : Color.FromArgb("#D93025");

    [Ignore]
    public string StatusTexto => Estornada
        ? "Estornada"
        : Status == StatusMovimentacao.Pago
            ? (Tipo == TipoMovimentacao.Receita ? "Recebido" : "Pago")
            : "Pendente";

    [Ignore]
    public Color CorStatus => Estornada
        ? Colors.Gray
        : Status == StatusMovimentacao.Pago ? Color.FromArgb("#1E8E3E") : Color.FromArgb("#E8710A");

    [Ignore]
    public string VencimentoTexto => $"Vence em {Formatacao.Data(DataVencimento)}";

    [Ignore]
    public string? PagamentoTexto => DataPagamento.HasValue
        ? $"{(Tipo == TipoMovimentacao.Receita ? "Recebido" : "Pago")} em {Formatacao.Data(DataPagamento.Value)}"
        : null;

    [Ignore]
    public string DetalheTexto =>
        string.Join(" · ", new[]
        {
            CategoriaNome,
            FormaPagamentoNome,
            QuantidadeLitros.HasValue ? $"{QuantidadeLitros.Value.ToString("N1", Formatacao.PtBr)} L" : null,
            ViagemId.HasValue ? "vinculada a viagem" : null
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

    [Ignore]
    public bool PodePagar => Status == StatusMovimentacao.Pendente && !Estornada;

    [Ignore]
    public bool PodeEstornar => !Estornada;

    [Ignore]
    public bool TemPagamento => DataPagamento.HasValue;
}
