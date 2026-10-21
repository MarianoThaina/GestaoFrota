using System.ComponentModel.DataAnnotations;
using GestaoFrota.Domain.Entities;
using GestaoFrota.Domain.Enums;
using GestaoFrota.Domain.Services;

namespace GestaoFrota.API.DTOs;

public record VeiculoResponse(
    Guid Id,
    string Placa,
    string PlacaFormatada,
    string Marca,
    string Modelo,
    int Ano,
    decimal CapacidadeCargaToneladas,
    StatusVeiculo Status,
    decimal KmAtual,
    DateTime? UltimaManutencao,
    string? ObservacoesManutencao,
    bool Ativo)
{
    public static VeiculoResponse De(Veiculo v) => new(
        v.Id, v.Placa, ValidadorPlaca.Formatar(v.Placa), v.Marca, v.Modelo, v.Ano,
        v.CapacidadeCargaToneladas, v.Status, v.KmAtual, v.UltimaManutencao,
        v.ObservacoesManutencao, v.Ativo);
}

public class VeiculoRequest : IValidatableObject
{
    [Required(ErrorMessage = "A placa é obrigatória.")]
    [MaxLength(10)]
    public string Placa { get; set; } = string.Empty;

    [Required(ErrorMessage = "A marca é obrigatória.")]
    [MaxLength(100)]
    public string Marca { get; set; } = string.Empty;

    [Required(ErrorMessage = "O modelo é obrigatório.")]
    [MaxLength(100)]
    public string Modelo { get; set; } = string.Empty;

    [Range(1950, 2100, ErrorMessage = "Informe um ano de fabricação válido.")]
    public int Ano { get; set; }

    [Range(0.01, 999.99, ErrorMessage = "A capacidade de carga deve ser maior que zero (em toneladas).")]
    public decimal CapacidadeCargaToneladas { get; set; }

    [Range(0, 9_999_999_999, ErrorMessage = "O hodômetro atual não pode ser negativo.")]
    public decimal KmAtual { get; set; }

    public bool Ativo { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ValidadorPlaca.EhValida(Placa))
        {
            yield return new ValidationResult(
                "Placa inválida. Use o formato antigo (ABC-1234) ou Mercosul (ABC1D23).",
                new[] { nameof(Placa) });
        }

        if (Ano > DateTime.UtcNow.Year + 1)
        {
            yield return new ValidationResult(
                "O ano de fabricação não pode estar no futuro.", new[] { nameof(Ano) });
        }
    }
}
