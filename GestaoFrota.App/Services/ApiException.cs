using System.Net;
using System.Net.Http.Json;

namespace GestaoFrota.App.Services;

/// <summary>Erro de negócio/validação devolvido pela API, com mensagem pronta para o usuário.</summary>
public class ApiException : Exception
{
    public HttpStatusCode? StatusCode { get; }

    public ApiException(string mensagem, HttpStatusCode? statusCode = null) : base(mensagem)
    {
        StatusCode = statusCode;
    }
}

public class ErroApi
{
    public string? Mensagem { get; set; }
}

public static class HttpResponseExtensions
{
    /// <summary>
    /// Lança <see cref="ApiException"/> com a mensagem {"mensagem": "..."} devolvida
    /// pela API (validações, regras de negócio), ou um texto padrão por status.
    /// </summary>
    public static async Task GarantirSucessoAsync(this HttpResponseMessage resposta)
    {
        if (resposta.IsSuccessStatusCode)
        {
            return;
        }

        var mensagem = resposta.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Sua sessão expirou. Entre novamente.",
            HttpStatusCode.Forbidden => "Você não tem permissão para esta ação.",
            HttpStatusCode.NotFound => "Registro não encontrado.",
            _ => "Não foi possível concluir a operação. Tente novamente."
        };

        try
        {
            var erro = await resposta.Content.ReadFromJsonAsync<ErroApi>();
            if (!string.IsNullOrWhiteSpace(erro?.Mensagem))
            {
                mensagem = erro.Mensagem;
            }
        }
        catch (Exception)
        {
            // Corpo vazio ou fora do formato esperado: mantém a mensagem padrão.
        }

        throw new ApiException(mensagem, resposta.StatusCode);
    }
}
