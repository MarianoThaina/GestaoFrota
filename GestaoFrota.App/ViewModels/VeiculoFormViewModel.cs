using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Helpers;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

/// <summary>
/// Cadastro e edição de veículo (US06): placa (antiga ou Mercosul), marca,
/// modelo, ano de fabricação, capacidade de carga e hodômetro atual.
/// Abre em modo edição quando chega com "?id=".
/// </summary>
public partial class VeiculoFormViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IVeiculoService _veiculoService;

    private Guid? _id;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Titulo))]
    private bool edicao;

    [ObservableProperty]
    private string placaTexto = string.Empty;

    [ObservableProperty]
    private string marca = string.Empty;

    [ObservableProperty]
    private string modelo = string.Empty;

    [ObservableProperty]
    private string anoTexto = string.Empty;

    [ObservableProperty]
    private string capacidadeTexto = string.Empty;

    [ObservableProperty]
    private string kmAtualTexto = string.Empty;

    [ObservableProperty]
    private bool ativo = true;

    public string Titulo => Edicao ? "Editar veículo" : "Novo veículo";

    public VeiculoFormViewModel(IVeiculoService veiculoService)
    {
        _veiculoService = veiculoService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var valor) && Guid.TryParse(valor?.ToString(), out var id))
        {
            _id = id;
            Edicao = true;
            _ = CarregarAsync(id);
        }
    }

    private async Task CarregarAsync(Guid id)
    {
        Carregando = true;
        MensagemErro = null;

        try
        {
            var veiculo = await _veiculoService.ObterLocalAsync(id);
            if (veiculo is null)
            {
                MensagemErro = "Veículo não encontrado. Volte e puxe para atualizar a lista.";
                return;
            }

            PlacaTexto = veiculo.PlacaTexto;
            Marca = veiculo.Marca;
            Modelo = veiculo.Modelo;
            AnoTexto = veiculo.Ano.ToString(CultureInfo.InvariantCulture);
            CapacidadeTexto = NumeroTexto(veiculo.CapacidadeCargaToneladas);
            KmAtualTexto = NumeroTexto(veiculo.KmAtual);
            Ativo = veiculo.Ativo;
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

        MensagemErro = Validar(out var dados);
        if (MensagemErro is not null)
        {
            return;
        }

        Carregando = true;

        try
        {
            if (_id.HasValue)
            {
                await _veiculoService.AtualizarAsync(_id.Value, dados);
            }
            else
            {
                await _veiculoService.CriarAsync(dados);
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível salvar o veículo.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            Carregando = false;
        }
    }

    private string? Validar(out DadosVeiculo dados)
    {
        dados = new DadosVeiculo();

        if (!Placa.EhValida(PlacaTexto))
        {
            return "Placa inválida. Use ABC-1234 (antiga) ou ABC1D23 (Mercosul).";
        }

        if (string.IsNullOrWhiteSpace(Marca))
        {
            return "Informe a marca.";
        }

        if (string.IsNullOrWhiteSpace(Modelo))
        {
            return "Informe o modelo.";
        }

        if (!int.TryParse(AnoTexto?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var ano)
            || ano < 1950 || ano > DateTime.Today.Year + 1)
        {
            return $"Informe um ano de fabricação entre 1950 e {DateTime.Today.Year + 1}.";
        }

        if (!ParserMoeda.TentarConverter(CapacidadeTexto, out var capacidade) || capacidade <= 0m)
        {
            return "Informe a capacidade de carga em toneladas (ex.: 32 ou 27,5).";
        }

        if (!ParserMoeda.TentarConverter(string.IsNullOrWhiteSpace(KmAtualTexto) ? "0" : KmAtualTexto, out var km))
        {
            return "Informe o hodômetro atual em km, sem sinal negativo (ex.: 184.320).";
        }

        dados.Placa = Placa.Normalizar(PlacaTexto);
        dados.Marca = Marca.Trim();
        dados.Modelo = Modelo.Trim();
        dados.Ano = ano;
        dados.CapacidadeCargaToneladas = capacidade;
        dados.KmAtual = km;
        dados.Ativo = Ativo;

        return null;
    }

    private static string NumeroTexto(decimal valor) =>
        valor == decimal.Truncate(valor)
            ? valor.ToString("N0", Formatacao.PtBr)
            : valor.ToString("N2", Formatacao.PtBr);
}
