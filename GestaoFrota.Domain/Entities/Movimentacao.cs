using GestaoFrota.Domain.Common;
using GestaoFrota.Domain.Enums;

namespace GestaoFrota.Domain.Entities;

/// <summary>
/// Movimentação financeira (receita ou despesa). É a entidade central
/// do módulo financeiro: alimenta o motor de saldo e as projeções.
/// Pode estar vinculada a uma viagem (frete, abastecimento) e/ou a uma dívida (parcela).
/// Suporta rollback (estorno) via campo Estornada.
/// </summary>
public class Movimentacao : EntidadeBase
{
    public TipoMovimentacao Tipo { get; set; }

    public decimal Valor { get; set; }

    public DateTime Data { get; set; } = DateTime.UtcNow;

    public string? Descricao { get; set; }

    public string? ComprovanteUrl { get; set; }

    public Guid CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public Guid FormaPagamentoId { get; set; }
    public FormaPagamento? FormaPagamento { get; set; }

    public Guid UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public Guid? ViagemId { get; set; }
    public Viagem? Viagem { get; set; }

    public Guid? DividaId { get; set; }
    public Divida? Divida { get; set; }

    /// <summary>
    /// Indica se a movimentação foi estornada (rollback), mantendo
    /// o histórico íntegro em vez de excluir o registro.
    /// </summary>
    public bool Estornada { get; set; }

    public bool SincronizadoOffline { get; set; } = true;
}
