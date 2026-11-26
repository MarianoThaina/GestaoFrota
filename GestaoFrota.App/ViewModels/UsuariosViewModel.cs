using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

public partial class UsuariosViewModel : BaseViewModel
{
    private readonly IUsuarioService _usuarioService;
    private readonly IAuthService _authService;

    public ObservableCollection<Usuario> Usuarios { get; } = new();

    /// <summary>
    /// Estática de propósito: o Picker recebe a lista antes do SelectedItem.
    /// Por RelativeSource ela chegava depois e o perfil atual ficava em branco.
    /// </summary>
    public static List<PerfilUsuario> Perfis { get; } =
        Enum.GetValues<PerfilUsuario>().ToList();

    [ObservableProperty]
    private bool podeGerenciar;

    public UsuariosViewModel(
        IUsuarioService usuarioService,
        IAuthService authService)
    {
        _usuarioService = usuarioService;
        _authService = authService;
    }

    // Trava própria: o RefreshView liga "Carregando" antes de chamar o comando,
    // então usar "Carregando" como trava fazia o indicador girar para sempre.
    private bool _carregandoAgora;

    [RelayCommand]
    private async Task CarregarAsync()
    {
        if (_carregandoAgora)
        {
            return;
        }

        _carregandoAgora = true;
        Carregando = true;
        MensagemErro = null;

        try
        {
            var usuarioLogado =
                await _authService.ObterUsuarioLogadoAsync();

            PodeGerenciar =
                usuarioLogado?.Perfil ==
                PerfilUsuario.Administrador;

            if (!PodeGerenciar)
            {
                Usuarios.Clear();

                MensagemErro =
                    "Apenas administradores podem gerenciar usuários.";

                return;
            }

            var usuarios =
                await _usuarioService.ListarAsync();

            Usuarios.Clear();

            foreach (var usuario in usuarios)
            {
                Usuarios.Add(usuario);
            }
        }
        catch (HttpRequestException ex)
        {
            MensagemErro =
                "Não foi possível acessar a API de usuários.";

            System.Diagnostics.Debug.WriteLine(ex);
        }
        catch (Exception ex)
        {
            MensagemErro =
                "Não foi possível carregar os usuários.";

            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            _carregandoAgora = false;
            Carregando = false;
        }
    }

    [RelayCommand]
    private async Task SalvarPerfilAsync(Usuario usuario)
    {
        if (usuario is null)
        {
            return;
        }

        MensagemErro = null;

        try
        {
            await _usuarioService.AlterarPerfilAsync(
                usuario.Id,
                usuario.Perfil);

            if (Shell.Current is not null)
            {
                await Shell.Current.DisplayAlert(
                    "Perfil alterado",
                    $"{usuario.Nome} agora é {Helpers.PerfilNomes.Nome(usuario.Perfil)}. A mudança vale a partir do próximo login.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            MensagemErro =
                $"Não foi possível alterar o perfil de {usuario.Nome}.";

            System.Diagnostics.Debug.WriteLine(ex);

            await CarregarAsync();
        }
    }

    [RelayCommand]
    private async Task AlterarStatusAsync(Usuario usuario)
    {
        if (usuario is null)
        {
            return;
        }

        MensagemErro = null;

        try
        {
            var novoStatus = !usuario.Ativo;

            await _usuarioService.AlterarStatusAsync(
                usuario.Id,
                novoStatus);

            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MensagemErro =
                $"Não foi possível alterar o status de {usuario.Nome}.";

            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
}