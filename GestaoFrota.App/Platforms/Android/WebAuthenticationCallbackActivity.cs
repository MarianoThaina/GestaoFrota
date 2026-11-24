using Android.App;
using Android.Content;
using Android.Content.PM;

namespace GestaoFrota.App;

/// <summary>
/// Recebe o redirecionamento da API (com.gestaofrota.app://oauth2redirect#id_token=...)
/// e devolve o resultado ao WebAuthenticator. O esquema deve ser igual a
/// AppConfig.GoogleCallbackUri.
/// </summary>
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "com.gestaofrota.app",
    DataHost = "oauth2redirect")]
public class WebAuthenticationCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
}
