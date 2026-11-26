using GestaoFrota.App.Models;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.Helpers;

/// <summary>
/// Abre a Central de Ajuda (página pública do site, sem login) no navegador
/// do celular. Fica no site, e não dentro do app, para o mesmo texto valer
/// para o app, o site e a apresentação; corrigir uma frase não exige .apk novo.
/// Com o perfil conhecido, a página já abre filtrada para ele.
/// </summary>
public static class CentralDeAjuda
{
    public const string Endereco = AppConfig.SitePublico + "ajuda.html";

    public static async Task AbrirAsync(PerfilUsuario? perfil = null)
    {
        var endereco = perfil switch
        {
            PerfilUsuario.Administrador => Endereco + "?perfil=admin",
            PerfilUsuario.GestorDeFrota => Endereco + "?perfil=gestor",
            PerfilUsuario.Financeiro => Endereco + "?perfil=financeiro",
            PerfilUsuario.OperadorMotorista => Endereco + "?perfil=motorista",
            _ => Endereco
        };

        try
        {
            await Browser.Default.OpenAsync(new Uri(endereco), BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            var pagina = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (pagina is not null)
            {
                await pagina.DisplayAlert(
                    "Central de Ajuda",
                    $"Não foi possível abrir o navegador. Acesse pelo endereço:\n{Endereco}",
                    "OK");
            }
        }
    }
}
