using GestaoFrota.App.ViewModels;

namespace GestaoFrota.App.Views;

public partial class ViagemFormPage : ContentPage
{
    private readonly ViagemFormViewModel _viewModel;

    public ViagemFormPage(ViagemFormViewModel viewModel)
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
