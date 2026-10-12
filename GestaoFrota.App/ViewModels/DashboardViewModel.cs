using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Helpers;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;
using GestaoFrota.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace GestaoFrota.App.ViewModels;

/// <summary>
/// Início: exibe o usuário autenticado (sessão persistida), o saldo atual e a
/// projeção de caixa por mês (somente perfis financeiros) e permite encerrar
/// a sessão. O Motorista vê apenas a saudação — os módulos financeiros não
/// aparecem nem no menu nem aqui.
/// </summary>
public partial class DashboardViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IFinanceiroService _financeiroService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private UsuarioLogado? usuario;

    /// <summary>Administrador, Gestor de Frota e Financeiro enxergam o painel financeiro.</summary>
    [ObservableProperty]
    private bool mostrarFinanceiro;

    [ObservableProperty]
    private SaldoResumo? saldo;

    [ObservableProperty]
    private string? mensagemFinanceiro;

    [ObservableProperty]
    private bool semProjecao;

    public ObservableCollection<ProjecaoMes> Projecao { get; } = new();

    public DashboardViewModel(
        IAuthService authService,
        IFinanceiroService financeiroService,
        IServiceProvider serviceProvider)
    {
        _authService = authService;
        _financeiroService = financeiroService;
        _serviceProvider = serviceProvider;
    }

    public async Task CarregarAsync()
    {
        Usuario = await _authService.ObterUsuarioLogadoAsync();

        MostrarFinanceiro = Usuario?.Perfil is PerfilUsuario.Administrador
            or PerfilUsuario.GestorDeFrota
            or PerfilUsuario.Financeiro;

        if (!MostrarFinanceiro)
        {
            Saldo = null;
            Projecao.Clear();
            return;
        }

        Carregando = true;
        MensagemFinanceiro = null;

        try
        {
            Saldo = await _financeiroService.ObterSaldoAsync();

            var projecao = await _financeiroService.ObterProjecaoAsync();
            Projecao.Clear();
            foreach (var mes in projecao.Meses)
            {
                Projecao.Add(mes);
            }

            SemProjecao = Projecao.Count == 0;
        }
        catch (Exception ex)
        {
            MensagemFinanceiro = ErrosUi.Mensagem(ex, "Não foi possível carregar o saldo e a projeção.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            Carregando = false;
        }
    }

    [RelayCommand]
    private async Task AtualizarAsync() => await CarregarAsync();

    [RelayCommand]
    private async Task SairAsync()
    {
        if (Carregando)
        {
            return;
        }

        Carregando = true;
        try
        {
            await _authService.LogoutAsync();

            var loginPage = _serviceProvider.GetRequiredService<LoginPage>();
            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = loginPage;
            }
        }
        finally
        {
            Carregando = false;
        }
    }
}
