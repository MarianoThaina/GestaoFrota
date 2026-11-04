using GestaoFrota.Domain.Enums;

namespace GestaoFrota.API.Services;

/// <summary>
/// Uma movimentação do período, já com os nomes de categoria e forma de
/// pagamento. É a mesma lista para o CSV e para o PDF da US14, para os dois
/// formatos nunca divergirem nos números.
/// </summary>
public record LinhaExportacao(
    DateTime DataVencimento,
    TipoMovimentacao Tipo,
    string? Descricao,
    string Categoria,
    string FormaPagamento,
    StatusMovimentacao Status,
    DateTime? DataPagamento,
    decimal Valor);

/// <summary>Totais da exportação, calculados uma vez sobre as linhas.</summary>
public readonly record struct TotaisExportacao(decimal Entradas, decimal Saidas)
{
    public decimal Resultado => Entradas - Saidas;

    public static TotaisExportacao De(IEnumerable<LinhaExportacao> linhas)
    {
        decimal entradas = 0, saidas = 0;
        foreach (var l in linhas)
        {
            if (l.Tipo == TipoMovimentacao.Receita)
            {
                entradas += l.Valor;
            }
            else
            {
                saidas += l.Valor;
            }
        }

        return new TotaisExportacao(entradas, saidas);
    }
}
