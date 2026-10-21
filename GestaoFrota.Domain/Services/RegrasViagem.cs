namespace GestaoFrota.Domain.Services;

/// <summary>
/// Regras puras do apontamento de hodômetro (US08), sem acesso a banco, no
/// mesmo modelo da CalculadoraFinanceira. A API chama antes de gravar e o
/// banco repete a checagem com o check constraint CK_Viagem_HodometroFinalMaiorQueInicial.
/// </summary>
public static class RegrasViagem
{
    /// <summary>
    /// Valida o encerramento. Devolve a mensagem de erro ou nulo quando está tudo certo.
    /// </summary>
    public static string? ValidarEncerramento(decimal hodometroInicial, decimal hodometroFinal)
    {
        if (hodometroFinal < 0)
        {
            return "O hodômetro final não pode ser negativo.";
        }

        if (hodometroFinal <= hodometroInicial)
        {
            return "O hodômetro final deve ser maior que o hodômetro inicial.";
        }

        return null;
    }

    /// <summary>Quilometragem rodada = final − inicial (US08), arredondada em 2 casas.</summary>
    public static decimal QuilometragemRodada(decimal hodometroInicial, decimal hodometroFinal) =>
        Math.Round(hodometroFinal - hodometroInicial, 2);
}
