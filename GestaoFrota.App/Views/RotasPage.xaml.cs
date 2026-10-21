using GestaoFrota.App.ViewModels;

namespace GestaoFrota.App.Views;

public partial class RotasPage : ContentPage
{
    private readonly RotasViewModel _viewModel;

    public RotasPage(RotasViewModel viewModel)
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
