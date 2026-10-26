using GestaoFrota.Domain.Services;

namespace GestaoFrota.API.Services;

/// <summary>
/// Período resolvido a partir dos filtros da US11/US12: últimos N meses
/// (padrão), tudo desde o primeiro registro, um mês/ano, um ano inteiro ou
/// um intervalo livre de datas. Início inclusivo, fim exclusivo.
/// </summary>
public readonly record struct PeriodoRelatorio(DateTime Inicio, DateTime Fim, string Rotulo)
{
    public const int MesesPadrao = 6;
    public const int MesesMaximo = 36;

    /// <summary>Último dia do período (inclusivo), para a resposta.</summary>
    public DateTime UltimoDia => Fim.AddDays(-1);

    /// <summary>
    /// Prioridade: intervalo livre (de/ate) > mês/ano (ano, mes) > ano inteiro
    /// (ano) > tudo > últimos N meses. <paramref name="primeiroRegistro"/> é a
    /// data mais antiga da base, usada só por "tudo".
    /// </summary>
    public static PeriodoRelatorio Resolver(
        DateTime hoje,
        int? meses,
        DateTime? de,
        DateTime? ate,
        int? ano,
        int? mes,
        bool tudo,
        DateTime? primeiroRegistro)
    {
        if (de.HasValue || ate.HasValue)
        {
            var inicio = (de ?? primeiroRegistro ?? hoje).Date;
            var ultimo = (ate ?? hoje).Date;
            if (ultimo < inicio)
            {
                (inicio, ultimo) = (ultimo, inicio);
            }

            return new PeriodoRelatorio(inicio, ultimo.AddDays(1), $"{inicio:dd/MM/yyyy} a {ultimo:dd/MM/yyyy}");
        }

        if (ano.HasValue && mes.HasValue)
        {
            var m = Math.Clamp(mes.Value, 1, 12);
            var inicio = new DateTime(ano.Value, m, 1);
            return new PeriodoRelatorio(inicio, inicio.AddMonths(1), $"{m:00}/{ano.Value}");
        }

        if (ano.HasValue)
        {
            var inicio = new DateTime(ano.Value, 1, 1);
            return new PeriodoRelatorio(inicio, inicio.AddYears(1), $"ano de {ano.Value}");
        }

        if (tudo)
        {
            var base_ = primeiroRegistro ?? hoje;
            var inicio = new DateTime(base_.Year, base_.Month, 1);
            return new PeriodoRelatorio(inicio, CalculadoraRelatorios.FimDoPeriodo(hoje), "todo o histórico");
        }

        var n = Math.Clamp(meses ?? MesesPadrao, 1, MesesMaximo);
        return new PeriodoRelatorio(
            CalculadoraRelatorios.InicioDoPeriodo(hoje, n),
            CalculadoraRelatorios.FimDoPeriodo(hoje),
            n == 1 ? "este mês" : $"últimos {n} meses");
    }

    /// <summary>Quantidade de meses cobertos (para a resposta manter o campo "meses").</summary>
    public int QuantidadeMeses =>
        Math.Max(1, (Fim.Year - Inicio.Year) * 12 + Fim.Month - Inicio.Month + (Fim.Day > 1 ? 1 : 0));
}
