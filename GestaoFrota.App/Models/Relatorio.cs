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

/// <summary>Rota agregada pelas viagens do período (US10/US13).</summary>
public class EstatisticaRota
{
    public Guid RotaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Origem { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public int Viagens { get; set; }
    public decimal Receitas { get; set; }
    public decimal Despesas { get; set; }
    public decimal Rentabilidade { get; set; }
    public decimal KmRodados { get; set; }
    public decimal? CustoPorKm { get; set; }
    public decimal RentabilidadeMediaPorViagem { get; set; }

    /// <summary>Posição no ranking (1, 2, 3), preenchida pelo ViewModel.</summary>
    public int Posicao { get; set; }

    public string PosicaoTexto => $"{Posicao}º";
    public string TrajetoTexto => $"{Origem} → {Destino}";
    public string RentabilidadeTexto => Formatacao.Moeda(Rentabilidade);
    public Color CorRentabilidade => Rentabilidade >= 0 ? Color.FromArgb("#1E8E3E") : Color.FromArgb("#D93025");
    public string DetalheTexto =>
        $"{Viagens} {(Viagens == 1 ? "viagem" : "viagens")} · {Formatacao.Km(KmRodados)} · " +
        (CustoPorKm.HasValue ? $"{Formatacao.Moeda(CustoPorKm.Value)}/km" : "custo/km indisponível");
}

/// <summary>Veículo agregado pelas viagens do período (US10/US13).</summary>
public class EstatisticaVeiculo
{
    public Guid VeiculoId { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string PlacaFormatada { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int Viagens { get; set; }
    public decimal KmRodados { get; set; }
    public decimal Receitas { get; set; }
    public decimal Despesas { get; set; }
    public decimal? CustoPorKm { get; set; }
    public decimal Litros { get; set; }
    public decimal? KmPorLitro { get; set; }

    public int Posicao { get; set; }

    public string PosicaoTexto => $"{Posicao}º";
    public string CustoPorKmTexto => CustoPorKm.HasValue ? $"{Formatacao.Moeda(CustoPorKm.Value)}/km" : "—";
    public string KmPorLitroTexto => KmPorLitro.HasValue
        ? $"{KmPorLitro.Value.ToString("N2", Formatacao.PtBr)} km/L"
        : "sem abastecimento vinculado";
    public string DetalheTexto =>
        $"{Viagens} {(Viagens == 1 ? "viagem" : "viagens")} · {Formatacao.Km(KmRodados)} · {KmPorLitroTexto}";
}

/// <summary>Resposta de GET /api/relatorios/frota.</summary>
public class EstatisticasFrota
{
    public int Meses { get; set; }
    public DateTime PeriodoInicio { get; set; }
    public DateTime PeriodoFim { get; set; }
    public int VeiculosEmViagem { get; set; }
    public int VeiculosAtivos { get; set; }
    public int TotalViagens { get; set; }
    public int ViagensConcluidas { get; set; }
    public decimal KmRodados { get; set; }
    public decimal ReceitasVinculadas { get; set; }
    public decimal DespesasVinculadas { get; set; }
    public List<EstatisticaRota> Rotas { get; set; } = new();
    public List<EstatisticaVeiculo> Veiculos { get; set; } = new();
    public List<EstatisticaRota> RotasMaisLucrativas { get; set; } = new();
    public List<EstatisticaVeiculo> VeiculosMaisEficientes { get; set; } = new();
    public DateTime GeradoEm { get; set; }

    public string ResumoTexto =>
        $"{TotalViagens} {(TotalViagens == 1 ? "viagem" : "viagens")} no período ({ViagensConcluidas} concluídas) · {Formatacao.Km(KmRodados)} rodados";
    public string RentabilidadeTexto => Formatacao.Moeda(ReceitasVinculadas - DespesasVinculadas);
    public Color CorRentabilidade => ReceitasVinculadas - DespesasVinculadas >= 0 ? Color.FromArgb("#1E8E3E") : Color.FromArgb("#D93025");
    public string VeiculosEmViagemTexto => $"{VeiculosEmViagem} de {VeiculosAtivos}";
}

/// <summary>Arquivo devolvido por GET /api/relatorios/exportar.</summary>
public record ArquivoExportado(string NomeArquivo, byte[] Conteudo, string ContentType);
