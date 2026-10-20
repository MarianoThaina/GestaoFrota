using GestaoFrota.App.Helpers;

namespace GestaoFrota.App.Models;

/// <summary>Entradas e saídas de um mês, pela data de vencimento (US12).</summary>
public class FluxoMensal
{
    public int Ano { get; set; }
    public int Mes { get; set; }
    public string Rotulo { get; set; } = string.Empty;
    public decimal Entradas { get; set; }
    public decimal Saidas { get; set; }
    public decimal SaldoDoMes { get; set; }
    public decimal EntradasRealizadas { get; set; }
    public decimal SaidasRealizadas { get; set; }

    public string MesTexto => Formatacao.MesAno(Ano, Mes);
    public string MesCurto => Formatacao.MesAno(Ano, Mes).Split('/')[0];
    public string EntradasTexto => "+ " + Formatacao.Moeda(Entradas);
    public string SaidasTexto => "- " + Formatacao.Moeda(Saidas);
    public string SaldoDoMesTexto => Formatacao.Moeda(SaldoDoMes);
    public Color CorSaldo => SaldoDoMes >= 0 ? Color.FromArgb("#1E8E3E") : Color.FromArgb("#D93025");
}

/// <summary>Participação de uma categoria nas despesas do período (US12).</summary>
public class DespesaCategoria
{
    public string Categoria { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal Percentual { get; set; }

    /// <summary>Preenchida pelo ViewModel para a legenda casar com a rosca.</summary>
    public Color Cor { get; set; } = Colors.Gray;

    public string TotalTexto => Formatacao.Moeda(Total);
    public string PercentualTexto => $"{Percentual:0.#}%";
}

/// <summary>Cards do painel (US11).</summary>
public class IndicadoresRelatorio
{
    public decimal TotalEntradas { get; set; }
    public decimal TotalSaidas { get; set; }
    public decimal Resultado { get; set; }
    public decimal AReceberNoMes { get; set; }
    public decimal APagarNoMes { get; set; }
    public decimal EmAtraso { get; set; }
    public string? MaiorDespesaCategoria { get; set; }
    public decimal MaiorDespesaValor { get; set; }
    public decimal? VariacaoSaidasPercentual { get; set; }
    public int QuantidadeLancamentos { get; set; }

    public string TotalEntradasTexto => Formatacao.Moeda(TotalEntradas);
    public string TotalSaidasTexto => Formatacao.Moeda(TotalSaidas);
    public string ResultadoTexto => Formatacao.Moeda(Resultado);
    public Color CorResultado => Resultado >= 0 ? Color.FromArgb("#1E8E3E") : Color.FromArgb("#D93025");
    public string AReceberTexto => Formatacao.Moeda(AReceberNoMes);
    public string APagarTexto => Formatacao.Moeda(APagarNoMes);
    public string EmAtrasoTexto => Formatacao.Moeda(EmAtraso);
    public bool TemAtraso => EmAtraso > 0;

    public string MaiorDespesaTexto => string.IsNullOrWhiteSpace(MaiorDespesaCategoria)
        ? "Nenhuma despesa no período"
        : $"{MaiorDespesaCategoria} · {Formatacao.Moeda(MaiorDespesaValor)}";

    public string VariacaoTexto => VariacaoSaidasPercentual switch
    {
        null => "Sem base de comparação",
        > 0 => $"▲ {VariacaoSaidasPercentual:0.#}% em relação ao mês anterior",
        < 0 => $"▼ {Math.Abs(VariacaoSaidasPercentual.Value):0.#}% em relação ao mês anterior",
        _ => "Igual ao mês anterior"
    };

    public Color CorVariacao => VariacaoSaidasPercentual switch
    {
        > 0 => Color.FromArgb("#D93025"),
        < 0 => Color.FromArgb("#1E8E3E"),
        _ => Colors.Gray
    };
}

/// <summary>Resposta de GET /api/relatorios/resumo.</summary>
public class RelatorioResumo
{
    public int Meses { get; set; }
    public DateTime PeriodoInicio { get; set; }
    public DateTime PeriodoFim { get; set; }
    public IndicadoresRelatorio Indicadores { get; set; } = new();
    public List<FluxoMensal> FluxoMensal { get; set; } = new();
    public List<DespesaCategoria> DespesasPorCategoria { get; set; } = new();
    public DateTime GeradoEm { get; set; }

    public string PeriodoTexto => $"{Formatacao.MesAno(PeriodoInicio.Year, PeriodoInicio.Month)} a {Formatacao.MesAno(PeriodoFim.Year, PeriodoFim.Month)}";
}

/// <summary>Arquivo devolvido por GET /api/relatorios/exportar.</summary>
public record ArquivoExportado(string NomeArquivo, byte[] Conteudo, string ContentType);
