using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    /// <summary>
    /// Verdadeiro enquanto o App confere a sessão salva ao abrir: a tela mostra
    /// "Verificando sessão…" em vez do botão, para ninguém entrar de novo à toa.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarBotaoEntrar))]
    private bool verificandoSessao;

    public bool MostrarBotaoEntrar => !VerificandoSessao;

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

var usuario =
    await _authService.ObterUsuarioLogadoAsync();

if (usuario is null)
{
    MensagemErro =
        "Não foi possível identificar o usuário logado.";
    return;
}

var appShell = new AppShell();

appShell.ConfigurarMenuPorPerfil(usuario);

if (Application.Current?.Windows.Count > 0)
{
    Application.Current.Windows[0].Page =
        appShell;
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
