using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Controls;
using GestaoFrota.App.Helpers;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

/// <summary>
/// Relatórios (US10 a US14): indicadores do período, gráfico de colunas de
/// entradas e saídas por mês, rosca de despesas por categoria, estatísticas e
/// ranking de frota e exportação em CSV. O período pode ser 3, 6, 12 ou 24
/// meses, todo o histórico, um mês/ano, um ano ou um intervalo livre; o toque
/// nos gráficos mostra o valor exato. Só perfis financeiros chegam aqui.
/// </summary>
public partial class RelatoriosViewModel : BaseViewModel
{
    /// <summary>Paleta da rosca, na ordem do protótipo (tons de azul, do mais forte ao mais claro).</summary>
    private static readonly Color[] PaletaCategorias =
    {
        Color.FromArgb("#0B5FA5"),
        Color.FromArgb("#4C8FD1"),
        Color.FromArgb("#7FB2E4"),
        Color.FromArgb("#B3D3F2"),
        Color.FromArgb("#D9E7F8"),
        Color.FromArgb("#9AA7B8"),
    };

    /// <summary>Acima deste número, as categorias menores viram "Outras" na rosca.</summary>
    private const int MaximoCategoriasNaRosca = 5;

    private readonly IRelatorioService _relatorioService;

    private List<DespesaCategoria> _categoriasRosca = new();

    // ---------- Período ----------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Periodo3Selecionado))]
    [NotifyPropertyChangedFor(nameof(Periodo6Selecionado))]
    [NotifyPropertyChangedFor(nameof(Periodo12Selecionado))]
    [NotifyPropertyChangedFor(nameof(Periodo24Selecionado))]
    [NotifyPropertyChangedFor(nameof(PeriodoTudoSelecionado))]
    [NotifyPropertyChangedFor(nameof(PeriodoPersonalizadoSelecionado))]
    [NotifyPropertyChangedFor(nameof(FiltroTexto))]
    private FiltroPeriodo filtro = FiltroPeriodo.UltimosMeses(6);

    /// <summary>Painel com mês/ano e período livre, aberto pelo botão "Personalizar".</summary>
    [ObservableProperty]
    private bool mostrarPersonalizado;

    public string[] NomesMeses { get; } =
        { "Ano inteiro", "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho", "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro" };

    public ObservableCollection<int> Anos { get; } = new(Enumerable.Range(0, 6).Select(i => DateTime.Today.Year - i));

    [ObservableProperty]
    private int anoSelecionado = DateTime.Today.Year;

    /// <summary>Índice em NomesMeses: 0 = ano inteiro, 1..12 = mês.</summary>
    [ObservableProperty]
    private int mesSelecionadoIndice = DateTime.Today.Month;

    [ObservableProperty]
    private DateTime dataDe = DateTime.Today.AddMonths(-1);

    [ObservableProperty]
    private DateTime dataAte = DateTime.Today;

    public DateTime DataMaxima => DateTime.Today;

    public bool Periodo3Selecionado => Filtro.Tipo == TipoFiltroPeriodo.UltimosMeses && Filtro.Meses == 3;
    public bool Periodo6Selecionado => Filtro.Tipo == TipoFiltroPeriodo.UltimosMeses && Filtro.Meses == 6;
    public bool Periodo12Selecionado => Filtro.Tipo == TipoFiltroPeriodo.UltimosMeses && Filtro.Meses == 12;
    public bool Periodo24Selecionado => Filtro.Tipo == TipoFiltroPeriodo.UltimosMeses && Filtro.Meses == 24;
    public bool PeriodoTudoSelecionado => Filtro.Tipo == TipoFiltroPeriodo.Tudo;
    public bool PeriodoPersonalizadoSelecionado => Filtro.Tipo is TipoFiltroPeriodo.Ano or TipoFiltroPeriodo.MesAno or TipoFiltroPeriodo.Livre;

