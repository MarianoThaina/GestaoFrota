using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoFrota.App.Controls;
using GestaoFrota.App.Helpers;
using GestaoFrota.App.Models;
using GestaoFrota.App.Services;

namespace GestaoFrota.App.ViewModels;

/// <summary>
/// Relatórios (US11, US12 e US14): indicadores do período, gráfico de colunas
/// de entradas e saídas por mês, rosca de despesas por categoria e exportação
/// em CSV. O período é escolhido em 3, 6 ou 12 meses, incluindo o atual.
/// Só perfis financeiros chegam aqui — o menu esconde a tela do Motorista e a
/// API devolve 403 de qualquer forma.
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Periodo3Selecionado))]
    [NotifyPropertyChangedFor(nameof(Periodo6Selecionado))]
    [NotifyPropertyChangedFor(nameof(Periodo12Selecionado))]
    private int meses = 6;

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

    public ObservableCollection<FluxoMensal> Fluxo { get; } = new();
    public ObservableCollection<DespesaCategoria> Categorias { get; } = new();

    public bool Periodo3Selecionado => Meses == 3;
    public bool Periodo6Selecionado => Meses == 6;
    public bool Periodo12Selecionado => Meses == 12;

    public string PeriodoTexto => Resumo?.PeriodoTexto ?? string.Empty;

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
            var resumo = await _relatorioService.ObterResumoAsync(Meses);
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
    }

    /// <summary>Troca o período (3, 6 ou 12 meses) e recarrega.</summary>
    [RelayCommand]
    private async Task SelecionarPeriodoAsync(string valor)
    {
        if (!int.TryParse(valor, out var novo) || novo == Meses)
        {
            return;
        }

        Meses = novo;
        await CarregarAsync();
    }

    /// <summary>
    /// US14: baixa o CSV do período para o cache do app e abre a folha de
    /// compartilhamento do sistema (e-mail, Drive, WhatsApp...). Não precisa
    /// de permissão de armazenamento porque o arquivo fica na pasta do app.
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
            var arquivo = await _relatorioService.ExportarCsvAsync(Meses);

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
        var categorias = AgruparCategorias(resumo.DespesasPorCategoria);
        for (var i = 0; i < categorias.Count; i++)
        {
            categorias[i].Cor = PaletaCategorias[Math.Min(i, PaletaCategorias.Length - 1)];
        }

        Categorias.Clear();
        foreach (var categoria in categorias)
        {
            Categorias.Add(categoria);
        }

        SemDados = resumo.Indicadores.QuantidadeLancamentos == 0;
        SemDespesas = categorias.Count == 0;

        // Trocar a instância do drawable faz o GraphicsView redesenhar via binding.
        GraficoColunas = new GraficoColunasDrawable(resumo.FluxoMensal);
        GraficoRosca = new GraficoRoscaDrawable(categorias, Formatacao.Moeda(resumo.Indicadores.TotalSaidas));
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
