namespace GestaoFrota.Domain.Enums;

/// <summary>
/// Situação de uma movimentação: Pendente (ainda não paga/recebida) ou
/// Pago (paga ou recebida, com data de pagamento registrada).
/// </summary>
public enum StatusMovimentacao
{
    Pendente = 1,
    Pago = 2
}
