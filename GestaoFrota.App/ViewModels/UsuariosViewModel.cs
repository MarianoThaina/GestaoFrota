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

    // ---------- Novo usuário com senha (já nasce com o perfil escolhido) ----------

    [ObservableProperty]
    private bool mostrarNovoUsuario;

    [ObservableProperty]
    private string novoNome = string.Empty;

    [ObservableProperty]
    private string novoEmail = string.Empty;

    [ObservableProperty]
    private PerfilUsuario novoPerfil = PerfilUsuario.OperadorMotorista;

    [ObservableProperty]
    private string novaSenha = string.Empty;

    [ObservableProperty]
    private string? erroNovoUsuario;

    [RelayCommand]
    private void AlternarNovoUsuario()
    {
        MostrarNovoUsuario = !MostrarNovoUsuario;
        ErroNovoUsuario = null;
    }

    [RelayCommand]
    private async Task CriarUsuarioAsync()
    {
        if (string.IsNullOrWhiteSpace(NovoNome) || string.IsNullOrWhiteSpace(NovoEmail) || string.IsNullOrEmpty(NovaSenha))
        {
            ErroNovoUsuario = "Preencha nome, e-mail e senha.";
            return;
        }

        ErroNovoUsuario = null;

        try
        {
            await _usuarioService.CriarAsync(NovoNome.Trim(), NovoEmail.Trim(), NovoPerfil, NovaSenha);

            var nome = NovoNome.Trim();
            var perfil = Helpers.PerfilNomes.Nome(NovoPerfil);
            var email = NovoEmail.Trim();

            NovoNome = string.Empty;
            NovoEmail = string.Empty;
            NovaSenha = string.Empty;
            NovoPerfil = PerfilUsuario.OperadorMotorista;
            MostrarNovoUsuario = false;

            await CarregarAsync();

            if (Shell.Current is not null)
            {
                await Shell.Current.DisplayAlert(
                    "Usuário criado",
                    $"{nome} ({perfil}) já pode entrar com o e-mail {email} e a senha definida.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            ErroNovoUsuario = Helpers.ErrosUi.Mensagem(ex, "Não foi possível criar o usuário.");
            System.Diagnostics.Debug.WriteLine(ex.GetType().Name);
        }
    }

    [RelayCommand]
    private async Task DefinirSenhaAsync(Usuario usuario)
    {
        if (usuario is null || Shell.Current is null)
        {
            return;
        }

        var senha = await Shell.Current.DisplayPromptAsync(
            "Definir senha",
            $"Nova senha de {usuario.Nome}: pelo menos 8 caracteres, com letras e números. " +
            "A senha aparece enquanto você digita.",
            "Salvar", "Cancelar",
            placeholder: "Nova senha",
            maxLength: 100,
            keyboard: Keyboard.Text);

        if (string.IsNullOrEmpty(senha))
        {
            return;
        }

        MensagemErro = null;

        try
        {
            await _usuarioService.DefinirSenhaAsync(usuario.Id, senha);
            await CarregarAsync();
            await Shell.Current.DisplayAlert(
                "Senha definida",
                $"{usuario.Nome} já pode entrar com o e-mail {usuario.Email} e a nova senha.",
                "OK");
        }
        catch (Exception ex)
        {
            MensagemErro = Helpers.ErrosUi.Mensagem(ex, $"Não foi possível definir a senha de {usuario.Nome}.");
            System.Diagnostics.Debug.WriteLine(ex.GetType().Name);
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