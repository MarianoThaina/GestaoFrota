using GestaoFrota.App.Helpers;

namespace GestaoFrota.App.Models;

public enum StatusDivida
{
    EmAberto = 1,
    QuitadaParcialmente = 2,
    Quitada = 3
}

public class Divida
{
    public Guid Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal ValorTotal { get; set; }
    public decimal ValorParcela { get; set; }
    public int NumeroParcelas { get; set; }
    public int ParcelasPagas { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime DataPrimeiroVencimento { get; set; }
    public decimal? ValorQuitacaoAntecipada { get; set; }
    public StatusDivida Status { get; set; }
    public decimal SaldoDevedor { get; set; }

    public string ParcelaTexto => $"{NumeroParcelas}x de {Formatacao.Moeda(ValorParcela)}";

    public string ProgressoTexto => $"{ParcelasPagas}/{NumeroParcelas} parcelas pagas";

    public double Progresso => NumeroParcelas > 0 ? (double)ParcelasPagas / NumeroParcelas : 0d;

    public string SaldoDevedorTexto => $"Saldo devedor: {Formatacao.Moeda(SaldoDevedor)}";

    public string PrimeiroVencimentoTexto => $"1ª parcela em {Formatacao.Data(DataPrimeiroVencimento)}";

    public string QuitacaoTexto => ValorQuitacaoAntecipada.HasValue
        ? $"Quitação antecipada: {Formatacao.Moeda(ValorQuitacaoAntecipada.Value)}"
        : "Quitação antecipada: não informada";

    public string StatusTexto => Status switch
    {
        StatusDivida.Quitada => "Quitada",
        StatusDivida.QuitadaParcialmente => "Em pagamento",
        _ => "Em aberto"
    };

    public Color CorStatus => Status switch
    {
        StatusDivida.Quitada => Color.FromArgb("#1E8E3E"),
        StatusDivida.QuitadaParcialmente => Color.FromArgb("#2C6CE0"),
        _ => Color.FromArgb("#E8710A")
    };
}

public class DividaDetalhe
{
    public Divida Divida { get; set; } = new();
    public List<Movimentacao> Parcelas { get; set; } = new();
}
