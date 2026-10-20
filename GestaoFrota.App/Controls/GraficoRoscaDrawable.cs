using GestaoFrota.App.Models;

namespace GestaoFrota.App.Controls;

/// <summary>
/// Rosca "Despesas por categoria" (US12). Cada fatia usa a cor já atribuída
/// ao item pelo ViewModel, que é a mesma da legenda ao lado.
/// </summary>
public class GraficoRoscaDrawable : IDrawable
{
    private static readonly Color CorVazio = Color.FromArgb("#DCE3EE");
    private static readonly Color CorTexto = Color.FromArgb("#0B1D3A");

    private readonly IReadOnlyList<DespesaCategoria> _categorias;
    private readonly string _textoCentral;

    public GraficoRoscaDrawable(IReadOnlyList<DespesaCategoria> categorias, string textoCentral)
    {
        _categorias = categorias;
        _textoCentral = textoCentral;
    }

    public void Draw(ICanvas canvas, RectF area)
    {
        canvas.Antialias = true;

        var diametro = Math.Min(area.Width, area.Height) - 8f;
        if (diametro <= 0f)
        {
            return;
        }

        var espessura = Math.Max(12f, diametro * 0.18f);
        var raioExterno = diametro / 2f;
        var centroX = area.Center.X;
        var centroY = area.Center.Y;

        // O arco é traçado pelo meio da espessura, então o retângulo encolhe metade dela.
        var raioArco = raioExterno - espessura / 2f;
        var retangulo = new RectF(centroX - raioArco, centroY - raioArco, raioArco * 2f, raioArco * 2f);

        canvas.StrokeSize = espessura;
        canvas.StrokeLineCap = LineCap.Butt;

        var total = _categorias.Sum(c => c.Total);

        if (total <= 0m)
        {
            canvas.StrokeColor = CorVazio;
            canvas.DrawEllipse(retangulo);
        }
        else
        {
            // Começa no topo (90°) e segue no sentido horário.
            var anguloInicio = 90f;

            foreach (var categoria in _categorias)
            {
                var fracao = (float)(categoria.Total / total);
                var varredura = 360f * fracao;
                if (varredura <= 0f)
                {
                    continue;
                }

                var anguloFim = anguloInicio - varredura;

                canvas.StrokeColor = categoria.Cor;
                canvas.DrawArc(retangulo, anguloInicio, anguloFim, clockwise: true, closed: false);

                anguloInicio = anguloFim;
            }
        }

        // Texto central: total de despesas do período.
        var raioInterno = raioExterno - espessura;
        canvas.FontColor = CorTexto;
        canvas.FontSize = Math.Clamp(raioInterno * 0.22f, 10f, 16f);
        canvas.DrawString(
            _textoCentral,
            centroX - raioInterno, centroY - raioInterno, raioInterno * 2f, raioInterno * 2f,
            HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}
