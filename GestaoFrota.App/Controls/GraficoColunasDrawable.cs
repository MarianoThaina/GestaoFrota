using GestaoFrota.App.Helpers;
using GestaoFrota.App.Models;

namespace GestaoFrota.App.Controls;

/// <summary>
/// Gráfico de colunas "Entradas e saídas" por mês (US12), desenhado com o
/// GraphicsView nativo do MAUI, sem biblioteca externa. Cada mês recebe um
/// par de colunas: verde para entradas, vermelho para saídas. Um mês pode
/// estar selecionado (toque): ele ganha fundo destacado e os valores exatos
/// acima das colunas; os outros ficam esmaecidos.
/// </summary>
public class GraficoColunasDrawable : IDrawable
{
    private const float MargemEsquerda = 8f;
    private const float MargemDireita = 8f;
    private const float MargemTopo = 24f;
    private const float AlturaRotulos = 22f;

    private static readonly Color CorEntrada = Color.FromArgb("#1E8E3E");
    private static readonly Color CorSaida = Color.FromArgb("#D93025");
    private static readonly Color CorGrade = Color.FromArgb("#DCE3EE");
    private static readonly Color CorTexto = Color.FromArgb("#5F6B7A");
    private static readonly Color CorDestaque = Color.FromArgb("#EEF2F8");

    private readonly IReadOnlyList<FluxoMensal> _meses;
    private readonly int? _selecionado;

    public GraficoColunasDrawable(IReadOnlyList<FluxoMensal> meses, int? selecionado = null)
    {
        _meses = meses;
        _selecionado = selecionado is >= 0 && selecionado < meses.Count ? selecionado : null;
    }

    /// <summary>
    /// Converte a coordenada X de um toque no índice do mês tocado, usando a
    /// mesma geometria do desenho. Nulo quando o toque cai fora das colunas.
    /// </summary>
    public static int? IndiceNoToque(float x, float larguraTotal, int quantidadeMeses)
    {
        var largura = larguraTotal - MargemEsquerda - MargemDireita;
        if (quantidadeMeses <= 0 || largura <= 0 || x < MargemEsquerda || x > MargemEsquerda + largura)
        {
            return null;
        }

        var indice = (int)((x - MargemEsquerda) / (largura / quantidadeMeses));
        return Math.Clamp(indice, 0, quantidadeMeses - 1);
    }

    public void Draw(ICanvas canvas, RectF area)
    {
        canvas.Antialias = true;

        var largura = area.Width - MargemEsquerda - MargemDireita;
        var alturaUtil = area.Height - MargemTopo - AlturaRotulos;

        if (_meses.Count == 0 || largura <= 0 || alturaUtil <= 0)
        {
            return;
        }

        var maximo = (float)_meses.Max(m => Math.Max(m.Entradas, m.Saidas));
        if (maximo <= 0f)
        {
            maximo = 1f;
        }

        var larguraMes = largura / _meses.Count;
        var larguraColuna = Math.Clamp(larguraMes * 0.32f, 4f, 28f);
        var espacoEntrePar = Math.Min(4f, larguraColuna * 0.3f);
        var baseY = MargemTopo + alturaUtil;

        // Fundo do mês selecionado, atrás da grade.
        if (_selecionado is { } sel)
        {
            canvas.FillColor = CorDestaque;
            canvas.FillRoundedRectangle(MargemEsquerda + larguraMes * sel, 4f, larguraMes, area.Height - 8f, 6f);
        }

        // Linhas de grade horizontais (4 faixas) para dar escala ao olho.
        canvas.StrokeColor = CorGrade;
        canvas.StrokeSize = 1f;
        for (var i = 0; i <= 4; i++)
        {
            var y = MargemTopo + alturaUtil * i / 4f;
            canvas.DrawLine(MargemEsquerda, y, MargemEsquerda + largura, y);
        }

        // Com muitos meses (1 ano, 2 anos, tudo), mostra um rótulo sim, outro não.
        var passoRotulo = _meses.Count > 12 ? (int)Math.Ceiling(_meses.Count / 12.0) : 1;

        canvas.FontSize = 11f;
        canvas.FontColor = CorTexto;

        for (var i = 0; i < _meses.Count; i++)
        {
            var mes = _meses[i];
            var centroX = MargemEsquerda + larguraMes * i + larguraMes / 2f;
            var esmaecido = _selecionado.HasValue && _selecionado.Value != i;

            var alturaEntrada = alturaUtil * (float)mes.Entradas / maximo;
            var alturaSaida = alturaUtil * (float)mes.Saidas / maximo;

            var xEntrada = centroX - espacoEntrePar / 2f - larguraColuna;
            var xSaida = centroX + espacoEntrePar / 2f;

            DesenharColuna(canvas, xEntrada, baseY, larguraColuna, alturaEntrada, CorEntrada, esmaecido);
            DesenharColuna(canvas, xSaida, baseY, larguraColuna, alturaSaida, CorSaida, esmaecido);

            if (i % passoRotulo == 0 || _selecionado == i)
            {
                canvas.FontColor = _selecionado == i ? Color.FromArgb("#0B1D3A") : CorTexto;
                canvas.DrawString(
                    _meses.Count > 12 ? mes.MesCurto : mes.MesCurto,
                    centroX - larguraMes / 2f, baseY + 4f, larguraMes, AlturaRotulos - 4f,
                    HorizontalAlignment.Center, VerticalAlignment.Top);
            }
        }

        // Valores exatos do mês tocado, acima do par de colunas.
        if (_selecionado is { } s)
        {
            var mes = _meses[s];
            var centroX = MargemEsquerda + larguraMes * s + larguraMes / 2f;
            var topoMaisAlto = baseY - alturaUtil * (float)Math.Max(mes.Entradas, mes.Saidas) / maximo;
            var y = Math.Max(2f, topoMaisAlto - 20f);

            canvas.FontSize = 10f;
            canvas.FontColor = Color.FromArgb("#0B1D3A");
            canvas.DrawString(
                $"+{Formatacao.Moeda(mes.Entradas)}  −{Formatacao.Moeda(mes.Saidas)}",
                centroX - 120f, y, 240f, 16f,
                HorizontalAlignment.Center, VerticalAlignment.Top);
        }
    }

    private static void DesenharColuna(ICanvas canvas, float x, float baseY, float largura, float altura, Color cor, bool esmaecido)
    {
        // Altura mínima visível para meses com valor pequeno, mas não zero.
        if (altura > 0f && altura < 2f)
        {
            altura = 2f;
        }

        if (altura <= 0f)
        {
            return;
        }

        canvas.FillColor = esmaecido ? cor.WithAlpha(0.35f) : cor;
        canvas.FillRoundedRectangle(x, baseY - altura, largura, altura, 3f);
    }
}
