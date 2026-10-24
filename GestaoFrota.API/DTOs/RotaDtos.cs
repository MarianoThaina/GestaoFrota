using System.ComponentModel.DataAnnotations;
using GestaoFrota.Domain.Entities;

namespace GestaoFrota.API.DTOs;

public record RotaResponse(
    Guid Id,
    string Nome,
    string Origem,
    string Destino,
    decimal DistanciaEstimadaKm,
    bool Ativa)
{
    public static RotaResponse De(Rota r) => new(r.Id, r.Nome, r.Origem, r.Destino, r.DistanciaEstimadaKm, r.Ativa);
}

public class RotaRequest
{
    [Required(ErrorMessage = "O nome da rota é obrigatório.")]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "A origem é obrigatória.")]
    [MaxLength(150)]
    public string Origem { get; set; } = string.Empty;

    [Required(ErrorMessage = "O destino é obrigatório.")]
    [MaxLength(150)]
    public string Destino { get; set; } = string.Empty;

    [Range(0.1, 99_999_999, ErrorMessage = "A distância estimada deve ser maior que zero (em km).")]
    public decimal DistanciaEstimadaKm { get; set; }

    public bool Ativa { get; set; } = true;
}
