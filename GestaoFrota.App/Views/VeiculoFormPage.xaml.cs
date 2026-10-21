using GestaoFrota.App.ViewModels;

namespace GestaoFrota.App.Views;

public partial class VeiculoFormPage : ContentPage
{
    public VeiculoFormPage(VeiculoFormViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
