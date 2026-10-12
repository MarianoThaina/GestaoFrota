using GestaoFrota.App.Helpers;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;
using GestaoFrota.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace GestaoFrota.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Telas "empurradas" sobre o menu (formulários, detalhe, comprovante).
        Routing.RegisterRoute(Rotas.MovimentacaoForm, typeof(MovimentacaoFormPage));
        Routing.RegisterRoute(Rotas.Comprovante, typeof(ComprovantePage));
        Routing.RegisterRoute(Rotas.DividaForm, typeof(DividaFormPage));
        Routing.RegisterRoute(Rotas.DividaDetalhe, typeof(DividaDetalhePage));

        // Até o perfil ser conhecido, nada além do Início fica visível.
        MenuMovimentacoes.IsVisible = false;
        MenuDividas.IsVisible = false;
        MenuCategorias.IsVisible = false;
        MenuFormasPagamento.IsVisible = false;
        MenuUsuarios.IsVisible = false;
    }

    /// <summary>
    /// Adapta o menu por perfil (US17, RBAC):
    /// Administrador vê tudo; Financeiro e Gestor de Frota veem os módulos
    /// financeiros e os cadastros base (o Gestor só consulta; escrever exige
    /// Administrador/Financeiro, validado em cada tela e na API); o Motorista
    /// vê apenas o Início — os módulos financeiros nem aparecem no menu.
    /// </summary>
    public void ConfigurarMenuPorPerfil(UsuarioLogado? usuario)
    {
        var perfil = usuario?.Perfil;

        var podeVerFinanceiro = perfil is PerfilUsuario.Administrador
            or PerfilUsuario.GestorDeFrota
            or PerfilUsuario.Financeiro;

        MenuMovimentacoes.IsVisible = podeVerFinanceiro;
        MenuDividas.IsVisible = podeVerFinanceiro;
        MenuCategorias.IsVisible = podeVerFinanceiro;
        MenuFormasPagamento.IsVisible = podeVerFinanceiro;
        MenuUsuarios.IsVisible = perfil == PerfilUsuario.Administrador;

        LblNomeUsuario.Text = usuario?.Nome ?? "Gestão de Frota";
        LblPerfilUsuario.Text = perfil.HasValue ? NomeDoPerfil(perfil.Value) : string.Empty;
    }

    private static string NomeDoPerfil(PerfilUsuario perfil) => perfil switch
    {
        PerfilUsuario.Administrador => "Administrador",
        PerfilUsuario.GestorDeFrota => "Gestor de Frota",
        PerfilUsuario.Financeiro => "Financeiro",
        PerfilUsuario.OperadorMotorista => "Motorista / Operador",
        _ => perfil.ToString()
    };

    private async void OnSairClicked(object? sender, EventArgs e)
    {
        var servicos = Handler?.MauiContext?.Services;
        if (servicos is null)
        {
            return;
        }

        FlyoutIsPresented = false;

        await servicos.GetRequiredService<IAuthService>().LogoutAsync();

        if (Application.Current?.Windows.Count > 0)
        {
            Application.Current.Windows[0].Page = servicos.GetRequiredService<LoginPage>();
        }
    }
}
