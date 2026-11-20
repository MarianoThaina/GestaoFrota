namespace GestaoFrota.App.Services;

/// <summary>
/// Configuração central do app. Única fonte da URL da API.
/// ApiBaseUrl muda conforme a compilação:
/// - Debug no Android: emulador, que enxerga o localhost do PC por 10.0.2.2 (HTTP, só em Debug);
/// - Release no Android (o .apk do celular): ApiPublica, a API hospedada no MonsterASP.NET (HTTPS).
/// </summary>
public static class AppConfig
{
    /// <summary>Endereço da API hospedada (MonsterASP.NET), usado pelo .apk de Release.</summary>
    public const string ApiPublica = "https://gestaofrota.runasp.net/api/";

    public static string ApiBaseUrl { get; set; } =
#if ANDROID && DEBUG
        "http://10.0.2.2:5137/api/";
#elif ANDROID
        ApiPublica;
#else
        "https://localhost:7201/api/";
#endif

    /// <summary>Client ID OAuth2 do tipo "Web application" ou "iOS/Android" no Google Cloud Console.</summary>
    public const string GoogleClientId = "SEU_GOOGLE_CLIENT_ID.apps.googleusercontent.com";

    /// <summary>Deve bater com o esquema registrado nas plataformas (ver README de configuração).</summary>
    public const string GoogleRedirectUri = "com.gestaofrota.app://oauth2redirect";
}
