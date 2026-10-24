using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Helpers;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

/// <summary>
/// Frota (US06 e US06-b): lista os veículos com situação (disponível, em viagem,
/// em manutenção), filtro por situação, cadastro/edição, registro e conclusão
/// de manutenção. Administrador e Gestor de Frota gerenciam; Motorista só consulta.
/// </summary>
public partial class FrotaViewModel : BaseViewModel
{
    private readonly IVeiculoService _veiculoService;
    private readonly IManutencaoService _manutencaoService;
    private readonly IAuthService _authService;

    private List<Veiculo> _todos = new();

    public ObservableCollection<Veiculo> Itens { get; } = new();

    public string[] OpcoesFiltro { get; } = { "Todos", "Disponíveis", "Em viagem", "Em manutenção" };

    [ObservableProperty]
    private string filtro = "Todos";

    [ObservableProperty]
    private string resumoLista = string.Empty;

    /// <summary>Administrador e Gestor de Frota cadastram, editam e registram manutenção.</summary>
    [ObservableProperty]
    private bool podeGerenciar;

    /// <summary>Só o Administrador desativa (DELETE /veiculos/{id}).</summary>
    [ObservableProperty]
    private bool podeDesativar;

    public FrotaViewModel(IVeiculoService veiculoService, IManutencaoService manutencaoService, IAuthService authService)
    {
        _veiculoService = veiculoService;
        _manutencaoService = manutencaoService;
        _authService = authService;
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
            PodeGerenciar = usuario?.Perfil is PerfilUsuario.Administrador or PerfilUsuario.GestorDeFrota;
            PodeDesativar = usuario?.Perfil is PerfilUsuario.Administrador;

            _todos = await _veiculoService.ListarAsync();
            AplicarFiltro();
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível carregar a frota.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            Carregando = false;
        }
    }

    private void AplicarFiltro()
    {
        IEnumerable<Veiculo> consulta = Filtro switch
        {
            "Disponíveis" => _todos.Where(v => v.Disponivel),
            "Em viagem" => _todos.Where(v => v.Status == StatusVeiculo.EmViagem),
            "Em manutenção" => _todos.Where(v => v.Status == StatusVeiculo.EmManutencao),
            _ => _todos
        };

        Itens.Clear();
        foreach (var veiculo in consulta)
        {
            Itens.Add(veiculo);
        }

        var emViagem = _todos.Count(v => v.Status == StatusVeiculo.EmViagem);
        ResumoLista = Itens.Count == 1 ? "1 veículo" : $"{Itens.Count} veículos";
        ResumoLista += $" · {emViagem} em viagem";
    }

    [RelayCommand]
    private async Task NovoAsync() => await Shell.Current.GoToAsync(Rotas.VeiculoForm);

    [RelayCommand]
    private async Task EditarAsync(Veiculo veiculo) =>
        await Shell.Current.GoToAsync($"{Rotas.VeiculoForm}?id={veiculo.Id}");

    [RelayCommand]
    private async Task RegistrarManutencaoAsync(Veiculo veiculo) =>
        await Shell.Current.GoToAsync($"{Rotas.ManutencaoForm}?veiculoId={veiculo.Id}");

    /// <summary>Conclui as manutenções abertas do veículo e o devolve a "Disponível".</summary>
    [RelayCommand]
    private async Task ConcluirManutencaoAsync(Veiculo veiculo)
    {
        var confirmou = await Shell.Current.DisplayAlert(
            "Concluir manutenção",
            $"Liberar o veículo {veiculo.PlacaTexto} da oficina? Ele volta a ficar disponível para viagens.",
            "Liberar", "Cancelar");

        if (!confirmou)
        {
            return;
        }

        try
        {
            var abertas = await _manutencaoService.ListarAsync(veiculo.Id, abertas: true);
            if (abertas.Count == 0)
            {
                MensagemErro = "Não há manutenção aberta para este veículo. Puxe para atualizar a lista.";
                return;
            }

            foreach (var manutencao in abertas)
            {
                await _manutencaoService.ConcluirAsync(manutencao.Id);
            }

            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível concluir a manutenção.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    [RelayCommand]
    private async Task DesativarAsync(Veiculo veiculo)
    {
        var confirmou = await Shell.Current.DisplayAlert(
            "Desativar veículo",
            $"O veículo {veiculo.PlacaTexto} sai das viagens novas, mas o histórico continua. Desativar?",
            "Desativar", "Cancelar");

        if (!confirmou)
        {
            return;
        }

        try
        {
            await _veiculoService.DesativarAsync(veiculo.Id);
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível desativar o veículo.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
}
