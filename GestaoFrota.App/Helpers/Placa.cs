using System.Text.RegularExpressions;

namespace GestaoFrota.App.Helpers;

/// <summary>
/// Cópia da regra de placa do domínio (o app não referencia GestaoFrota.Domain):
/// formato antigo (AAA9999) ou Mercosul (AAA9A99), sempre normalizada em
/// maiúscula e sem hífen. A API valida de novo no servidor.
/// </summary>
public static partial class Placa
{
    [GeneratedRegex("^[A-Z]{3}[0-9]{4}$")]
    private static partial Regex FormatoAntigo();

    [GeneratedRegex("^[A-Z]{3}[0-9][A-Z][0-9]{2}$")]
    private static partial Regex FormatoMercosul();

    public static string Normalizar(string? placa) =>
        string.IsNullOrWhiteSpace(placa)
            ? string.Empty
            : placa.Trim().ToUpperInvariant().Replace("-", string.Empty).Replace(" ", string.Empty);

    public static bool EhValida(string? placa)
    {
        var normalizada = Normalizar(placa);
        return FormatoAntigo().IsMatch(normalizada) || FormatoMercosul().IsMatch(normalizada);
    }

    /// <summary>"ABC1234" vira "ABC-1234"; Mercosul fica "ABC1D23".</summary>
    public static string FormatarPlaca(this string? placa)
    {
        var normalizada = Normalizar(placa);
        return FormatoAntigo().IsMatch(normalizada)
            ? $"{normalizada[..3]}-{normalizada[3..]}"
            : normalizada;
    }
}
