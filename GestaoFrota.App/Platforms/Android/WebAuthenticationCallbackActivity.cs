using Android.App;
using Android.Content;
using Android.Content.PM;

namespace GestaoFrota.App;

/// <summary>
/// Recebe o redirecionamento do Google (com.suaempresa.gestaofrota://oauth2redirect)
/// e devolve o resultado ao WebAuthenticator. O esquema deve ser igual a
/// AppConfig.GoogleRedirectUri.
/// </summary>
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "com.suaempresa.gestaofrota",
    DataHost = "oauth2redirect")]
public class WebAuthenticationCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
}
