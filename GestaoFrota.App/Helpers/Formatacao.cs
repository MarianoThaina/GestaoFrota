using System.Globalization;

namespace GestaoFrota.App.Helpers;

/// <summary>Formatação em pt-BR usada pelas telas financeiras e de frota.</summary>
public static class Formatacao
{
    public static readonly CultureInfo PtBr = new("pt-BR");

    private static readonly string[] MesesAbreviados =
        { "Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez" };

    public static string Moeda(decimal valor) => valor.ToString("C2", PtBr);

    public static string Data(DateTime data) => data.ToString("dd/MM/yyyy", PtBr);

    public static string MesAno(int ano, int mes) => $"{MesesAbreviados[Math.Clamp(mes, 1, 12) - 1]}/{ano}";

    /// <summary>"184.320 km" (sem casas quando é inteiro; 1 casa quando não é).</summary>
    public static string Km(decimal km) =>
        (km == decimal.Truncate(km) ? km.ToString("N0", PtBr) : km.ToString("N1", PtBr)) + " km";

    /// <summary>"32 t" ou "27,5 t".</summary>
    public static string Toneladas(decimal toneladas) =>
        (toneladas == decimal.Truncate(toneladas) ? toneladas.ToString("N0", PtBr) : toneladas.ToString("N1", PtBr)) + " t";
}
