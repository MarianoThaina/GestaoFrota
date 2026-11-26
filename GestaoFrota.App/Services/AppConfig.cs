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
    public const string ApiPublica = SitePublico + "api/";

    /// <summary>Site publicado junto com a API (painel web e Central de Ajuda).</summary>
    public const string SitePublico = "https://gestaofrota.runasp.net/";

    public static string ApiBaseUrl { get; set; } =
#if ANDROID && DEBUG
        "http://10.0.2.2:5137/api/";
#elif ANDROID
        ApiPublica;
#else
        "https://localhost:7201/api/";
#endif

    /// <summary>
    /// Client ID OAuth2 do tipo "Aplicativo da Web" no Google Cloud Console.
    /// É o mesmo da API e do site: a API só aceita id_token emitido para ele.
    /// </summary>
    public const string GoogleClientId = "869635075019-me5auvsrffljgkk40oc9jfucd91sblls.apps.googleusercontent.com";

    /// <summary>
    /// Para onde o Google devolve o login. O cliente Web do Google só aceita
    /// endereço https, então o Google devolve o código para a API publicada,
    /// que troca pelo id_token e redireciona para GoogleCallbackUri.
    /// Cadastrado em "URIs de redirecionamento autorizados" no Google Cloud.
    /// Vale também no emulador (Debug): o retorno sempre passa pela API publicada.
    /// </summary>
    public const string GoogleRedirectUri = ApiPublica + "auth/google/retorno";

    /// <summary>
    /// Esquema que a API usa para devolver o id_token ao app. Deve bater com
    /// WebAuthenticationCallbackActivity (Platforms/Android).
    /// </summary>
    public const string GoogleCallbackUri = "com.gestaofrota.app://oauth2redirect";
}
