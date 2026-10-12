using GestaoFrota.App.Helpers;

namespace GestaoFrota.App.Models;

public class SaldoResumo
{
    public decimal SaldoAtual { get; set; }
    public decimal EntradasPagas { get; set; }
    public decimal SaidasPagas { get; set; }
    public decimal AReceber { get; set; }
    public decimal APagar { get; set; }

    public string SaldoAtualTexto => Formatacao.Moeda(SaldoAtual);
    public string EntradasPagasTexto => Formatacao.Moeda(EntradasPagas);
    public string SaidasPagasTexto => Formatacao.Moeda(SaidasPagas);
    public string AReceberTexto => Formatacao.Moeda(AReceber);
    public string APagarTexto => Formatacao.Moeda(APagar);

    public Color CorSaldo => SaldoAtual >= 0 ? Color.FromArgb("#1E8E3E") : Color.FromArgb("#D93025");
}

public class ProjecaoMes
{
    public int Ano { get; set; }
    public int Mes { get; set; }
    public string Rotulo { get; set; } = string.Empty;
    public decimal EntradasPrevistas { get; set; }
    public decimal SaidasPrevistas { get; set; }
    public decimal SaldoDoMes { get; set; }
    public decimal SaldoProjetado { get; set; }

    public string MesTexto => Formatacao.MesAno(Ano, Mes);
    public string EntradasTexto => "+ " + Formatacao.Moeda(EntradasPrevistas);
    public string SaidasTexto => "- " + Formatacao.Moeda(SaidasPrevistas);
    public string SaldoProjetadoTexto => Formatacao.Moeda(SaldoProjetado);
    public Color CorSaldoProjetado => SaldoProjetado >= 0 ? Color.FromArgb("#1E8E3E") : Color.FromArgb("#D93025");
}

public class ProjecaoResposta
{
    public decimal SaldoAtual { get; set; }
    public List<ProjecaoMes> Meses { get; set; } = new();
}
