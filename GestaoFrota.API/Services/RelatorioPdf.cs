using System.Globalization;
using GestaoFrota.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GestaoFrota.API.Services;

/// <summary>
/// Monta o PDF do relatório financeiro (US14) com QuestPDF: cabeçalho com
/// período e autoria, resumo (entradas, saídas, resultado) e a tabela das
/// movimentações. A4 paisagem para as oito colunas caberem sem quebrar texto;
/// o cabeçalho da tabela se repete em cada página e o rodapé numera as páginas.
/// Os gráficos da tela não entram: são desenhados no app (GraphicsView).
/// </summary>
public static class RelatorioPdf
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    private const string AzulMarinho = "#0B1D3A";
    private const string Verde = "#1B7F3B";
    private const string Vermelho = "#B3261E";

    public static byte[] Gerar(
        PeriodoRelatorio periodo,
        IReadOnlyList<LinhaExportacao> linhas,
        string? geradoPor,
        DateTime geradoEmUtc)
    {
        var totais = TotaisExportacao.De(linhas);

        return Document.Create(documento =>
        {
            documento.Page(pagina =>
            {
                pagina.Size(PageSizes.A4.Landscape());
                pagina.Margin(28);
                pagina.PageColor(Colors.White);
                pagina.DefaultTextStyle(estilo => estilo.FontSize(9).FontColor(Colors.Grey.Darken4));

                pagina.Header().Element(c => Cabecalho(c, periodo, geradoPor, geradoEmUtc));

                pagina.Content().PaddingVertical(12).Column(coluna =>
                {
                    coluna.Spacing(14);
                    coluna.Item().Element(c => Resumo(c, totais, linhas.Count));

                    if (linhas.Count == 0)
                    {
                        coluna.Item().PaddingTop(20).AlignCenter()
                            .Text(t => t.Span("Nenhuma movimentação no período.").Italic().FontColor(Colors.Grey.Darken1));
                    }
                    else
                    {
                        coluna.Item().Element(c => Tabela(c, linhas));
                    }
                });

                pagina.Footer().AlignCenter().Text(t =>
                {
                    t.DefaultTextStyle(estilo => estilo.FontSize(8).FontColor(Colors.Grey.Darken1));
                    t.Span("Gestão de Frota · Página ");
                    t.CurrentPageNumber();
                    t.Span(" de ");
                    t.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static void Cabecalho(IContainer container, PeriodoRelatorio periodo, string? geradoPor, DateTime geradoEmUtc)
    {
        // A API grava e compara datas em UTC; a hora impressa vai no fuso de Brasília.
        var geradoEmLocal = ParaHorarioDeBrasilia(geradoEmUtc);

        container.BorderBottom(1.5f).BorderColor(AzulMarinho).PaddingBottom(8).Row(linha =>
        {
            linha.RelativeItem().Column(coluna =>
            {
                coluna.Item().Text("Relatório financeiro").FontSize(18).Bold().FontColor(AzulMarinho);
                coluna.Item().Text(t =>
                {
                    t.Span("Período: ").SemiBold();
                    t.Span($"{periodo.Rotulo} ({periodo.Inicio.ToString("dd/MM/yyyy", PtBr)} a {periodo.UltimoDia.ToString("dd/MM/yyyy", PtBr)})");
                });
                coluna.Item().Text("Movimentações agrupadas pela data de vencimento; estornadas não entram.")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
            });

            linha.ConstantItem(220).AlignRight().Column(coluna =>
            {
                coluna.Item().AlignRight().Text($"Gerado em {geradoEmLocal.ToString("dd/MM/yyyy 'às' HH:mm", PtBr)}")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
                if (!string.IsNullOrWhiteSpace(geradoPor))
                {
                    coluna.Item().AlignRight().Text($"por {geradoPor}").FontSize(8).FontColor(Colors.Grey.Darken1);
                }
            });
        });
    }

    private static void Resumo(IContainer container, TotaisExportacao totais, int quantidade)
    {
        container.Row(linha =>
        {
            linha.Spacing(10);
            Cartao(linha.RelativeItem(), "Total de entradas", Moeda(totais.Entradas), Verde);
            Cartao(linha.RelativeItem(), "Total de saídas", Moeda(totais.Saidas), Vermelho);
            Cartao(linha.RelativeItem(), "Resultado do período", Moeda(totais.Resultado),
                totais.Resultado >= 0 ? Verde : Vermelho);
            Cartao(linha.RelativeItem(), "Movimentações", quantidade.ToString(PtBr), AzulMarinho);
        });
    }

    private static void Cartao(IContainer container, string titulo, string valor, string cor)
    {
        container.Border(0.75f).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(coluna =>
        {
            coluna.Item().Text(titulo).FontSize(8).FontColor(Colors.Grey.Darken1);
            coluna.Item().Text(valor).FontSize(14).Bold().FontColor(cor);
        });
    }

    private static void Tabela(IContainer container, IReadOnlyList<LinhaExportacao> linhas)
    {
        container.Table(tabela =>
        {
            tabela.ColumnsDefinition(colunas =>
            {
                colunas.ConstantColumn(62);  // Vencimento
                colunas.ConstantColumn(52);  // Tipo
                colunas.RelativeColumn(3);   // Descrição
                colunas.RelativeColumn(2);   // Categoria
                colunas.RelativeColumn(2);   // Forma de pagamento
                colunas.ConstantColumn(78);  // Situação
                colunas.ConstantColumn(62);  // Pagamento
                colunas.ConstantColumn(80);  // Valor
            });

            // Header() repete esta linha no topo de cada página da tabela.
            tabela.Header(cabecalho =>
            {
                CelulaCabecalho(cabecalho.Cell(), "Vencimento");
                CelulaCabecalho(cabecalho.Cell(), "Tipo");
                CelulaCabecalho(cabecalho.Cell(), "Descrição");
                CelulaCabecalho(cabecalho.Cell(), "Categoria");
                CelulaCabecalho(cabecalho.Cell(), "Forma de pagamento");
                CelulaCabecalho(cabecalho.Cell(), "Situação");
                CelulaCabecalho(cabecalho.Cell(), "Pagamento");
                CelulaCabecalho(cabecalho.Cell(), "Valor", alinharDireita: true);
            });

            for (var i = 0; i < linhas.Count; i++)
            {
                var l = linhas[i];
                Color fundo = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                var receita = l.Tipo == TipoMovimentacao.Receita;

                Celula(tabela.Cell(), fundo, l.DataVencimento.ToString("dd/MM/yyyy", PtBr));
                Celula(tabela.Cell(), fundo, receita ? "Receita" : "Despesa");
                Celula(tabela.Cell(), fundo, l.Descricao ?? "—");
                Celula(tabela.Cell(), fundo, l.Categoria);
                Celula(tabela.Cell(), fundo, l.FormaPagamento);
                Celula(tabela.Cell(), fundo, l.Status == StatusMovimentacao.Pago ? "Pago/Recebido" : "Pendente");
                Celula(tabela.Cell(), fundo, l.DataPagamento?.ToString("dd/MM/yyyy", PtBr) ?? "—");
                tabela.Cell().Background(fundo).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                    .PaddingVertical(4).PaddingHorizontal(4).AlignRight()
                    .Text((receita ? "" : "− ") + Moeda(l.Valor))
                    .FontColor(receita ? Verde : Vermelho);
            }
        });
    }

    private static void CelulaCabecalho(IContainer container, string texto, bool alinharDireita = false)
    {
        var celula = container.Background(AzulMarinho).PaddingVertical(5).PaddingHorizontal(4);
        if (alinharDireita)
        {
            celula = celula.AlignRight();
        }

        celula.Text(texto).FontColor(Colors.White).SemiBold();
    }

    private static void Celula(IContainer container, Color fundo, string texto)
    {
        container.Background(fundo).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(4).PaddingHorizontal(4)
            .Text(texto);
    }

    private static string Moeda(decimal valor) => valor.ToString("C", PtBr);

    /// <summary>
    /// "E. South America Standard Time" no Windows, "America/Sao_Paulo" no
    /// Linux/macOS. O .NET 6+ converte entre os dois nomes, mas o fallback para
    /// UTC−3 fixo evita erro em servidor sem a base de fusos.
    /// </summary>
    private static DateTime ParaHorarioDeBrasilia(DateTime utc)
    {
        try
        {
            var fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), fuso);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return utc.AddHours(-3);
        }
    }
}
