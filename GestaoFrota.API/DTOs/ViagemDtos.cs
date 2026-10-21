using System.ComponentModel.DataAnnotations;
using GestaoFrota.Domain.Enums;

namespace GestaoFrota.API.DTOs;

public record ViagemResponse(
    Guid Id,
    Guid VeiculoId,
    string VeiculoPlaca,
    string VeiculoDescricao,
    Guid RotaId,
    string RotaNome,
    string RotaOrigem,
    string RotaDestino,
    Guid MotoristaId,
    string MotoristaNome,
    DateTime DataInicio,
    DateTime? DataFim,
    decimal HodometroInicial,
    decimal? HodometroFinal,
    decimal? QuilometragemRodadaKm,
    StatusViagem Status,
    string? Observacoes,
    bool SincronizadoOffline);

/// <summary>Usuário que pode ser escolhido como motorista ao abrir uma viagem.</summary>
public record MotoristaResponse(Guid Id, string Nome, PerfilUsuario Perfil);

public class AbrirViagemRequest
{
    /// <summary>
    /// Opcional. O app gera o Id no aparelho para a viagem aberta sem internet;
    /// se já existir no servidor, a API devolve a existente (envio idempotente).
    /// </summary>
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "O veículo é obrigatório.")]
    public Guid? VeiculoId { get; set; }

    [Required(ErrorMessage = "A rota é obrigatória.")]
    public Guid? RotaId { get; set; }

    /// <summary>
    /// Opcional. Se omitido, o motorista é o próprio usuário autenticado.
    /// Só Administrador e Gestor de Frota podem abrir viagem para outra pessoa.
    /// </summary>
    public Guid? MotoristaId { get; set; }

    /// <summary>Opcional. Se omitida, usa o momento da requisição (UTC).</summary>
    public DateTime? DataInicio { get; set; }

    [Range(0, 9_999_999_999, ErrorMessage = "O hodômetro inicial não pode ser negativo.")]
    public decimal HodometroInicial { get; set; }

    [MaxLength(500, ErrorMessage = "As observações aceitam no máximo 500 caracteres.")]
    public string? Observacoes { get; set; }

    /// <summary>True quando o registro nasceu no aparelho sem conexão.</summary>
    public bool CriadaOffline { get; set; }
}

public class EncerrarViagemRequest
{
    [Range(0, 9_999_999_999, ErrorMessage = "O hodômetro final não pode ser negativo.")]
    public decimal HodometroFinal { get; set; }

    /// <summary>Opcional. Se omitida, usa o momento da requisição (UTC).</summary>
    public DateTime? DataFim { get; set; }

    [MaxLength(500, ErrorMessage = "As observações aceitam no máximo 500 caracteres.")]
    public string? Observacoes { get; set; }
}
