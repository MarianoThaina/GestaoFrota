using GestaoFrota.App.ViewModels;

namespace GestaoFrota.App.Views;

public partial class DividaFormPage : ContentPage
{
    private readonly DividaFormViewModel _viewModel;

    public DividaFormPage(DividaFormViewModel viewModel)
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
