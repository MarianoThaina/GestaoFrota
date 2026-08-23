using GestaoFrota.Domain.Common;
using GestaoFrota.Domain.Enums;

namespace GestaoFrota.Domain.Entities;

/// <summary>
/// Categoria usada para classificar movimentações financeiras
/// (ex.: Combustível, Pedágio, Frete, Manutenção).
/// </summary>
public class Categoria : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;

    public TipoCategoria Tipo { get; set; }

    public bool Ativo { get; set; } = true;

    // Navegação
    public ICollection<Movimentacao> Movimentacoes { get; set; } = new List<Movimentacao>();
}
