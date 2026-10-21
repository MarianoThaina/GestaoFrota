using GestaoFrota.App.ViewModels;

namespace GestaoFrota.App.Views;

public partial class ViagensPage : ContentPage
{
    private readonly ViagensViewModel _viewModel;

    public ViagensPage(ViagensViewModel viewModel)
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
