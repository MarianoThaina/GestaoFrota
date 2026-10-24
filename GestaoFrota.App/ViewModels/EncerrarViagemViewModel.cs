using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Helpers;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

/// <summary>
/// Encerrar viagem (US08): a validação crítica é o hodômetro final maior que o
/// inicial. Mostra a prévia da quilometragem rodada enquanto o motorista digita.
/// Funciona sem internet (fica na fila e sobe depois).
/// </summary>
public partial class EncerrarViagemViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IViagemService _viagemService;
    private readonly IConnectivityService _connectivity;

    [ObservableProperty]
    private Viagem? viagem;

    [ObservableProperty]
    private string hodometroFinalTexto = string.Empty;

    [ObservableProperty]
    private string observacoes = string.Empty;

    [ObservableProperty]
    private string? previaRodado;

    [ObservableProperty]
    private bool hodometroInvalido;

    [ObservableProperty]
    private bool offline;

    public EncerrarViagemViewModel(IViagemService viagemService, IConnectivityService connectivity)
    {
        _viagemService = viagemService;
        _connectivity = connectivity;
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
            Offline = !_connectivity.EstaOnline();

            Viagem = await _viagemService.ObterLocalAsync(id);
            if (Viagem is null)
            {
                MensagemErro = "Viagem não encontrada neste aparelho. Volte e puxe para atualizar.";
                return;
            }

            Observacoes = Viagem.Observacoes ?? string.Empty;
        }
        finally
        {
            Carregando = false;
        }
    }

    partial void OnHodometroFinalTextoChanged(string value)
    {
        if (Viagem is null || !ParserMoeda.TentarConverter(value, out var final))
        {
            PreviaRodado = null;
            HodometroInvalido = false;
            return;
        }

        if (final <= Viagem.HodometroInicial)
        {
            PreviaRodado = null;
            HodometroInvalido = true;
            return;
        }

        HodometroInvalido = false;
        PreviaRodado = $"{Formatacao.Km(final - Viagem.HodometroInicial)} rodados nesta viagem";
    }

    [RelayCommand]
    private async Task ConfirmarAsync()
    {
        if (Carregando || Viagem is null)
        {
            return;
        }

        if (!ParserMoeda.TentarConverter(HodometroFinalTexto, out var final))
        {
            MensagemErro = "Informe o hodômetro final em km, sem sinal negativo (ex.: 184.735).";
            return;
        }

        if (final <= Viagem.HodometroInicial)
        {
            MensagemErro = $"O hodômetro final deve ser maior que o inicial ({Viagem.HodometroInicialTexto}).";
            HodometroInvalido = true;
            return;
        }

        MensagemErro = null;
        Carregando = true;

        try
        {
            await _viagemService.EncerrarAsync(Viagem.Id, final, Observacoes);

            if (!_connectivity.EstaOnline())
            {
                await Shell.Current.DisplayAlert(
                    "Encerramento salvo no aparelho",
                    "Sem conexão agora. O hodômetro final ficou salvo e será enviado ao servidor assim que a rede voltar.",
                    "OK");
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível encerrar a viagem.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            Carregando = false;
        }
    }

    [RelayCommand]
    private async Task VoltarAsync() => await Shell.Current.GoToAsync("..");
}
