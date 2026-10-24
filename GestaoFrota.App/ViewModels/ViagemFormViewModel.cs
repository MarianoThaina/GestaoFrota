using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Helpers;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

/// <summary>
/// Abrir viagem (US08): veículo disponível, rota, hodômetro inicial (sugerido a
/// partir do último hodômetro do veículo) e observações. Gestores podem abrir a
/// viagem em nome de um motorista; o Motorista abre sempre para si. Funciona
/// sem internet com os veículos e rotas do cache.
/// </summary>
public partial class ViagemFormViewModel : BaseViewModel
{
    private readonly IViagemService _viagemService;
    private readonly IVeiculoService _veiculoService;
    private readonly IRotaService _rotaService;
    private readonly IAuthService _authService;
    private readonly IConnectivityService _connectivity;

    private UsuarioLogado? _usuario;

    public ObservableCollection<Veiculo> Veiculos { get; } = new();

    public ObservableCollection<Rota> Rotas { get; } = new();

    public ObservableCollection<Motorista> Motoristas { get; } = new();

    [ObservableProperty]
    private Veiculo? veiculoSelecionado;

    [ObservableProperty]
    private Rota? rotaSelecionada;

    [ObservableProperty]
    private Motorista? motoristaSelecionado;

    /// <summary>Só Administrador e Gestor de Frota escolhem o motorista (e só online).</summary>
    [ObservableProperty]
    private bool podeEscolherMotorista;

    [ObservableProperty]
    private string hodometroInicialTexto = string.Empty;

    [ObservableProperty]
    private string observacoes = string.Empty;

    [ObservableProperty]
    private bool offline;

    [ObservableProperty]
    private string? dicaHodometro;

    public ViagemFormViewModel(
        IViagemService viagemService,
        IVeiculoService veiculoService,
        IRotaService rotaService,
        IAuthService authService,
        IConnectivityService connectivity)
    {
        _viagemService = viagemService;
        _veiculoService = veiculoService;
        _rotaService = rotaService;
        _authService = authService;
        _connectivity = connectivity;
    }

    partial void OnVeiculoSelecionadoChanged(Veiculo? value)
    {
        if (value is null)
        {
            DicaHodometro = null;
            return;
        }

        // Sugere o último hodômetro conhecido; o motorista confere no painel e ajusta.
        HodometroInicialTexto = value.KmAtual == decimal.Truncate(value.KmAtual)
            ? value.KmAtual.ToString("N0", Formatacao.PtBr)
            : value.KmAtual.ToString("N1", Formatacao.PtBr);

        DicaHodometro = $"Último hodômetro registrado: {Formatacao.Km(value.KmAtual)}. Confira no painel antes de confirmar.";
    }

    [RelayCommand]
    private async Task CarregarAsync()
    {
        Carregando = true;
        MensagemErro = null;

        try
        {
            Offline = !_connectivity.EstaOnline();

            _usuario = await _authService.ObterUsuarioLogadoAsync();
            var gestor = _usuario?.Perfil is PerfilUsuario.Administrador or PerfilUsuario.GestorDeFrota;

            var veiculos = (await _veiculoService.ListarAsync()).Where(v => v.Disponivel).ToList();
            Veiculos.Clear();
            foreach (var veiculo in veiculos)
            {
                Veiculos.Add(veiculo);
            }

            var rotas = (await _rotaService.ListarAsync()).Where(r => r.Ativa).ToList();
            Rotas.Clear();
            foreach (var rota in rotas)
            {
                Rotas.Add(rota);
            }

            Motoristas.Clear();
            PodeEscolherMotorista = false;
            if (gestor && !Offline)
            {
                foreach (var motorista in await _viagemService.ListarMotoristasAsync())
                {
                    Motoristas.Add(motorista);
                }

                PodeEscolherMotorista = Motoristas.Count > 0;
                MotoristaSelecionado = Motoristas.FirstOrDefault(m => m.Id == _usuario!.Id) ?? Motoristas.FirstOrDefault();
            }

            if (Veiculos.Count == 0)
            {
                MensagemErro = Offline
                    ? "Nenhum veículo disponível no cache deste aparelho. Conecte-se uma vez para baixar a frota."
                    : "Nenhum veículo disponível. Cadastre um veículo em Frota ou encerre a viagem em andamento.";
            }
            else if (Rotas.Count == 0)
            {
                MensagemErro = Offline
                    ? "Nenhuma rota no cache deste aparelho. Conecte-se uma vez para baixar as rotas."
                    : "Nenhuma rota ativa. Cadastre uma rota antes de abrir a viagem.";
            }
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível carregar veículos e rotas.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            Carregando = false;
        }
    }

    [RelayCommand]
    private async Task SalvarAsync()
    {
        if (Carregando)
        {
            return;
        }

        MensagemErro = Validar(out var hodometro);
        if (MensagemErro is not null)
        {
            return;
        }

        Carregando = true;

        try
        {
            var motoristaId = PodeEscolherMotorista && MotoristaSelecionado is not null
                ? MotoristaSelecionado.Id
                : _usuario!.Id;
            var motoristaNome = PodeEscolherMotorista && MotoristaSelecionado is not null
                ? MotoristaSelecionado.Nome
                : _usuario!.Nome;

            var viagem = await _viagemService.AbrirAsync(new NovaViagem
            {
                Veiculo = VeiculoSelecionado!,
                Rota = RotaSelecionada!,
                MotoristaId = motoristaId,
                MotoristaNome = motoristaNome,
                HodometroInicial = hodometro,
                Observacoes = string.IsNullOrWhiteSpace(Observacoes) ? null : Observacoes.Trim()
            });

            if (viagem.AberturaPendente)
            {
                await Shell.Current.DisplayAlert(
                    "Viagem aberta no aparelho",
                    "Sem conexão agora. A viagem ficou salva neste aparelho e será enviada ao servidor assim que a rede voltar.",
                    "OK");
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível abrir a viagem.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            Carregando = false;
        }
    }

    private string? Validar(out decimal hodometro)
    {
        hodometro = 0m;

        if (_usuario is null)
        {
            return "Não foi possível identificar o usuário logado.";
        }

        if (VeiculoSelecionado is null)
        {
            return "Escolha o veículo.";
        }

        if (RotaSelecionada is null)
        {
            return "Escolha a rota.";
        }

        if (!ParserMoeda.TentarConverter(HodometroInicialTexto, out hodometro))
        {
            return "Informe o hodômetro inicial em km, sem sinal negativo (ex.: 184.320).";
        }

        if (Observacoes?.Length > 500)
        {
            return "As observações aceitam no máximo 500 caracteres.";
        }

        return null;
    }
}
