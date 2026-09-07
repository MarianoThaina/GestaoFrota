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
            // Importante: com SingleProject/multi-window, a troca de tela
            // é feita em Windows[0].Page (Application.Current.MainPage não
            // afeta a Window criada em App.CreateWindow).
            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = new AppShell();
            }
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
