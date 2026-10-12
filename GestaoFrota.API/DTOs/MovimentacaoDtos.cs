using System.ComponentModel.DataAnnotations;
using GestaoFrota.Domain.Entities;
using GestaoFrota.Domain.Enums;

namespace GestaoFrota.API.DTOs;

public record MovimentacaoResponse(
    Guid Id,
    TipoMovimentacao Tipo,
    decimal Valor,
    string? Descricao,
    StatusMovimentacao Status,
    DateTime Data,
    DateTime DataVencimento,
    DateTime? DataPagamento,
    Guid CategoriaId,
    string? CategoriaNome,
    Guid FormaPagamentoId,
    string? FormaPagamentoNome,
    Guid? DividaId,
    bool TemComprovante,
    string? ComprovanteUrl,
    bool Estornada,
    Guid UsuarioId)
{
    public static MovimentacaoResponse De(Movimentacao m) => new(
        m.Id, m.Tipo, m.Valor, m.Descricao, m.Status, m.Data, m.DataVencimento, m.DataPagamento,
        m.CategoriaId, m.Categoria?.Nome, m.FormaPagamentoId, m.FormaPagamento?.Nome,
        m.DividaId, m.ComprovanteUrl is not null, m.ComprovanteUrl, m.Estornada, m.UsuarioId);
}

public class MovimentacaoRequest : IValidatableObject
{
    [Required(ErrorMessage = "O tipo é obrigatório (Receita ou Despesa).")]
    public TipoMovimentacao Tipo { get; set; }

    [Range(0.01, 999999999999.99,
        ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal Valor { get; set; }

    [Required(ErrorMessage = "A descrição é obrigatória.")]
    [MaxLength(300, ErrorMessage = "A descrição aceita no máximo 300 caracteres.")]
    public string Descricao { get; set; } = string.Empty;

    [Required(ErrorMessage = "A categoria é obrigatória.")]
    public Guid? CategoriaId { get; set; }

    [Required(ErrorMessage = "A forma de pagamento é obrigatória.")]
    public Guid? FormaPagamentoId { get; set; }

    [Required(ErrorMessage = "A data de vencimento é obrigatória.")]
    public DateTime? DataVencimento { get; set; }

    public StatusMovimentacao Status { get; set; } = StatusMovimentacao.Pendente;

    /// <summary>Obrigatória quando Status = Pago.</summary>
    public DateTime? DataPagamento { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enum.IsDefined(Tipo))
        {
            yield return new ValidationResult("Tipo de movimentação inválido.", new[] { nameof(Tipo) });
        }

        if (!Enum.IsDefined(Status))
        {
            yield return new ValidationResult("Status inválido.", new[] { nameof(Status) });
        }

        if (Status == StatusMovimentacao.Pago && DataPagamento is null)
        {
            yield return new ValidationResult(
                "Informe a data do pagamento/recebimento para movimentações pagas.",
                new[] { nameof(DataPagamento) });
        }

        if (Status == StatusMovimentacao.Pago && DataPagamento is { } dp && dp.Date > DateTime.UtcNow.Date.AddDays(1))
        {
            yield return new ValidationResult(
                "A data do pagamento não pode estar no futuro.", new[] { nameof(DataPagamento) });
        }
    }
}

public class PagarMovimentacaoRequest
{
    /// <summary>Se omitida, usa a data de hoje (UTC).</summary>
    public DateTime? DataPagamento { get; set; }
}
