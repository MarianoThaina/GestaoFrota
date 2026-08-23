using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task EntrarComGoogleAsync()
    {
        if (Carregando)
        {
            return;
        }

        Carregando = true;
        MensagemErro = null;

        try
        {
            await _authService.LoginComGoogleAsync();

            // AppShell decide o menu inicial com base no perfil logado.
            Application.Current!.MainPage = new AppShell();
        }
        catch (Exception ex)
        {
            MensagemErro = "Não foi possível entrar com o Google. Tente novamente.";
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            Carregando = false;
        }
    }
}
