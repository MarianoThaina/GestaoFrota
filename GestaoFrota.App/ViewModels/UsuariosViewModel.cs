using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Helpers;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

/// <summary>
/// Gestão de usuários (US16): aprovar cadastros pendentes definindo o perfil,
/// alterar perfil, desativar e reativar. Só o Administrador chega aqui.
/// </summary>
public partial class UsuariosViewModel : BaseViewModel
{
    private readonly IUsuarioService _usuarioService;
    private readonly IAuthService _authService;

    public ObservableCollection<Usuario> Usuarios { get; } = new();

    public List<PerfilUsuario> PerfisDisponiveis { get; } = Enum.GetValues<PerfilUsuario>().ToList();

    [ObservableProperty]
    private bool podeGerenciar;

    [ObservableProperty]
    private int pendentes;

    [ObservableProperty]
    private string? avisoPendentes;

    public UsuariosViewModel(IUsuarioService usuarioService, IAuthService authService)
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
            var usuarioLogado = await _authService.ObterUsuarioLogadoAsync();
            PodeGerenciar = usuarioLogado?.Perfil == PerfilUsuario.Administrador;

            if (!PodeGerenciar)
            {
                Usuarios.Clear();
                MensagemErro = "Apenas administradores podem gerenciar usuários.";
                return;
            }

            var usuarios = await _usuarioService.ListarAsync();

            Usuarios.Clear();
            foreach (var usuario in usuarios)
            {
                Usuarios.Add(usuario);
            }

            Pendentes = usuarios.Count(u => u.AguardandoAprovacao);
            AvisoPendentes = Pendentes switch
            {
                0 => null,
                1 => "1 cadastro aguardando aprovação. Defina o perfil e toque em Aprovar.",
                _ => $"{Pendentes} cadastros aguardando aprovação. Defina o perfil e toque em Aprovar."
            };
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível carregar os usuários.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            Carregando = false;
        }
    }

    /// <summary>US16: libera o acesso com o perfil escolhido no picker do cartão.</summary>
    [RelayCommand]
    private async Task AprovarAsync(Usuario usuario)
    {
        if (usuario is null)
        {
            return;
        }

        var confirmou = await Shell.Current.DisplayAlert(
            "Aprovar cadastro",
            $"Liberar o acesso de {usuario.Nome} ({usuario.Email}) como {NomeDoPerfil(usuario.Perfil)}?",
            "Aprovar", "Cancelar");

        if (!confirmou)
        {
            return;
        }

        MensagemErro = null;

        try
        {
            await _usuarioService.AprovarAsync(usuario.Id, usuario.Perfil);
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, $"Não foi possível aprovar {usuario.Nome}.");
            System.Diagnostics.Debug.WriteLine(ex);
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
            await _usuarioService.AlterarPerfilAsync(usuario.Id, usuario.Perfil);
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, $"Não foi possível alterar o perfil de {usuario.Nome}.");
            System.Diagnostics.Debug.WriteLine(ex);
            await CarregarAsync();
        }
    }

    /// <summary>Ativo vira Inativo; Inativo (ou pendente) vira Ativo.</summary>
    [RelayCommand]
    private async Task AlterarSituacaoAsync(Usuario usuario)
    {
        if (usuario is null)
        {
            return;
        }

        var nova = usuario.Situacao == SituacaoUsuario.Ativo ? SituacaoUsuario.Inativo : SituacaoUsuario.Ativo;

        if (nova == SituacaoUsuario.Inativo)
        {
            var confirmou = await Shell.Current.DisplayAlert(
                "Desativar usuário",
                $"{usuario.Nome} não vai mais conseguir entrar no app até ser reativado. Desativar?",
                "Desativar", "Cancelar");

            if (!confirmou)
            {
                return;
            }
        }

        MensagemErro = null;

        try
        {
            await _usuarioService.AlterarSituacaoAsync(usuario.Id, nova);
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, $"Não foi possível alterar a situação de {usuario.Nome}.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    private static string NomeDoPerfil(PerfilUsuario perfil) => perfil switch
    {
        PerfilUsuario.Administrador => "Administrador",
        PerfilUsuario.GestorDeFrota => "Gestor de Frota",
        PerfilUsuario.Financeiro => "Financeiro",
        _ => "Motorista / Operador"
    };
}
