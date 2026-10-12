using System.Globalization;

namespace GestaoFrota.App.Helpers;

/// <summary>Formatação em pt-BR usada pelas telas financeiras.</summary>
public static class Formatacao
{
    public static readonly CultureInfo PtBr = new("pt-BR");

    private static readonly string[] MesesAbreviados =
        { "Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez" };

    public static string Moeda(decimal valor) => valor.ToString("C2", PtBr);

    public static string Data(DateTime data) => data.ToString("dd/MM/yyyy", PtBr);

    public static string MesAno(int ano, int mes) => $"{MesesAbreviados[Math.Clamp(mes, 1, 12) - 1]}/{ano}";
}
