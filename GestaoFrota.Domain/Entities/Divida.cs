using GestaoFrota.Domain.Common;
using GestaoFrota.Domain.Enums;

namespace GestaoFrota.Domain.Entities;

/// <summary>
/// Dívida/financiamento com controle de parcelamento.
/// O valor da parcela é calculado automaticamente (ValorTotal / NumeroParcelas).
/// Operações de pagamento podem ser revertidas (rollback) via Movimentacao.
/// </summary>
public class Divida : EntidadeBase
{
    public string Descricao { get; set; } = string.Empty;

    public decimal ValorTotal { get; set; }

    public int NumeroParcelas { get; set; }

    public int ParcelasPagas { get; set; }

    public DateTime DataInicio { get; set; }

    public StatusDivida Status { get; set; } = StatusDivida.EmAberto;

    // Navegação
    public ICollection<Movimentacao> Movimentacoes { get; set; } = new List<Movimentacao>();

    /// <summary>
    /// Valor calculado automaticamente de cada parcela.
    /// </summary>
    public decimal ValorParcela => NumeroParcelas > 0 ? Math.Round(ValorTotal / NumeroParcelas, 2) : 0;

    public decimal SaldoDevedor => ValorTotal - (ValorParcela * ParcelasPagas);
}
