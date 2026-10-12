using GestaoFrota.Domain.Common;
using GestaoFrota.Domain.Enums;

namespace GestaoFrota.Domain.Entities;

/// <summary>
/// Dívida/financiamento com controle de parcelamento.
/// Ao ser salva, a API gera automaticamente NumeroParcelas movimentações
/// pendentes (uma por mês, a partir de DataPrimeiroVencimento) dentro de
/// uma transação: se qualquer parcela falhar, tudo é desfeito (rollback).
/// </summary>
public class Divida : EntidadeBase
{
    public string Descricao { get; set; } = string.Empty;

    /// <summary>Total a pagar no parcelamento (ValorParcela x NumeroParcelas).</summary>
    public decimal ValorTotal { get; set; }

    /// <summary>Valor de cada parcela, informado no cadastro.</summary>
    public decimal ValorParcela { get; set; }

    public int NumeroParcelas { get; set; }

    public int ParcelasPagas { get; set; }

    /// <summary>Data de contratação da dívida.</summary>
    public DateTime DataInicio { get; set; }

    /// <summary>Vencimento da 1ª parcela; as demais incrementam os meses.</summary>
    public DateTime DataPrimeiroVencimento { get; set; }

    /// <summary>Valor para quitar a dívida antecipadamente (informado pelo credor).</summary>
    public decimal? ValorQuitacaoAntecipada { get; set; }

    public StatusDivida Status { get; set; } = StatusDivida.EmAberto;

    // Navegação
    public ICollection<Movimentacao> Movimentacoes { get; set; } = new List<Movimentacao>();

    public decimal SaldoDevedor => ValorTotal - (ValorParcela * ParcelasPagas);
}
