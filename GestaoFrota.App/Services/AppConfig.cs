namespace GestaoFrota.App.Services;

/// <summary>
<<<<<<< Updated upstream
/// Configuração central do app.
/// ApiBaseUrl muda conforme a compilação:
/// - Debug no Android: emulador, que enxerga o localhost do PC por 10.0.2.2;
/// - Release no Android (o .apk do celular): ApiPublica, o endereço HTTPS da API hospedada.
=======
/// Configuração central do app. ApiBaseUrl aponta para a API hospedada no MonsterASP.NET (HTTPS).
/// Única fonte da URL da API: não há mais placeholder de endereço.
>>>>>>> Stashed changes
/// </summary>
public static class AppConfig
{
    /// <summary>Endereço da API hospedada, usado pelo .apk de Release. Preencher antes de gerar o .apk.</summary>
    public const string ApiPublica = "https://ENDERECO_DA_API_HOSPEDADA/api/";

    public static string ApiBaseUrl { get; set; } =
<<<<<<< Updated upstream
#if ANDROID && DEBUG
        "http://10.0.2.2:5137/api/";
#elif ANDROID
        ApiPublica;
#else
        "https://localhost:7201/api/";
#endif
=======
        "https://gestaofrota.runasp.net/api/";
>>>>>>> Stashed changes

    /// <summary>Client ID OAuth2 do tipo "Web application" ou "iOS/Android" no Google Cloud Console.</summary>
    public const string GoogleClientId = "SEU_GOOGLE_CLIENT_ID.apps.googleusercontent.com";

    /// <summary>Deve bater com o esquema registrado nas plataformas (ver README de configuração).</summary>
    public const string GoogleRedirectUri = "com.gestaofrota.app://oauth2redirect";
<<<<<<< Updated upstream
}
=======
}
>>>>>>> Stashed changes
