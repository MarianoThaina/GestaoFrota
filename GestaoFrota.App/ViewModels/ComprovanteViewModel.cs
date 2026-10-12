using CommunityToolkit.Mvvm.ComponentModel;
using GestaoFrota.App.Helpers;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

/// <summary>Exibe o comprovante (imagem) de uma movimentação, baixado da API.</summary>
public partial class ComprovanteViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IMovimentacaoService _movimentacaoService;

    [ObservableProperty]
    private ImageSource? imagem;

    public ComprovanteViewModel(IMovimentacaoService movimentacaoService)
    {
        _movimentacaoService = movimentacaoService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var valor) && Guid.TryParse(valor?.ToString(), out var id))
        {
            _ = CarregarAsync(id);
        }
    }

    private async Task CarregarAsync(Guid id)
    {
        Carregando = true;
        MensagemErro = null;

        try
        {
            var bytes = await _movimentacaoService.ObterComprovanteAsync(id);
            Imagem = ImageSource.FromStream(() => new MemoryStream(bytes));
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível carregar o comprovante.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            Carregando = false;
        }
    }
}
