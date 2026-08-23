using GestaoFrota.Domain.Common;

namespace GestaoFrota.Domain.Entities;

/// <summary>
/// Forma de pagamento usada nas movimentações financeiras
/// (ex.: Pix, Boleto, Cartão, Dinheiro).
/// </summary>
public class FormaPagamento : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;

    // Navegação
    public ICollection<Movimentacao> Movimentacoes { get; set; } = new List<Movimentacao>();
}
