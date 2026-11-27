using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Helpers;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

/// <summary>
/// Tela de entrada com as duas formas de login: e-mail e senha (conta criada
/// pelo Administrador) ou Google. As duas recebem o mesmo JWT da API.
/// </summary>
public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    /// <summary>
    /// Verdadeiro enquanto o App confere a sessão salva ao abrir: a tela mostra
    /// "Verificando sessão…" em vez dos botões, para ninguém entrar de novo à toa.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarBotaoEntrar))]
    private bool verificandoSessao;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string senha = string.Empty;

    public bool MostrarBotaoEntrar => !VerificandoSessao;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task EntrarComSenhaAsync()
    {
        if (Carregando)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrEmpty(Senha))
        {
            MensagemErro = "Informe o e-mail e a senha.";
            return;
        }

        Carregando = true;
        MensagemErro = null;

        try
        {
            var usuario = await _authService.LoginComSenhaAsync(Email.Trim(), Senha);
            Senha = string.Empty;
            AbrirApp(usuario);
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível entrar. Tente novamente.");
            System.Diagnostics.Debug.WriteLine(ex.GetType().Name);
        }
        finally
        {
            Carregando = false;
        }
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

            var usuario = await _authService.ObterUsuarioLogadoAsync();
            if (usuario is null)
            {
                MensagemErro = "Não foi possível identificar o usuário logado.";
                return;
            }

            AbrirApp(usuario);
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

    private static void AbrirApp(UsuarioLogado usuario)
    {
        var appShell = new AppShell();
        appShell.ConfigurarMenuPorPerfil(usuario);

        if (Application.Current?.Windows.Count > 0)
        {
            Application.Current.Windows[0].Page = appShell;
        }
    }
}
