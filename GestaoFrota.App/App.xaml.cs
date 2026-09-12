using GestaoFrota.App.Services;
using GestaoFrota.App.Views;

namespace GestaoFrota.App;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Abre sempre no Login primeiro; assim que checarmos a sessão salva
        // (token válido no SecureStorage), trocamos para o Shell autenticado.
        var loginPage = _serviceProvider.GetRequiredService<LoginPage>();
        var window = new Window(loginPage);

        _ = VerificarSessaoAsync(window);

        return window;
    }

    private async Task VerificarSessaoAsync(Window window)
    {
        var localDb = _serviceProvider.GetRequiredService<ILocalDatabaseService>();
        await localDb.InicializarAsync();

        var authService = _serviceProvider.GetRequiredService<IAuthService>();

        // Documentação Técnica, seção 8, item 7: GET /api/auth/me valida a
        // sessão salva (token + usuário ainda ativo) sem exigir novo login.
        var sessaoValida = await authService.ValidarSessaoAsync();

        if (sessaoValida)
        {
            var usuario = await authService.ObterUsuarioLogadoAsync();

            var appShell = new AppShell();
            appShell.ConfigurarMenuPorPerfil(usuario);

            window.Page = appShell;
        }
    }
}
