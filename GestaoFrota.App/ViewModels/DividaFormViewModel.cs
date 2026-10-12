using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Helpers;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

/// <summary>
/// Cadastro de dívida/financiamento (US04). Ao salvar, a API gera as N parcelas
/// pendentes (uma por mês) dentro de uma transação com rollback.
/// </summary>
public partial class DividaFormViewModel : BaseViewModel
{
    private const decimal ValorMaximo = 999_999_999_999m;

    private readonly IDividaService _dividaService;
    private readonly ICategoriaService _categoriaService;
    private readonly IFormaPagamentoService _formaPagamentoService;

    public ObservableCollection<Categoria> Categorias { get; } = new();

    public ObservableCollection<FormaPagamento> Formas { get; } = new();

    [ObservableProperty]
    private string descricao = string.Empty;

    [ObservableProperty]
    private string valorParcelaTexto = string.Empty;

    [ObservableProperty]
    private string numeroParcelasTexto = string.Empty;

    [ObservableProperty]
    private DateTime dataPrimeiroVencimento = DateTime.Today.AddMonths(1);

    [ObservableProperty]
    private string valorQuitacaoTexto = string.Empty;

    [ObservableProperty]
    private Categoria? categoriaSelecionada;

    [ObservableProperty]
    private FormaPagamento? formaSelecionada;

    /// <summary>Prévia: total e período, atualizada enquanto o usuário digita.</summary>
    [ObservableProperty]
    private string resumo = string.Empty;

    public DividaFormViewModel(
        IDividaService dividaService,
        ICategoriaService categoriaService,
        IFormaPagamentoService formaPagamentoService)
    {
        _dividaService = dividaService;
        _categoriaService = categoriaService;
        _formaPagamentoService = formaPagamentoService;
    }

    partial void OnValorParcelaTextoChanged(string value) => AtualizarResumo();

    partial void OnNumeroParcelasTextoChanged(string value) => AtualizarResumo();

    partial void OnDataPrimeiroVencimentoChanged(DateTime value) => AtualizarResumo();

    private void AtualizarResumo()
    {
        if (ParserMoeda.TentarConverter(ValorParcelaTexto, out var parcela) && parcela > 0m
            && int.TryParse(NumeroParcelasTexto, NumberStyles.None, CultureInfo.InvariantCulture, out var quantidade)
            && quantidade is >= 1 and <= 480)
        {
            var ultimo = DataPrimeiroVencimento.Date.AddMonths(quantidade - 1);
            Resumo = $"Total: {Formatacao.Moeda(parcela * quantidade)} · {quantidade} parcelas pendentes " +
                     $"({Formatacao.MesAno(DataPrimeiroVencimento.Year, DataPrimeiroVencimento.Month)} a " +
                     $"{Formatacao.MesAno(ultimo.Year, ultimo.Month)})";
        }
        else
        {
            Resumo = string.Empty;
        }
    }

    [RelayCommand]
    private async Task CarregarAsync()
    {
        Carregando = true;
        MensagemErro = null;

        try
        {
            var categorias = (await _categoriaService.ListarAsync())
                .Where(c => c.Ativo && c.Tipo == TipoCategoria.Despesa)
                .ToList();

            Categorias.Clear();
            foreach (var categoria in categorias)
            {
                Categorias.Add(categoria);
            }

            // Pré-seleciona "Financiamento" (categoria padrão), se existir.
            CategoriaSelecionada ??= categorias.FirstOrDefault(c =>
                string.Equals(c.Nome, "Financiamento", StringComparison.OrdinalIgnoreCase));

            var formas = (await _formaPagamentoService.ListarAsync()).Where(f => f.Ativo).ToList();
            Formas.Clear();
            foreach (var forma in formas)
            {
                Formas.Add(forma);
            }
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível carregar categorias e formas de pagamento.");
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

        MensagemErro = Validar(out var parcela, out var quantidade, out var quitacao);
        if (MensagemErro is not null)
        {
            return;
        }

        Carregando = true;

        try
        {
            var detalhe = await _dividaService.CriarAsync(new NovaDivida
            {
                Descricao = Descricao.Trim(),
                ValorParcela = parcela,
                NumeroParcelas = quantidade,
                DataPrimeiroVencimento = DataPrimeiroVencimento.Date,
                ValorQuitacaoAntecipada = quitacao,
                CategoriaId = CategoriaSelecionada!.Id,
                FormaPagamentoId = FormaSelecionada!.Id
            });

            // Fecha o formulário e abre a dívida com as parcelas geradas.
            await Shell.Current.GoToAsync("..");
            await Shell.Current.GoToAsync($"{Rotas.DividaDetalhe}?id={detalhe.Divida.Id}");
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível cadastrar a dívida.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            Carregando = false;
        }
    }

    private string? Validar(out decimal parcela, out int quantidade, out decimal? quitacao)
    {
        parcela = 0m;
        quantidade = 0;
        quitacao = null;

        if (string.IsNullOrWhiteSpace(Descricao))
        {
            return "Informe a descrição da dívida.";
        }

        if (Descricao.Trim().Length > 200)
        {
            return "A descrição aceita no máximo 200 caracteres.";
        }

        if (!ParserMoeda.TentarConverter(ValorParcelaTexto, out parcela) || parcela <= 0m)
        {
            return "Informe o valor da parcela (maior que zero, sem sinal negativo).";
        }

        if (parcela > ValorMaximo)
        {
            return "O valor da parcela é alto demais.";
        }

        if (!int.TryParse(NumeroParcelasTexto, NumberStyles.None, CultureInfo.InvariantCulture, out quantidade)
            || quantidade is < 1 or > 480)
        {
            return "A quantidade de parcelas deve ficar entre 1 e 480.";
        }

        if (!string.IsNullOrWhiteSpace(ValorQuitacaoTexto))
        {
            if (!ParserMoeda.TentarConverter(ValorQuitacaoTexto, out var q) || q > ValorMaximo)
            {
                return "Informe um valor de quitação antecipada válido, sem sinal negativo.";
            }

            quitacao = q;
        }

        if (CategoriaSelecionada is null)
        {
            return "Escolha a categoria das parcelas.";
        }

        if (FormaSelecionada is null)
        {
            return "Escolha a forma de pagamento das parcelas.";
        }

        return null;
    }
}
