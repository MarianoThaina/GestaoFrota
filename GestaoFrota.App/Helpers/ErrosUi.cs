using GestaoFrota.App.Services;

namespace GestaoFrota.App.Helpers;

public static class ErrosUi
{
    /// <summary>Traduz uma exceção em uma mensagem amigável para o usuário.</summary>
    public static string Mensagem(Exception ex, string padrao) => ex switch
    {
        ApiException api => api.Message,
        InvalidOperationException invalida => invalida.Message,
        HttpRequestException => "Não foi possível falar com o servidor. Verifique a conexão e tente novamente.",
        TaskCanceledException => "O servidor demorou para responder. Tente novamente.",
        _ => padrao
    };
}
