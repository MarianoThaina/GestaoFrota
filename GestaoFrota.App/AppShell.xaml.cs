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
        _itemInicial = perfil == PerfilUsuario.OperadorMotorista ? MenuViagens : MenuInicio;
        if (perfil == PerfilUsuario.OperadorMotorista)
        {
            CurrentItem = MenuViagens;
        }
    }

    private FlyoutItem? _itemInicial;

    /// <summary>
    /// Voltar (botão ou gesto): fecha o menu se estiver aberto; dentro de um
    /// módulo do menu volta para a tela inicial do perfil, em vez de fechar o
    /// app; só na tela inicial é que o app vai para segundo plano.
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
        if (FlyoutIsPresented)
        {
            FlyoutIsPresented = false;
            return true;
        }

        if (Navigation.NavigationStack.Count <= 1 && _itemInicial is not null && CurrentItem != _itemInicial)
        {
            CurrentItem = _itemInicial;
            return true;
        }

        return base.OnBackButtonPressed();
    }

    private static string NomeDoPerfil(PerfilUsuario perfil) => Helpers.PerfilNomes.Nome(perfil);

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
