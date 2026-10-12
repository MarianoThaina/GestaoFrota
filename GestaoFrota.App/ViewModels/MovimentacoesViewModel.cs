using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Helpers;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

public partial class MovimentacoesViewModel : BaseViewModel
{
    private readonly IMovimentacaoService _movimentacaoService;
    private readonly IAuthService _authService;

    private List<Movimentacao> _todas = new();

    public ObservableCollection<Movimentacao> Itens { get; } = new();

    public string[] OpcoesTipo { get; } = { "Todas", "Receitas", "Despesas" };

    public string[] OpcoesStatus { get; } = { "Todos", "Pendentes", "Pagas" };

    [ObservableProperty]
    private string filtroTipo = "Todas";

    [ObservableProperty]
    private string filtroStatus = "Todos";

    [ObservableProperty]
    private string resumoLista = string.Empty;

    /// <summary>Somente Administrador e Financeiro lançam, pagam e estornam (RBAC).</summary>
    [ObservableProperty]
    private bool podeGerenciar;

    public MovimentacoesViewModel(IMovimentacaoService movimentacaoService, IAuthService authService)
    {
        _movimentacaoService = movimentacaoService;
        _authService = authService;
    }

    partial void OnFiltroTipoChanged(string value) => AplicarFiltro();

    partial void OnFiltroStatusChanged(string value) => AplicarFiltro();

    [RelayCommand]
    private async Task CarregarAsync()
    {
        Carregando = true;
        MensagemErro = null;

        try
        {
            var usuario = await _authService.ObterUsuarioLogadoAsync();
            PodeGerenciar = usuario?.Perfil is PerfilUsuario.Administrador or PerfilUsuario.Financeiro;

            _todas = await _movimentacaoService.ListarAsync();
            AplicarFiltro();
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível carregar as movimentações.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            Carregando = false;
        }
    }

    private void AplicarFiltro()
    {
        IEnumerable<Movimentacao> consulta = _todas;

        consulta = FiltroTipo switch
        {
            "Receitas" => consulta.Where(m => m.Tipo == TipoMovimentacao.Receita),
            "Despesas" => consulta.Where(m => m.Tipo == TipoMovimentacao.Despesa),
            _ => consulta
        };

        consulta = FiltroStatus switch
        {
            "Pendentes" => consulta.Where(m => m.Status == StatusMovimentacao.Pendente && !m.Estornada),
            "Pagas" => consulta.Where(m => m.Status == StatusMovimentacao.Pago && !m.Estornada),
            _ => consulta
        };

        Itens.Clear();
        foreach (var movimentacao in consulta)
        {
            Itens.Add(movimentacao);
        }

        ResumoLista = Itens.Count == 1 ? "1 movimentação" : $"{Itens.Count} movimentações";
    }

    [RelayCommand]
    private async Task NovaAsync() => await Shell.Current.GoToAsync(Rotas.MovimentacaoForm);

    [RelayCommand]
    private async Task PagarAsync(Movimentacao movimentacao)
    {
        var verbo = movimentacao.Tipo == TipoMovimentacao.Receita ? "recebida" : "paga";
        var confirmou = await Shell.Current.DisplayAlert(
            "Confirmar",
            $"Marcar \"{movimentacao.Descricao}\" como {verbo} hoje ({Formatacao.Data(DateTime.Today)})?",
            "Sim", "Não");

        if (!confirmou)
        {
            return;
        }

        try
        {
            await _movimentacaoService.PagarAsync(movimentacao.Id, DateTime.Today);
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível atualizar a movimentação.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    [RelayCommand]
    private async Task EstornarAsync(Movimentacao movimentacao)
    {
        var confirmou = await Shell.Current.DisplayAlert(
            "Estornar movimentação",
            "O registro continua no histórico, mas deixa de contar no saldo e na projeção. Deseja estornar?",
            "Estornar", "Cancelar");

        if (!confirmou)
        {
            return;
        }

        try
        {
            await _movimentacaoService.EstornarAsync(movimentacao.Id);
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível estornar a movimentação.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    [RelayCommand]
    private async Task VerComprovanteAsync(Movimentacao movimentacao)
    {
        await Shell.Current.GoToAsync($"{Rotas.Comprovante}?id={movimentacao.Id}");
    }
}
