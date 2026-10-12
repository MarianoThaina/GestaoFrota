using GestaoFrota.App.ViewModels;

namespace GestaoFrota.App.Views;

public partial class DividaDetalhePage : ContentPage
{
    private readonly DividaDetalheViewModel _viewModel;

    public DividaDetalhePage(DividaDetalheViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

}
