using GestaoFrota.App.ViewModels;

namespace GestaoFrota.App.Views;

public partial class MovimentacoesPage : ContentPage
{
    private readonly MovimentacoesViewModel _viewModel;

    public MovimentacoesPage(MovimentacoesViewModel viewModel)
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
