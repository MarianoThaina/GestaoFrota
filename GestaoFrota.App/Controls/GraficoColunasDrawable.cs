using GestaoFrota.App.Models;

namespace GestaoFrota.App.Controls;

/// <summary>
/// Gráfico de colunas "Entradas e saídas" por mês (US12), desenhado com o
/// GraphicsView nativo do MAUI, sem biblioteca externa. Cada mês recebe um
/// par de colunas: verde para entradas, vermelho para saídas.
/// </summary>
public class GraficoColunasDrawable : IDrawable
{
    private static readonly Color CorEntrada = Color.FromArgb("#1E8E3E");
    private static readonly Color CorSaida = Color.FromArgb("#D93025");
    private static readonly Color CorGrade = Color.FromArgb("#DCE3EE");
    private static readonly Color CorTexto = Color.FromArgb("#5F6B7A");

    private readonly IReadOnlyList<FluxoMensal> _meses;

    public GraficoColunasDrawable(IReadOnlyList<FluxoMensal> meses)
    {
        _meses = meses;
    }

    public void Draw(ICanvas canvas, RectF area)
    {
        canvas.Antialias = true;

        const float margemEsquerda = 8f;
        const float margemDireita = 8f;
        const float margemTopo = 12f;
        const float alturaRotulos = 22f;

        var largura = area.Width - margemEsquerda - margemDireita;
        var alturaUtil = area.Height - margemTopo - alturaRotulos;

        if (_meses.Count == 0 || largura <= 0 || alturaUtil <= 0)
        {
            return;
        }

        var maximo = (float)_meses.Max(m => Math.Max(m.Entradas, m.Saidas));
        if (maximo <= 0f)
        {
            maximo = 1f;
        }

        // Linhas de grade horizontais (4 faixas) para dar escala ao olho.
        canvas.StrokeColor = CorGrade;
        canvas.StrokeSize = 1f;
        for (var i = 0; i <= 4; i++)
        {
            var y = margemTopo + alturaUtil * i / 4f;
            canvas.DrawLine(margemEsquerda, y, margemEsquerda + largura, y);
        }

        var larguraMes = largura / _meses.Count;
        var larguraColuna = Math.Clamp(larguraMes * 0.32f, 6f, 28f);
        var espacoEntrePar = Math.Min(4f, larguraColuna * 0.3f);

        canvas.FontSize = 11f;
        canvas.FontColor = CorTexto;

        for (var i = 0; i < _meses.Count; i++)
        {
            var mes = _meses[i];
            var centroX = margemEsquerda + larguraMes * i + larguraMes / 2f;
            var baseY = margemTopo + alturaUtil;

            var alturaEntrada = alturaUtil * (float)mes.Entradas / maximo;
            var alturaSaida = alturaUtil * (float)mes.Saidas / maximo;

            var xEntrada = centroX - espacoEntrePar / 2f - larguraColuna;
            var xSaida = centroX + espacoEntrePar / 2f;

            DesenharColuna(canvas, xEntrada, baseY, larguraColuna, alturaEntrada, CorEntrada);
            DesenharColuna(canvas, xSaida, baseY, larguraColuna, alturaSaida, CorSaida);

            canvas.DrawString(
                mes.MesCurto,
                centroX - larguraMes / 2f, baseY + 4f, larguraMes, alturaRotulos - 4f,
                HorizontalAlignment.Center, VerticalAlignment.Top);
        }
    }

    private static void DesenharColuna(ICanvas canvas, float x, float baseY, float largura, float altura, Color cor)
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

        canvas.FillColor = cor;
        canvas.FillRoundedRectangle(x, baseY - altura, largura, altura, 3f);
    }
}
