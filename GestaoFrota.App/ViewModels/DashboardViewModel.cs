using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;
using GestaoFrota.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace GestaoFrota.App.ViewModels;

/// <summary>
/// Exibe o usuário autenticado (obtido da sessão persistida) e permite
/// encerrar a sessão — evidência funcional de que o login com Google e a
/// persistência de sessão (Documentação Técnica, seção 8) estão de fato
/// operando, e não apenas desenhados no protótipo.
/// </summary>
public partial class DashboardViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private UsuarioLogado? usuario;

    public DashboardViewModel(IAuthService authService, IServiceProvider serviceProvider)
    {
        _authService = authService;
        _serviceProvider = serviceProvider;
    }

    public async Task CarregarAsync()
    {
        Usuario = await _authService.ObterUsuarioLogadoAsync();
    }

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
