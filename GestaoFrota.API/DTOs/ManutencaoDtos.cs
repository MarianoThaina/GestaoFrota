using System.ComponentModel.DataAnnotations;
using GestaoFrota.Domain.Enums;

namespace GestaoFrota.API.DTOs;

public record ManutencaoResponse(
    Guid Id,
    Guid VeiculoId,
    string VeiculoPlaca,
    string VeiculoDescricao,
    DateTime Data,
    string Descricao,
    decimal ValorTotal,
    DateTime? DataConclusao,
    bool Concluida,
    Guid? MovimentacaoId,
    StatusMovimentacao? DespesaStatus);

public class ManutencaoRequest : IValidatableObject
{
    [Required(ErrorMessage = "O veículo é obrigatório.")]
    public Guid? VeiculoId { get; set; }

    [Required(ErrorMessage = "A data da manutenção é obrigatória.")]
    public DateTime? Data { get; set; }

    [Required(ErrorMessage = "Descreva o serviço realizado.")]
    [MaxLength(300, ErrorMessage = "A descrição aceita no máximo 300 caracteres.")]
    public string Descricao { get; set; } = string.Empty;

    [Range(0.01, 999999999999.99, ErrorMessage = "O valor total deve ser maior que zero.")]
    public decimal ValorTotal { get; set; }

    /// <summary>Forma de pagamento da despesa gerada automaticamente.</summary>
    [Required(ErrorMessage = "A forma de pagamento é obrigatória.")]
    public Guid? FormaPagamentoId { get; set; }

    /// <summary>
    /// Opcional. Se omitida, a despesa entra na categoria padrão "Manutenção".
    /// </summary>
    public Guid? CategoriaId { get; set; }

    /// <summary>False = despesa pendente; True = já paga (exige DataPagamento).</summary>
    public bool Pago { get; set; }

    public DateTime? DataPagamento { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Pago && DataPagamento is null)
        {
            yield return new ValidationResult(
                "Informe a data do pagamento para uma manutenção já paga.", new[] { nameof(DataPagamento) });
        }

        if (Pago && DataPagamento is { } dp && dp.Date > DateTime.UtcNow.Date.AddDays(1))
        {
            yield return new ValidationResult(
                "A data do pagamento não pode estar no futuro.", new[] { nameof(DataPagamento) });
        }

        if (Data is { } d && d.Date > DateTime.UtcNow.Date.AddDays(1))
        {
            yield return new ValidationResult(
                "A data da manutenção não pode estar no futuro.", new[] { nameof(Data) });
        }
    }
}
