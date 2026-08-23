namespace GestaoFrota.Domain.Common;

/// <summary>
/// Classe base para todas as entidades do domínio, garantindo
/// identidade única e rastreabilidade de criação/atualização.
/// </summary>
public abstract class EntidadeBase
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public DateTime? DataAtualizacao { get; set; }
}
