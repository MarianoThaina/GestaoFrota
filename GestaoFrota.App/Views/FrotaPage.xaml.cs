using GestaoFrota.App.ViewModels;

namespace GestaoFrota.App.Views;

public partial class FrotaPage : ContentPage
{
    private readonly FrotaViewModel _viewModel;

    public FrotaPage(FrotaViewModel viewModel)
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