    public string FiltroTexto => Filtro.Rotulo;

    // ---------- Dados ----------

    [ObservableProperty]
    private RelatorioResumo? resumo;

    [ObservableProperty]
    private IndicadoresRelatorio indicadores = new();

    [ObservableProperty]
    private IDrawable graficoColunas = new GraficoColunasDrawable(Array.Empty<FluxoMensal>());

    [ObservableProperty]
    private IDrawable graficoRosca = new GraficoRoscaDrawable(Array.Empty<DespesaCategoria>(), string.Empty);

    [ObservableProperty]
    private bool semDados;

    [ObservableProperty]
    private bool semDespesas;

    [ObservableProperty]
    private bool exportando;

    [ObservableProperty]
    private string? mensagemExportacao;

    // ---------- Toque nos gráficos ----------

    [ObservableProperty]
    private int? mesTocado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SemMesTocado))]
    private string? mesTocadoTexto;

    public bool SemMesTocado => string.IsNullOrEmpty(MesTocadoTexto);

    [ObservableProperty]
    private int? categoriaTocada;

    [ObservableProperty]
    private string? categoriaTocadaTexto;

    // ---------- US10 / US13 ----------

    [ObservableProperty]
    private EstatisticasFrota frota = new();

    [ObservableProperty]
    private bool semViagens;

    [ObservableProperty]
    private bool semRanking;

    [ObservableProperty]
    private string? mensagemFrota;

    public ObservableCollection<FluxoMensal> Fluxo { get; } = new();
    public ObservableCollection<DespesaCategoria> Categorias { get; } = new();
    public ObservableCollection<EstatisticaRota> TopRotas { get; } = new();
    public ObservableCollection<EstatisticaVeiculo> TopVeiculos { get; } = new();

    public string PeriodoTexto => Resumo?.PeriodoTexto ?? Filtro.Rotulo;

    public RelatoriosViewModel(IRelatorioService relatorioService)
    {
        _relatorioService = relatorioService;
    }

    [RelayCommand]
    private async Task CarregarAsync()
    {
        if (Carregando)
        {
            return;
        }

        Carregando = true;
        MensagemErro = null;
        MensagemExportacao = null;

        try
        {
            var resumo = await _relatorioService.ObterResumoAsync(Filtro);
            AplicarResumo(resumo);
        }
        catch (Exception ex)
        {
            MensagemErro = ErrosUi.Mensagem(ex, "Não foi possível carregar os relatórios.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            Carregando = false;
        }

        // US10/US13 em chamada separada: uma falha aqui não derruba o financeiro.
        await CarregarFrotaAsync();
    }

    // ---------- Seleção de período ----------

    /// <summary>Atalhos: "3", "6", "12", "24" meses ou "tudo".</summary>
    [RelayCommand]
    private async Task SelecionarPeriodoAsync(string valor)
    {
        var novo = valor == "tudo"
            ? FiltroPeriodo.Tudo()
            : int.TryParse(valor, out var meses) ? FiltroPeriodo.UltimosMeses(meses) : null;

        if (novo is null || novo == Filtro)
        {
            return;
        }

        MostrarPersonalizado = false;
        await AplicarFiltroAsync(novo);
    }

    [RelayCommand]
    private void AlternarPersonalizado() => MostrarPersonalizado = !MostrarPersonalizado;

    /// <summary>Mês/ano escolhido nos pickers (índice 0 = ano inteiro).</summary>
    [RelayCommand]
    private async Task AplicarMesAnoAsync()
    {
        var novo = MesSelecionadoIndice <= 0
            ? FiltroPeriodo.DoAno(AnoSelecionado)
            : FiltroPeriodo.DoMes(AnoSelecionado, MesSelecionadoIndice);

        await AplicarFiltroAsync(novo);
    }

    /// <summary>Intervalo livre entre duas datas.</summary>
    [RelayCommand]
    private async Task AplicarPeriodoLivreAsync()
    {
        if (DataAte.Date < DataDe.Date)
        {
            MensagemErro = "A data final não pode ser anterior à inicial.";
            return;
        }

        if ((DataAte.Date - DataDe.Date).TotalDays > 366 * 3)
        {
            MensagemErro = "Escolha um intervalo de até 3 anos, ou use \"Tudo\".";
            return;
        }

        await AplicarFiltroAsync(FiltroPeriodo.Entre(DataDe, DataAte));
    }

    private async Task AplicarFiltroAsync(FiltroPeriodo novo)
    {
        Filtro = novo;
        MesTocado = null;
        MesTocadoTexto = null;
        CategoriaTocada = null;
        CategoriaTocadaTexto = null;
        await CarregarAsync();
    }

    // ---------- Toque nos gráficos ----------

    /// <summary>Chamado pela página com a posição do toque no gráfico de colunas.</summary>
    public void TocarColunas(float x, float larguraTotal)
    {
        if (Resumo is null)
        {
            return;
        }

        var indice = GraficoColunasDrawable.IndiceNoToque(x, larguraTotal, Resumo.FluxoMensal.Count);

        // Tocar de novo no mesmo mês limpa a seleção.
        MesTocado = indice == MesTocado ? null : indice;

        if (MesTocado is { } i)
        {
            var mes = Resumo.FluxoMensal[i];
            MesTocadoTexto = $"{mes.MesTexto}: {mes.EntradasTexto} · {mes.SaidasTexto} · saldo {mes.SaldoDoMesTexto}";
        }
        else
        {
            MesTocadoTexto = null;
        }

        GraficoColunas = new GraficoColunasDrawable(Resumo.FluxoMensal, MesTocado);
    }

    /// <summary>Chamado pela página com a posição do toque na rosca.</summary>
    public void TocarRosca(float x, float y, float largura, float altura)
    {
        if (Resumo is null || _categoriasRosca.Count == 0)
        {
            return;
        }

        var indice = GraficoRoscaDrawable.IndiceNoToque(x, y, largura, altura, _categoriasRosca);
        CategoriaTocada = indice == CategoriaTocada ? null : indice;

        if (CategoriaTocada is { } i)
        {
            var categoria = _categoriasRosca[i];
            CategoriaTocadaTexto = $"{categoria.Categoria}: {categoria.TotalTexto} ({categoria.PercentualTexto} das despesas)";
        }
        else
        {
            CategoriaTocadaTexto = null;
        }

        GraficoRosca = new GraficoRoscaDrawable(_categoriasRosca, Formatacao.Moeda(Resumo.Indicadores.TotalSaidas), CategoriaTocada);
    }

    /// <summary>A legenda também seleciona a fatia.</summary>
    [RelayCommand]
    private void SelecionarCategoria(DespesaCategoria categoria)
    {
        var indice = _categoriasRosca.IndexOf(categoria);
        if (indice < 0 || Resumo is null)
        {
            return;
        }

        CategoriaTocada = indice == CategoriaTocada ? null : indice;
        CategoriaTocadaTexto = CategoriaTocada.HasValue
            ? $"{categoria.Categoria}: {categoria.TotalTexto} ({categoria.PercentualTexto} das despesas)"
            : null;

        GraficoRosca = new GraficoRoscaDrawable(_categoriasRosca, Formatacao.Moeda(Resumo.Indicadores.TotalSaidas), CategoriaTocada);
    }

    // ---------- Exportação (US14) ----------

    /// <summary>
    /// Baixa o CSV do período filtrado para o cache do app e abre a folha de
    /// compartilhamento do sistema (e-mail, Drive, WhatsApp...).
    /// </summary>
    [RelayCommand]
    private async Task ExportarAsync()
    {
        if (Exportando)
        {
            return;
        }

        Exportando = true;
        MensagemExportacao = null;

        try
        {
            var arquivo = await _relatorioService.ExportarCsvAsync(Filtro);

            var caminho = Path.Combine(FileSystem.CacheDirectory, arquivo.NomeArquivo);
            await File.WriteAllBytesAsync(caminho, arquivo.Conteudo);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Exportar relatório financeiro",
                File = new ShareFile(caminho, arquivo.ContentType)
            });

            MensagemExportacao = $"Arquivo {arquivo.NomeArquivo} gerado.";
        }
        catch (Exception ex)
        {
            MensagemExportacao = ErrosUi.Mensagem(ex, "Não foi possível exportar o relatório.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            Exportando = false;
        }
    }

    // ---------- Montagem ----------

    private void AplicarResumo(RelatorioResumo resumo)
    {
        Resumo = resumo;
        Indicadores = resumo.Indicadores;
        OnPropertyChanged(nameof(PeriodoTexto));

        Fluxo.Clear();
        foreach (var mes in resumo.FluxoMensal)
        {
            Fluxo.Add(mes);
        }

        // Rosca: no máximo 5 categorias nomeadas; o resto vira "Outras" para a
        // legenda caber na tela e as fatias continuarem legíveis.
        _categoriasRosca = AgruparCategorias(resumo.DespesasPorCategoria);
        for (var i = 0; i < _categoriasRosca.Count; i++)
        {
            _categoriasRosca[i].Cor = PaletaCategorias[Math.Min(i, PaletaCategorias.Length - 1)];
        }

        Categorias.Clear();
        foreach (var categoria in _categoriasRosca)
        {
            Categorias.Add(categoria);
        }

        SemDados = resumo.Indicadores.QuantidadeLancamentos == 0;
        SemDespesas = _categoriasRosca.Count == 0;

        // Trocar a instância do drawable faz o GraphicsView redesenhar via binding.
        MesTocado = null;
        MesTocadoTexto = null;
        CategoriaTocada = null;
        CategoriaTocadaTexto = null;
        GraficoColunas = new GraficoColunasDrawable(resumo.FluxoMensal);
        GraficoRosca = new GraficoRoscaDrawable(_categoriasRosca, Formatacao.Moeda(resumo.Indicadores.TotalSaidas));
    }

    private async Task CarregarFrotaAsync()
    {
        MensagemFrota = null;

        try
        {
            var frota = await _relatorioService.ObterFrotaAsync(Filtro);
            Frota = frota;

            TopRotas.Clear();
            for (var i = 0; i < frota.RotasMaisLucrativas.Count; i++)
            {
                frota.RotasMaisLucrativas[i].Posicao = i + 1;
                TopRotas.Add(frota.RotasMaisLucrativas[i]);
            }

            TopVeiculos.Clear();
            for (var i = 0; i < frota.VeiculosMaisEficientes.Count; i++)
            {
                frota.VeiculosMaisEficientes[i].Posicao = i + 1;
                TopVeiculos.Add(frota.VeiculosMaisEficientes[i]);
            }

            SemViagens = frota.TotalViagens == 0;
            SemRanking = TopRotas.Count == 0 && TopVeiculos.Count == 0;
        }
        catch (Exception ex)
        {
            MensagemFrota = ErrosUi.Mensagem(ex, "Não foi possível carregar as estatísticas de frota.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    private static List<DespesaCategoria> AgruparCategorias(IReadOnlyList<DespesaCategoria> origem)
    {
        if (origem.Count <= MaximoCategoriasNaRosca)
        {
            return origem.ToList();
        }

        var principais = origem.Take(MaximoCategoriasNaRosca - 1).ToList();
        var restantes = origem.Skip(MaximoCategoriasNaRosca - 1).ToList();

        principais.Add(new DespesaCategoria
        {
            Categoria = $"Outras ({restantes.Count})",
            Total = restantes.Sum(c => c.Total),
            Percentual = Math.Round(restantes.Sum(c => c.Percentual), 1)
        });

        return principais;
    }
}
