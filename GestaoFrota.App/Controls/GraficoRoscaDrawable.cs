using GestaoFrota.App.Models;

namespace GestaoFrota.App.Controls;

/// <summary>
/// Rosca "Despesas por categoria" (US12). Cada fatia usa a cor já atribuída
/// ao item pelo ViewModel, que é a mesma da legenda ao lado. Uma fatia pode
/// estar selecionada (toque): as outras esmaecem e o centro mostra o valor dela.
/// </summary>
public class GraficoRoscaDrawable : IDrawable
{
    private static readonly Color CorVazio = Color.FromArgb("#DCE3EE");
    private static readonly Color CorTexto = Color.FromArgb("#0B1D3A");

    private readonly IReadOnlyList<DespesaCategoria> _categorias;
    private readonly string _textoCentral;
    private readonly int? _selecionado;

    public GraficoRoscaDrawable(IReadOnlyList<DespesaCategoria> categorias, string textoCentral, int? selecionado = null)
    {
        _categorias = categorias;
        _textoCentral = textoCentral;
        _selecionado = selecionado is >= 0 && selecionado < categorias.Count ? selecionado : null;
    }

    /// <summary>
    /// Converte um toque (x, y) na área do gráfico no índice da fatia tocada,
    /// medindo o ângulo a partir do topo no sentido horário, como o desenho.
    /// Nulo quando o toque cai no buraco do meio ou fora da rosca.
    /// </summary>
    public static int? IndiceNoToque(float x, float y, float largura, float altura, IReadOnlyList<DespesaCategoria> categorias)
    {
        var total = categorias.Sum(c => c.Total);
        var diametro = Math.Min(largura, altura) - 8f;
        if (total <= 0m || diametro <= 0f)
        {
            return null;
        }

        var raioExterno = diametro / 2f;
        var espessura = Math.Max(12f, diametro * 0.18f);
        var dx = x - largura / 2f;
        var dy = y - altura / 2f;
        var distancia = Math.Sqrt(dx * dx + dy * dy);

        if (distancia > raioExterno + 6f || distancia < raioExterno - espessura - 6f)
        {
            return null;
        }

        // Ângulo horário a partir do topo, em [0, 360).
        var angulo = (Math.Atan2(dx, -dy) * 180.0 / Math.PI + 360.0) % 360.0;

        var acumulado = 0.0;
        for (var i = 0; i < categorias.Count; i++)
        {
            acumulado += (double)(categorias[i].Total / total) * 360.0;
            if (angulo < acumulado)
            {
                return i;
            }
        }

        return categorias.Count - 1;
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

            for (var i = 0; i < _categorias.Count; i++)
            {
                var categoria = _categorias[i];
                var fracao = (float)(categoria.Total / total);
                var varredura = 360f * fracao;
                if (varredura <= 0f)
                {
                    continue;
                }

                var anguloFim = anguloInicio - varredura;
                var esmaecida = _selecionado.HasValue && _selecionado.Value != i;

                canvas.StrokeColor = esmaecida ? categoria.Cor.WithAlpha(0.3f) : categoria.Cor;
                canvas.DrawArc(retangulo, anguloInicio, anguloFim, clockwise: true, closed: false);

                anguloInicio = anguloFim;
            }
        }

        // Texto central: total do período, ou a categoria tocada.
        var raioInterno = raioExterno - espessura;
        var texto = _selecionado is { } s
            ? $"{_categorias[s].Categoria}\n{_categorias[s].PercentualTexto}"
            : _textoCentral;

        canvas.FontColor = CorTexto;
        canvas.FontSize = Math.Clamp(raioInterno * (_selecionado.HasValue ? 0.18f : 0.22f), 9f, 16f);
        canvas.DrawString(
            texto,
            centroX - raioInterno, centroY - raioInterno, raioInterno * 2f, raioInterno * 2f,
            HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}
