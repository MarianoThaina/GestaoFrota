using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Helpers;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

/// <summary>Dívida com todas as parcelas geradas (histórico) e baixa de parcelas.</summary>
public partial class DividaDetalheViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IDividaService _dividaService;
    private readonly IMovimentacaoService _movimentacaoService;
    private readonly IAuthService _authService;

    private Guid _dividaId;

    [ObservableProperty]
    private Divida? divida;

    [ObservableProperty]
    private bool podeGerenciar;

    public ObservableCollection<Movimentacao> Parcelas { get; } = new();

    public DividaDetalheViewModel(
        IDividaService dividaService,
        IMovimentacaoService movimentacaoService,
        IAuthService authService)
    {
        _dividaService = dividaService;
        _movimentacaoService = movimentacaoService;
        _authService = authService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var valor) && Guid.TryParse(valor?.ToString(), out var id))
        {
            _dividaId = id;
            _ = CarregarAsync();
        }
    }

    [RelayCommand]
    private async Task CarregarAsync()
    {
        if (_dividaId == Guid.Empty)
        {
            return;
        }

        Carregando = true;
        MensagemErro = null;

        try
        {
            var usuario = await _authService.ObterUsuarioLogadoAsync();
            PodeGerenciar = usuario?.Perfil is PerfilUsuario.Administrador or PerfilUsuario.Financeiro;

            var detalhe = await _dividaService.ObterAsync(_dividaId);
            Divida = detalhe.Divida;

            Parcelas.Clear();
            foreach (var parcela in detalhe.Parcelas)
            {
                Parcelas.Add(parcela);
            }
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível carregar a dívida.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            Carregando = false;
        }
    }

    [RelayCommand]
    private async Task PagarParcelaAsync(Movimentacao parcela)
    {
        var confirmou = await Shell.Current.DisplayAlert(
            "Pagar parcela",
            $"Marcar \"{parcela.Descricao}\" como paga hoje ({Formatacao.Data(DateTime.Today)})?",
            "Sim", "Não");

        if (!confirmou)
        {
            return;
        }

        try
        {
            await _movimentacaoService.PagarAsync(parcela.Id, DateTime.Today);
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível pagar a parcela.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
}
