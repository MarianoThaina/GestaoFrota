namespace GestaoFrota.API.DTOs;

public record SaldoResponse(
    decimal SaldoAtual,
    decimal EntradasPagas,
    decimal SaidasPagas,
    decimal AReceber,
    decimal APagar,
    DateTime CalculadoEm);

public record ProjecaoMesResponse(
    int Ano,
    int Mes,
    string Rotulo,
    decimal EntradasPrevistas,
    decimal SaidasPrevistas,
    decimal SaldoDoMes,
    decimal SaldoProjetado);

public record ProjecaoResponse(decimal SaldoAtual, IReadOnlyList<ProjecaoMesResponse> Meses);
