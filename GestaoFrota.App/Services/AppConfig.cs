namespace GestaoFrota.App.Services;

/// <summary>
/// Configuração central do app. Ajuste ApiBaseUrl para o endereço da sua API
/// (em dev, o emulador Android usa 10.0.2.2 para acessar o localhost da máquina).
/// </summary>
public static class AppConfig
{
    public static string ApiBaseUrl { get; set; } =
#if ANDROID
        "http://10.0.2.2:5137/api/";
#else
        "https://localhost:7201/api/";
#endif

    /// <summary>Client ID OAuth2 do tipo "Web application" ou "iOS/Android" no Google Cloud Console.</summary>
    public const string GoogleClientId = "SEU_GOOGLE_CLIENT_ID.apps.googleusercontent.com";

    /// <summary>Deve bater com o esquema registrado nas plataformas (ver README de configuração).</summary>
    public const string GoogleRedirectUri = "com.suaempresa.gestaofrota://oauth2redirect";
}
