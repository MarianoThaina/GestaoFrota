using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Helpers;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

/// <summary>
/// Formulário de nova movimentação (US03): tipo, valor, descrição, categoria,
/// forma de pagamento, vencimento, status Pendente/Pago (com data de pagamento)
/// e comprovante em imagem (câmera ou galeria).
/// </summary>
public partial class MovimentacaoFormViewModel : BaseViewModel
{
    private const decimal ValorMaximo = 999_999_999_999m;

    private readonly IMovimentacaoService _movimentacaoService;
    private readonly ICategoriaService _categoriaService;
    private readonly IFormaPagamentoService _formaPagamentoService;

    private List<Categoria> _todasCategorias = new();

    public TipoMovimentacao[] Tipos { get; } = { TipoMovimentacao.Receita, TipoMovimentacao.Despesa };

    public ObservableCollection<Categoria> Categorias { get; } = new();

    public ObservableCollection<FormaPagamento> Formas { get; } = new();

    [ObservableProperty]
    private TipoMovimentacao tipo = TipoMovimentacao.Despesa;

    [ObservableProperty]
    private string valorTexto = string.Empty;

    [ObservableProperty]
    private string descricao = string.Empty;

    [ObservableProperty]
    private Categoria? categoriaSelecionada;

    [ObservableProperty]
    private FormaPagamento? formaSelecionada;

    [ObservableProperty]
    private DateTime dataVencimento = DateTime.Today;

    /// <summary>False = Pendente; True = Pago/Recebido (exige a data de pagamento).</summary>
    [ObservableProperty]
    private bool pago;

