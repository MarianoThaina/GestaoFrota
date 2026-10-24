using GestaoFrota.App.ViewModels;

namespace GestaoFrota.App.Views;

public partial class EncerrarViagemPage : ContentPage
{
    public EncerrarViagemPage(EncerrarViagemViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
