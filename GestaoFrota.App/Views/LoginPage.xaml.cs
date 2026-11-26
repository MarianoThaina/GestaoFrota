using GestaoFrota.App.ViewModels;

namespace GestaoFrota.App.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnAjudaClicked(object? sender, EventArgs e) => await Helpers.CentralDeAjuda.AbrirAsync();
}
