using GestaoFrota.App.Helpers;

namespace GestaoFrota.App.Models;

public enum TipoFiltroPeriodo
{
    /// <summary>Últimos N meses, incluindo o atual (3, 6, 12, 24).</summary>
    UltimosMeses = 1,

    /// <summary>Desde o primeiro registro da base.</summary>
    Tudo = 2,

    /// <summary>Um ano inteiro.</summary>
    Ano = 3,

    /// <summary>Um mês de um ano.</summary>
    MesAno = 4,

    /// <summary>Intervalo livre de datas.</summary>
    Livre = 5
}

/// <summary>
/// Filtro de período da tela de Relatórios (US11/US12). Vira query string
/// para /api/relatorios/resumo, /frota e /exportar, que aceitam os mesmos
/// parâmetros.
/// </summary>
public record FiltroPeriodo(
    TipoFiltroPeriodo Tipo,
    int Meses = 6,
    int Ano = 0,
    int Mes = 0,
    DateTime? De = null,
    DateTime? Ate = null)
{
    public static FiltroPeriodo UltimosMeses(int meses) => new(TipoFiltroPeriodo.UltimosMeses, meses);
    public static FiltroPeriodo Tudo() => new(TipoFiltroPeriodo.Tudo);
    public static FiltroPeriodo DoAno(int ano) => new(TipoFiltroPeriodo.Ano, Ano: ano);
    public static FiltroPeriodo DoMes(int ano, int mes) => new(TipoFiltroPeriodo.MesAno, Ano: ano, Mes: mes);
    public static FiltroPeriodo Entre(DateTime de, DateTime ate) => new(TipoFiltroPeriodo.Livre, De: de.Date, Ate: ate.Date);

    public string ParaQuery() => Tipo switch
    {
        TipoFiltroPeriodo.Tudo => "tudo=true",
        TipoFiltroPeriodo.Ano => $"ano={Ano}",
        TipoFiltroPeriodo.MesAno => $"ano={Ano}&mes={Mes}",
        TipoFiltroPeriodo.Livre => $"de={De:yyyy-MM-dd}&ate={Ate:yyyy-MM-dd}",
        _ => $"meses={Meses}"
    };

    /// <summary>Texto curto para o botão/legenda enquanto a resposta não chega.</summary>
    public string Rotulo => Tipo switch
    {
        TipoFiltroPeriodo.Tudo => "Todo o histórico",
        TipoFiltroPeriodo.Ano => $"Ano de {Ano}",
        TipoFiltroPeriodo.MesAno => Formatacao.MesAno(Ano, Mes),
        TipoFiltroPeriodo.Livre => $"{Formatacao.Data(De ?? DateTime.Today)} a {Formatacao.Data(Ate ?? DateTime.Today)}",
        _ => Meses switch
        {
            12 => "Último ano",
            24 => "Últimos 2 anos",
            _ => $"Últimos {Meses} meses"
        }
    };
}
