using GestaoFrota.App.ViewModels;

namespace GestaoFrota.App.Views;

public partial class MovimentacaoFormPage : ContentPage
{
    private readonly MovimentacaoFormViewModel _viewModel;

    public MovimentacaoFormPage(MovimentacaoFormViewModel viewModel)
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
