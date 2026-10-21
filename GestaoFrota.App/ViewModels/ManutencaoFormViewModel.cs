using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Helpers;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

/// <summary>
/// Registro de manutenção (US06-b): veículo (vem da tela de Frota), data,
/// descrição do serviço, valor total e forma de pagamento da despesa que a API
/// gera automaticamente. Ao salvar, o veículo fica "Em manutenção".
/// </summary>
public partial class ManutencaoFormViewModel : BaseViewModel, IQueryAttributable
{
    private const decimal ValorMaximo = 999_999_999_999m;

    private readonly IManutencaoService _manutencaoService;
    private readonly IVeiculoService _veiculoService;
    private readonly IFormaPagamentoService _formaPagamentoService;

    public ObservableCollection<FormaPagamento> Formas { get; } = new();

    [ObservableProperty]
    private Veiculo? veiculo;

    [ObservableProperty]
    private DateTime data = DateTime.Today;

    [ObservableProperty]
    private string descricao = string.Empty;

    [ObservableProperty]
    private string valorTexto = string.Empty;

    [ObservableProperty]
    private FormaPagamento? formaSelecionada;

    [ObservableProperty]
    private bool pago;

    [ObservableProperty]
    private DateTime dataPagamento = DateTime.Today;

    public DateTime DataMaxima => DateTime.Today;

    public ManutencaoFormViewModel(
        IManutencaoService manutencaoService,
        IVeiculoService veiculoService,
        IFormaPagamentoService formaPagamentoService)
    {
        _manutencaoService = manutencaoService;
        _veiculoService = veiculoService;
        _formaPagamentoService = formaPagamentoService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("veiculoId", out var valor) && Guid.TryParse(valor?.ToString(), out var veiculoId))
        {
            _ = CarregarAsync(veiculoId);
        }
    }

    private async Task CarregarAsync(Guid veiculoId)
    {
        Carregando = true;
        MensagemErro = null;

        try
        {
            Veiculo = await _veiculoService.ObterLocalAsync(veiculoId);
            if (Veiculo is null)
            {
                MensagemErro = "Veículo não encontrado. Volte e puxe para atualizar a lista.";
                return;
            }

            var formas = (await _formaPagamentoService.ListarAsync()).Where(f => f.Ativo).ToList();
            Formas.Clear();
            foreach (var forma in formas)
            {
                Formas.Add(forma);
            }
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível carregar as formas de pagamento.");
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
        if (Carregando || Veiculo is null)
        {
            return;
        }

        MensagemErro = Validar(out var valor);
        if (MensagemErro is not null)
        {
            return;
        }

        Carregando = true;

        try
        {
            var manutencao = await _manutencaoService.CriarAsync(new NovaManutencao
            {
                VeiculoId = Veiculo.Id,
                Data = Data.Date,
                Descricao = Descricao.Trim(),
                ValorTotal = valor,
                FormaPagamentoId = FormaSelecionada!.Id,
                Pago = Pago,
                DataPagamento = Pago ? DataPagamento.Date : null
            });

            await Shell.Current.DisplayAlert(
                "Manutenção registrada",
                $"O veículo {Veiculo.PlacaTexto} está em manutenção e a despesa de {manutencao.ValorTexto} já foi lançada no caixa automaticamente.",
                "OK");

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível registrar a manutenção.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            Carregando = false;
        }
    }

    private string? Validar(out decimal valor)
    {
        valor = 0m;

        if (string.IsNullOrWhiteSpace(Descricao))
        {
            return "Descreva o serviço realizado.";
        }

        if (Descricao.Trim().Length > 300)
        {
            return "A descrição aceita no máximo 300 caracteres.";
        }

        if (!ParserMoeda.TentarConverter(ValorTexto, out valor) || valor <= 0m)
        {
            return "Informe o valor total da manutenção (ex.: 5.400,00).";
        }

        if (valor > ValorMaximo)
        {
            return "O valor informado é alto demais.";
        }

        if (FormaSelecionada is null)
        {
            return "Escolha a forma de pagamento da despesa.";
        }

        if (Data.Date > DateTime.Today)
        {
            return "A data da manutenção não pode estar no futuro.";
        }

        if (Pago && DataPagamento.Date > DateTime.Today)
        {
            return "A data do pagamento não pode estar no futuro.";
        }

        return null;
    }
}
