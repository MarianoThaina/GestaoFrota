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

    public List<PerfilUsuario> PerfisDisponiveis { get; } =
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

    [RelayCommand]
    private async Task CarregarAsync()
    {
        if (Carregando)
        {
            return;
        }

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