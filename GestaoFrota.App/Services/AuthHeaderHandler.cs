using System.Net.Http.Headers;

namespace GestaoFrota.App.Services;

/// <summary>
/// DelegatingHandler que anexa o JWT salvo em todas as requisições
/// autenticadas, evitando repetir esse código em cada serviço.
/// </summary>
public class AuthHeaderHandler : DelegatingHandler
{
    private readonly ITokenStore _tokenStore;

    public AuthHeaderHandler(ITokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenStore.ObterTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
