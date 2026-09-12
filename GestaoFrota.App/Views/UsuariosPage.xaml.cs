using GestaoFrota.App.ViewModels;

namespace GestaoFrota.App.Views;

public partial class UsuariosPage : ContentPage
{
    private readonly UsuariosViewModel _viewModel;

    public UsuariosPage(UsuariosViewModel viewModel)
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