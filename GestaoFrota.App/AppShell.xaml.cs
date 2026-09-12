using GestaoFrota.App.Models;

namespace GestaoFrota.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        AbaUsuarios.IsVisible = false;
    }

    public void ConfigurarMenuPorPerfil(UsuarioLogado usuario)
    {
        if (usuario is null)
        {
            AbaUsuarios.IsVisible = false;
            return;
        }

        AbaUsuarios.IsVisible =
            usuario.Perfil == PerfilUsuario.Administrador;
    }
}