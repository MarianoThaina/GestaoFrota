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

public record EstatisticaRotaResponse(
    Guid RotaId,
    string Nome,
    string Origem,
    string Destino,
    int Viagens,
    decimal Receitas,
    decimal Despesas,
    decimal Rentabilidade,
    decimal KmRodados,
    decimal? CustoPorKm,
    decimal RentabilidadeMediaPorViagem);

public record EstatisticaVeiculoResponse(
    Guid VeiculoId,
    string Placa,
    string PlacaFormatada,
    string Descricao,
    int Viagens,
    decimal KmRodados,
    decimal Receitas,
    decimal Despesas,
    decimal? CustoPorKm,
    decimal Litros,
    decimal? KmPorLitro);

/// <summary>
/// Estatísticas de rota e veículo (US10) e ranking (US13) das viagens iniciadas
/// no período. Lançamentos contam pelo vínculo com a viagem (US09).
/// </summary>
public record EstatisticasFrotaResponse(
    int Meses,
    DateTime PeriodoInicio,
    DateTime PeriodoFim,
    int VeiculosEmViagem,
    int VeiculosAtivos,
    int TotalViagens,
    int ViagensConcluidas,
    decimal KmRodados,
    decimal ReceitasVinculadas,
    decimal DespesasVinculadas,
    IReadOnlyList<EstatisticaRotaResponse> Rotas,
    IReadOnlyList<EstatisticaVeiculoResponse> Veiculos,
    IReadOnlyList<EstatisticaRotaResponse> RotasMaisLucrativas,
    IReadOnlyList<EstatisticaVeiculoResponse> VeiculosMaisEficientes,
    DateTime GeradoEm);

/// <summary>Resposta única da tela de relatórios: fluxo mensal, categorias e indicadores.</summary>
public record RelatorioResumoResponse(
    int Meses,
    DateTime PeriodoInicio,
    DateTime PeriodoFim,
    string PeriodoRotulo,
    IndicadoresResponse Indicadores,
    IReadOnlyList<FluxoMensalResponse> FluxoMensal,
    IReadOnlyList<DespesaCategoriaResponse> DespesasPorCategoria,
    DateTime GeradoEm);
