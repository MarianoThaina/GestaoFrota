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

        // Módulo de frota (US06, US06-b, US08)
        Routing.RegisterRoute(Rotas.VeiculoForm, typeof(VeiculoFormPage));
        Routing.RegisterRoute(Rotas.ManutencaoForm, typeof(ManutencaoFormPage));
        Routing.RegisterRoute(Rotas.ViagemForm, typeof(ViagemFormPage));
        Routing.RegisterRoute(Rotas.EncerrarViagem, typeof(EncerrarViagemPage));

        // Até o perfil ser conhecido, nada além do Início fica visível.
        MenuViagens.IsVisible = false;
        MenuFrota.IsVisible = false;
        MenuRotas.IsVisible = false;
        MenuMovimentacoes.IsVisible = false;
        MenuDividas.IsVisible = false;
        MenuRelatorios.IsVisible = false;
        MenuCategorias.IsVisible = false;
        MenuFormasPagamento.IsVisible = false;
        MenuUsuarios.IsVisible = false;
    }

    /// <summary>
    /// Adapta o menu por perfil (US17, RBAC):
    /// - Administrador vê tudo.
    /// - Gestor de Frota vê frota, rotas, viagens e consulta o financeiro.
    /// - Financeiro vê os módulos financeiros e os cadastros base.
    /// - Motorista vê Viagens (abre direto nela) e a Frota só para consulta;
    ///   os módulos financeiros nem aparecem no menu.
    /// Escrever exige o perfil certo, validado em cada tela e na API.
    /// </summary>
    public void ConfigurarMenuPorPerfil(UsuarioLogado? usuario)
    {
        var perfil = usuario?.Perfil;

        var podeVerFinanceiro = perfil is PerfilUsuario.Administrador
            or PerfilUsuario.GestorDeFrota
            or PerfilUsuario.Financeiro;

        var podeVerFrota = perfil is PerfilUsuario.Administrador
            or PerfilUsuario.GestorDeFrota
            or PerfilUsuario.OperadorMotorista;

        MenuViagens.IsVisible = podeVerFrota;
        MenuFrota.IsVisible = podeVerFrota;
        MenuRotas.IsVisible = perfil is PerfilUsuario.Administrador or PerfilUsuario.GestorDeFrota;

        MenuMovimentacoes.IsVisible = podeVerFinanceiro;
        MenuDividas.IsVisible = podeVerFinanceiro;
        MenuRelatorios.IsVisible = podeVerFinanceiro;
        MenuCategorias.IsVisible = podeVerFinanceiro;
        MenuFormasPagamento.IsVisible = podeVerFinanceiro;
        MenuUsuarios.IsVisible = perfil == PerfilUsuario.Administrador;

        LblNomeUsuario.Text = usuario?.Nome ?? "Gestão de Frota";
        LblPerfilUsuario.Text = perfil.HasValue ? NomeDoPerfil(perfil.Value) : string.Empty;

        // US17: o Motorista abre o app direto na tela de Viagens.
        if (perfil == PerfilUsuario.OperadorMotorista)
        {
            CurrentItem = MenuViagens;
        }
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
