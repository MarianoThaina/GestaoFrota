using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Helpers;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

/// <summary>
/// Viagens (US08). É a tela em que o Motorista abre o app. Mostra o aviso de
/// "sem conexão" com a quantidade de apontamentos na fila, e a lista com
/// filtro por situação. Abrir e encerrar funcionam offline; cancelar é só dos
/// gestores e exige conexão.
/// </summary>
public partial class ViagensViewModel : BaseViewModel
{
    private readonly IViagemService _viagemService;
    private readonly IAuthService _authService;
    private readonly IConnectivityService _connectivity;

    private List<Viagem> _todas = new();

    public ObservableCollection<Viagem> Itens { get; } = new();

    public string[] OpcoesFiltro { get; } = { "Todas", "Em andamento", "Concluídas", "Canceladas" };

    [ObservableProperty]
    private string filtro = "Todas";

    [ObservableProperty]
    private string resumoLista = string.Empty;

    [ObservableProperty]
    private bool offline;

    [ObservableProperty]
    private string avisoOffline = string.Empty;

    /// <summary>Administrador e Gestor de Frota cancelam viagens.</summary>
    [ObservableProperty]
    private bool podeCancelar;

    public ViagensViewModel(IViagemService viagemService, IAuthService authService, IConnectivityService connectivity)
    {
        _viagemService = viagemService;
        _authService = authService;
        _connectivity = connectivity;
    }

    partial void OnFiltroChanged(string value) => AplicarFiltro();

    [RelayCommand]
    private async Task CarregarAsync()
    {
        Carregando = true;
        MensagemErro = null;

        try
        {
            var usuario = await _authService.ObterUsuarioLogadoAsync();
            PodeCancelar = usuario?.Perfil is PerfilUsuario.Administrador or PerfilUsuario.GestorDeFrota;

            _todas = await _viagemService.ListarAsync();
            AplicarFiltro();
            AtualizarAvisoOffline();
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível carregar as viagens.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            Carregando = false;
        }
    }

    private void AplicarFiltro()
    {
        IEnumerable<Viagem> consulta = Filtro switch
        {
            "Em andamento" => _todas.Where(v => v.Status == StatusViagem.EmAndamento),
            "Concluídas" => _todas.Where(v => v.Status == StatusViagem.Concluida),
            "Canceladas" => _todas.Where(v => v.Status == StatusViagem.Cancelada),
            _ => _todas
        };

        Itens.Clear();
        foreach (var viagem in consulta)
        {
            Itens.Add(viagem);
        }

        ResumoLista = Itens.Count == 1 ? "1 viagem" : $"{Itens.Count} viagens";
    }

    private void AtualizarAvisoOffline()
    {
        var pendentes = _todas.Count(v => v.PendenteSincronizacao);
        Offline = !_connectivity.EstaOnline() || pendentes > 0;

        if (!_connectivity.EstaOnline())
        {
            AvisoOffline = pendentes == 0
                ? "Sem conexão. Você pode abrir e encerrar viagens; os apontamentos ficam salvos no aparelho e sobem quando a rede voltar."
                : $"Sem conexão. {pendentes} apontamento(s) salvos no aparelho, aguardando a rede voltar.";
        }
        else if (pendentes > 0)
        {
            AvisoOffline = $"{pendentes} apontamento(s) ainda não confirmados pelo servidor. Puxe para atualizar e reenviar.";
        }
        else
        {
            AvisoOffline = string.Empty;
        }
    }

    [RelayCommand]
    private async Task AbrirAsync() => await Shell.Current.GoToAsync(Rotas.ViagemForm);

    [RelayCommand]
    private async Task EncerrarAsync(Viagem viagem) =>
        await Shell.Current.GoToAsync($"{Rotas.EncerrarViagem}?id={viagem.Id}");

    [RelayCommand]
    private async Task CancelarAsync(Viagem viagem)
    {
        var confirmou = await Shell.Current.DisplayAlert(
            "Cancelar viagem",
            $"Cancelar a viagem {viagem.TrajetoTexto}? O veículo volta a ficar disponível e nada de quilometragem é contado.",
            "Cancelar viagem", "Voltar");

        if (!confirmou)
        {
            return;
        }

        try
        {
            await _viagemService.CancelarAsync(viagem.Id);
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível cancelar a viagem.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
}
