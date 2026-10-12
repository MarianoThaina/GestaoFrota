using System.Globalization;

namespace GestaoFrota.App.Helpers;

/// <summary>
/// Converte o texto digitado pelo usuário em decimal sem depender da cultura
/// do aparelho. Aceita "1.234,56", "1234,56", "1234.56" e "R$ 1.234,56".
/// Rejeita notação científica, sinais e números negativos.
/// </summary>
public static class ParserMoeda
{
    public static bool TentarConverter(string? texto, out decimal valor)
    {
        valor = 0m;
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        var limpo = texto.Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty)
            .Trim();

        if (limpo.Length == 0 || limpo.Any(c => !(char.IsDigit(c) || c == ',' || c == '.')))
        {
            return false;
        }

        var ultimaVirgula = limpo.LastIndexOf(',');
        var ultimoPonto = limpo.LastIndexOf('.');

        string normalizado;
        if (ultimaVirgula >= 0 && ultimoPonto >= 0)
        {
            // Os dois aparecem: o que vier por último é o separador decimal.
            var decimalEhVirgula = ultimaVirgula > ultimoPonto;
            normalizado = decimalEhVirgula
                ? limpo.Replace(".", string.Empty).Replace(',', '.')
                : limpo.Replace(",", string.Empty);
        }
        else if (ultimaVirgula >= 0)
        {
            if (limpo.Count(c => c == ',') > 1) return false;
            normalizado = limpo.Replace(',', '.');
        }
        else if (ultimoPonto >= 0)
        {
            var pontos = limpo.Count(c => c == '.');
            var casasAposPonto = limpo.Length - ultimoPonto - 1;

            // "1234.5" / "1234.56" => decimal; "1.234" / "1.234.567" => milhar.
            normalizado = pontos == 1 && casasAposPonto <= 2
                ? limpo
                : limpo.Replace(".", string.Empty);
        }
        else
        {
            normalizado = limpo;
        }

        return decimal.TryParse(normalizado, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out valor)
               && valor >= 0m;
    }
}
