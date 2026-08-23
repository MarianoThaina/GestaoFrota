using GestaoFrota.App.ViewModels;

namespace GestaoFrota.App.Views;

public partial class FormasPagamentoPage : ContentPage
{
    private readonly FormasPagamentoViewModel _viewModel;

    public FormasPagamentoPage(FormasPagamentoViewModel viewModel)
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
}
