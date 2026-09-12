using GestaoFrota.App.Models;

namespace GestaoFrota.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        AbaUsuarios.IsVisible = false;
    }

    /// <summary>
    /// Adapta o menu por perfil (US17): o Motorista não deve nem ver as
    /// abas financeiras (Categorias/Pagamento) — a restrição de escrita já
    /// é validada no backend, mas a navegação também não deve oferecer uma
    /// área que ele não pode usar. Usuários e Financeiro ficam restritos
    /// ao Administrador e ao Financeiro/GestorDeFrota, respectivamente.
    /// </summary>
    public void ConfigurarMenuPorPerfil(UsuarioLogado? usuario)
    {
        var perfil = usuario?.Perfil;

        var podeVerFinanceiro = perfil is PerfilUsuario.Administrador
            or PerfilUsuario.GestorDeFrota
            or PerfilUsuario.Financeiro;

        AbaCategorias.IsVisible = podeVerFinanceiro;
        AbaFormasPagamento.IsVisible = podeVerFinanceiro;
        AbaUsuarios.IsVisible = perfil == PerfilUsuario.Administrador;
    }
}