namespace GestaoFrota.API.DTOs;

public record FluxoMensalResponse(
    int Ano,
    int Mes,
    string Rotulo,
    decimal Entradas,
    decimal Saidas,
    decimal SaldoDoMes,
    decimal EntradasRealizadas,
    decimal SaidasRealizadas);

public record DespesaCategoriaResponse(string Categoria, decimal Total, decimal Percentual);

public record IndicadoresResponse(
    decimal TotalEntradas,
    decimal TotalSaidas,
    decimal Resultado,
    decimal AReceberNoMes,
    decimal APagarNoMes,
    decimal EmAtraso,
    string? MaiorDespesaCategoria,
    decimal MaiorDespesaValor,
    decimal? VariacaoSaidasPercentual,
    int QuantidadeLancamentos);

/// <summary>Resposta única da tela de relatórios: fluxo mensal, categorias e indicadores.</summary>
public record RelatorioResumoResponse(
    int Meses,
    DateTime PeriodoInicio,
    DateTime PeriodoFim,
    IndicadoresResponse Indicadores,
    IReadOnlyList<FluxoMensalResponse> FluxoMensal,
    IReadOnlyList<DespesaCategoriaResponse> DespesasPorCategoria,
    DateTime GeradoEm);
