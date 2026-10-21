using System.Text.RegularExpressions;

namespace GestaoFrota.Domain.Services;

/// <summary>
/// Regras de placa brasileira (US06): formato antigo (AAA9999) ou Mercosul
/// (AAA9A99). Guardamos sempre normalizada (maiúscula, sem hífen nem espaço)
/// para que o índice único do banco não deixe "ABC-1234" e "ABC1234" coexistirem.
/// </summary>
public static partial class ValidadorPlaca
{
    [GeneratedRegex("^[A-Z]{3}[0-9]{4}$")]
    private static partial Regex FormatoAntigo();

    [GeneratedRegex("^[A-Z]{3}[0-9][A-Z][0-9]{2}$")]
    private static partial Regex FormatoMercosul();

    /// <summary>Maiúscula, sem hífen e sem espaços. Nunca devolve nulo.</summary>
    public static string Normalizar(string? placa)
    {
        if (string.IsNullOrWhiteSpace(placa))
        {
            return string.Empty;
        }

        return placa.Trim().ToUpperInvariant().Replace("-", string.Empty).Replace(" ", string.Empty);
    }

    public static bool EhValida(string? placa)
    {
        var normalizada = Normalizar(placa);
        return FormatoAntigo().IsMatch(normalizada) || FormatoMercosul().IsMatch(normalizada);
    }

    public static bool EhMercosul(string? placa) => FormatoMercosul().IsMatch(Normalizar(placa));

    /// <summary>"ABC1234" vira "ABC-1234"; placa Mercosul fica como está ("ABC1D23").</summary>
    public static string Formatar(string? placa)
    {
        var normalizada = Normalizar(placa);
        return FormatoAntigo().IsMatch(normalizada)
            ? $"{normalizada[..3]}-{normalizada[3..]}"
            : normalizada;
    }
}
