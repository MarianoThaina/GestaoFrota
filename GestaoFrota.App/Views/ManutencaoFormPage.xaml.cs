using GestaoFrota.App.ViewModels;

namespace GestaoFrota.App.Views;

public partial class ManutencaoFormPage : ContentPage
{
    public ManutencaoFormPage(ManutencaoFormViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
