using CommunityToolkit.Mvvm.ComponentModel;

namespace GestaoFrota.App.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool carregando;

    [ObservableProperty]
    private string? mensagemErro;
}
