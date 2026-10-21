using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Helpers;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

/// <summary>
/// Rotas (US07), no mesmo desenho da tela de Categorias: formulário de criação
/// no topo, edição inline e desativação por deslize. Administrador e Gestor de
/// Frota gerenciam; desativar é só do Administrador.
/// </summary>
public partial class RotasViewModel : BaseViewModel
{
    private readonly IRotaService _rotaService;
    private readonly IAuthService _authService;

    public ObservableCollection<Rota> Rotas { get; } = new();

    [ObservableProperty]
    private string novoNome = string.Empty;

    [ObservableProperty]
    private string novaOrigem = string.Empty;

    [ObservableProperty]
    private string novoDestino = string.Empty;

    [ObservableProperty]
    private string novaDistanciaTexto = string.Empty;

    [ObservableProperty]
    private bool podeGerenciar;

    [ObservableProperty]
    private bool podeDesativar;

    [ObservableProperty]
    private Rota? rotaEmEdicao;

    [ObservableProperty]
    private string edicaoNome = string.Empty;

    [ObservableProperty]
    private string edicaoOrigem = string.Empty;

    [ObservableProperty]
    private string edicaoDestino = string.Empty;

    [ObservableProperty]
    private string edicaoDistanciaTexto = string.Empty;

    public RotasViewModel(IRotaService rotaService, IAuthService authService)
    {
        _rotaService = rotaService;
        _authService = authService;
    }

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

            var rotas = await _rotaService.ListarAsync();

            Rotas.Clear();
            foreach (var rota in rotas)
            {
                Rotas.Add(rota);
            }
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível carregar as rotas.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            Carregando = false;
        }
    }

    [RelayCommand]
    private async Task AdicionarAsync()
    {
        MensagemErro = Validar(NovoNome, NovaOrigem, NovoDestino, NovaDistanciaTexto, out var dados);
        if (MensagemErro is not null)
        {
            return;
        }

        try
        {
            var rota = await _rotaService.CriarAsync(dados);
            Rotas.Add(rota);

            NovoNome = string.Empty;
            NovaOrigem = string.Empty;
            NovoDestino = string.Empty;
            NovaDistanciaTexto = string.Empty;
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível cadastrar a rota.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    [RelayCommand]
    private void IniciarEdicao(Rota rota)
    {
        RotaEmEdicao = rota;
        EdicaoNome = rota.Nome;
        EdicaoOrigem = rota.Origem;
        EdicaoDestino = rota.Destino;
        EdicaoDistanciaTexto = rota.DistanciaEstimadaKm.ToString("N1", Formatacao.PtBr);
    }

    [RelayCommand]
    private void CancelarEdicao()
    {
        RotaEmEdicao = null;
    }

    [RelayCommand]
    private async Task SalvarEdicaoAsync()
    {
        if (RotaEmEdicao is null)
        {
            return;
        }

        MensagemErro = Validar(EdicaoNome, EdicaoOrigem, EdicaoDestino, EdicaoDistanciaTexto, out var dados);
        if (MensagemErro is not null)
        {
            return;
        }

        dados.Ativa = RotaEmEdicao.Ativa;

        try
        {
            await _rotaService.AtualizarAsync(RotaEmEdicao.Id, dados);

            var atualizada = new Rota
            {
                Id = RotaEmEdicao.Id,
                Nome = dados.Nome,
                Origem = dados.Origem,
                Destino = dados.Destino,
                DistanciaEstimadaKm = dados.DistanciaEstimadaKm,
                Ativa = dados.Ativa
            };

            var indice = Rotas.IndexOf(RotaEmEdicao);
            if (indice >= 0)
            {
                Rotas[indice] = atualizada;
            }

            RotaEmEdicao = null;
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível salvar a rota.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    [RelayCommand]
    private async Task DesativarAsync(Rota rota)
    {
        try
        {
            await _rotaService.DesativarAsync(rota.Id);
            rota.Ativa = false;

            var indice = Rotas.IndexOf(rota);
            if (indice >= 0)
            {
                Rotas[indice] = rota;
            }
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível desativar a rota.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    private static string? Validar(string nome, string origem, string destino, string distanciaTexto, out DadosRota dados)
    {
        dados = new DadosRota();

        if (string.IsNullOrWhiteSpace(nome))
        {
            return "Informe o nome da rota.";
        }

        if (string.IsNullOrWhiteSpace(origem) || string.IsNullOrWhiteSpace(destino))
        {
            return "Informe a origem e o destino.";
        }

        if (!ParserMoeda.TentarConverter(distanciaTexto, out var distancia) || distancia <= 0m)
        {
            return "Informe a distância estimada em km (ex.: 408).";
        }

        dados.Nome = nome.Trim();
        dados.Origem = origem.Trim();
        dados.Destino = destino.Trim();
        dados.DistanciaEstimadaKm = distancia;

        return null;
    }
}
