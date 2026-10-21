using GestaoFrota.App.ViewModels;

namespace GestaoFrota.App.Views;

public partial class RelatoriosPage : ContentPage
{
    private readonly RelatoriosViewModel _viewModel;

    public RelatoriosPage(RelatoriosViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CarregarCommand.ExecuteAsync(null);
    }

    /// <summary>Toque no gráfico de colunas: mostra o valor exato do mês (US12).</summary>
    private void OnColunasTocadas(object? sender, TouchEventArgs e)
    {
        if (sender is not GraphicsView grafico || e.Touches.Length == 0)
        {
            return;
        }

        var toque = e.Touches[0];
        _viewModel.TocarColunas((float)toque.X, (float)grafico.Width);
    }

    /// <summary>Toque na rosca: mostra o valor exato da categoria (US12).</summary>
    private void OnRoscaTocada(object? sender, TouchEventArgs e)
    {
        if (sender is not GraphicsView grafico || e.Touches.Length == 0)
        {
            return;
        }

        var toque = e.Touches[0];
        _viewModel.TocarRosca((float)toque.X, (float)toque.Y, (float)grafico.Width, (float)grafico.Height);
    }
}
