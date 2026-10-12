using GestaoFrota.App.ViewModels;

namespace GestaoFrota.App.Views;

public partial class ComprovantePage : ContentPage
{
    private readonly ComprovanteViewModel _viewModel;

    public ComprovantePage(ComprovanteViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

}