    [ObservableProperty]
    private DateTime dataPagamento = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemComprovante))]
    private string? comprovanteCaminho;

    [ObservableProperty]
    private ImageSource? comprovantePreview;

    public bool TemComprovante => ComprovanteCaminho is not null;

    /// <summary>Impede escolher uma data de pagamento futura no DatePicker.</summary>
    public DateTime DataMaximaPagamento => DateTime.Today;

    public string RotuloPago => Tipo == TipoMovimentacao.Receita ? "Já recebido" : "Já pago";

    public MovimentacaoFormViewModel(
        IMovimentacaoService movimentacaoService,
        ICategoriaService categoriaService,
        IFormaPagamentoService formaPagamentoService)
    {
        _movimentacaoService = movimentacaoService;
        _categoriaService = categoriaService;
        _formaPagamentoService = formaPagamentoService;
    }

    partial void OnTipoChanged(TipoMovimentacao value)
    {
        OnPropertyChanged(nameof(RotuloPago));
        FiltrarCategorias();
    }

    [RelayCommand]
    private async Task CarregarAsync()
    {
        Carregando = true;
        MensagemErro = null;

        try
        {
            _todasCategorias = (await _categoriaService.ListarAsync()).Where(c => c.Ativo).ToList();
            FiltrarCategorias();

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

    /// <summary>Só mostra categorias do mesmo tipo da movimentação (Receita/Despesa).</summary>
    private void FiltrarCategorias()
    {
        var alvo = Tipo == TipoMovimentacao.Receita ? TipoCategoria.Receita : TipoCategoria.Despesa;

        Categorias.Clear();
        foreach (var categoria in _todasCategorias.Where(c => c.Tipo == alvo))
        {
            Categorias.Add(categoria);
        }

        CategoriaSelecionada = null;
    }

    // ---------------- Comprovante ----------------

    [RelayCommand]
    private async Task TirarFotoAsync()
    {
        try
        {
            MensagemErro = null;

            if (!MediaPicker.Default.IsCaptureSupported)
            {
                MensagemErro = "Este dispositivo não tem câmera disponível. Use a galeria.";
                return;
            }

            var permissao = await Permissions.RequestAsync<Permissions.Camera>();
            if (permissao != PermissionStatus.Granted)
            {
                MensagemErro = "Permita o uso da câmera para fotografar o comprovante.";
                return;
            }

            var foto = await MediaPicker.Default.CapturePhotoAsync();
            await DefinirComprovanteAsync(foto);
        }
        catch (Exception ex)
        {
            MensagemErro = "Não foi possível usar a câmera.";
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    [RelayCommand]
    private async Task EscolherGaleriaAsync()
    {
        try
        {
            MensagemErro = null;

            var foto = await MediaPicker.Default.PickPhotoAsync();
            await DefinirComprovanteAsync(foto);
        }
        catch (Exception ex)
        {
            MensagemErro = "Não foi possível abrir a galeria.";
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    [RelayCommand]
    private void RemoverComprovante()
    {
        ComprovanteCaminho = null;
        ComprovantePreview = null;
    }

    private async Task DefinirComprovanteAsync(FileResult? arquivo)
    {
        if (arquivo is null)
        {
            return; // usuário cancelou
        }

        // Copia para o cache do app: o caminho original pode ser temporário
        // ou um content:// URI, e o upload precisa ler o arquivo depois.
        var extensao = Path.GetExtension(arquivo.FileName);
        var destino = Path.Combine(FileSystem.CacheDirectory, $"comprovante_{Guid.NewGuid():N}{extensao}");

        await using (var origem = await arquivo.OpenReadAsync())
        await using (var saida = System.IO.File.Create(destino))
        {
            await origem.CopyToAsync(saida);
        }

        if (new FileInfo(destino).Length > MovimentacaoService.TamanhoMaximoComprovanteBytes)
        {
            System.IO.File.Delete(destino);
            MensagemErro = "A imagem excede 5 MB. Escolha uma foto menor.";
            return;
        }

        ComprovanteCaminho = destino;
        ComprovantePreview = ImageSource.FromFile(destino);
    }

    // ---------------- Salvar ----------------

    [RelayCommand]
    private async Task SalvarAsync()
    {
        if (Carregando)
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
            var criada = await _movimentacaoService.CriarAsync(new NovaMovimentacao
            {
                Tipo = Tipo,
                Valor = valor,
                Descricao = Descricao.Trim(),
                CategoriaId = CategoriaSelecionada!.Id,
                FormaPagamentoId = FormaSelecionada!.Id,
                DataVencimento = DataVencimento.Date,
                Pago = Pago,
                DataPagamento = Pago ? DataPagamento.Date : null
            });

            if (ComprovanteCaminho is not null)
            {
                try
                {
                    await _movimentacaoService.EnviarComprovanteAsync(criada.Id, ComprovanteCaminho);
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert(
                        "Comprovante não enviado",
                        $"A movimentação foi salva, mas o comprovante falhou: {ErrosUi.Mensagem(ex, "erro inesperado")}",
                        "OK");
                }
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível salvar a movimentação.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            Carregando = false;
        }
    }

    /// <summary>Validações de entrada (valores negativos e inconsistências). Retorna a mensagem de erro ou null.</summary>
    private string? Validar(out decimal valor)
    {
        valor = 0m;

        if (string.IsNullOrWhiteSpace(Descricao))
        {
            return "Informe a descrição.";
        }

        if (Descricao.Trim().Length > 300)
        {
            return "A descrição aceita no máximo 300 caracteres.";
        }

        if (!ParserMoeda.TentarConverter(ValorTexto, out valor))
        {
            return "Informe um valor válido, sem sinal negativo (ex.: 1.250,00).";
        }

        if (valor <= 0m)
        {
            return "O valor deve ser maior que zero.";
        }

        if (valor > ValorMaximo)
        {
            return "O valor informado é alto demais.";
        }

        if (CategoriaSelecionada is null)
        {
            return "Escolha a categoria.";
        }

        if (FormaSelecionada is null)
        {
            return "Escolha a forma de pagamento.";
        }

        if (Pago && DataPagamento.Date > DateTime.Today)
        {
            return "A data do pagamento não pode estar no futuro.";
        }

        return null;
    }
}
