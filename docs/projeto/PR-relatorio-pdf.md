# Exportação do relatório em PDF (US14)

O professor exigiu a exportação em PDF. Até aqui a US14 saía só em CSV.

## O que entra

**API**
- Pacote `QuestPDF` 2026.9.1 no `GestaoFrota.API.csproj` e a licença Community declarada no `Program.cs` (o QuestPDF exige essa linha antes do primeiro documento; é gratuita para projeto acadêmico).
- `Services/LinhaExportacao.cs`: a linha da exportação (`LinhaExportacao`) e os totais (`TotaisExportacao`). CSV e PDF usam a mesma lista e os mesmos totais, então os números nunca divergem entre os dois formatos.
- `Services/RelatorioPdf.cs`: monta o documento.
  - Cabeçalho: "Relatório financeiro", período (rótulo e datas), "Gerado em dd/MM/aaaa às HH:mm" no horário de Brasília e o nome de quem gerou (claim `Name` do JWT).
  - Resumo em quatro cartões: total de entradas, total de saídas, resultado do período (verde se ≥ 0, vermelho se negativo) e quantidade de movimentações.
  - Tabela: vencimento, tipo, descrição, categoria, forma de pagamento, situação, pagamento e valor (despesa com "−" e em vermelho). A4 paisagem; o cabeçalho da tabela se repete em cada página; linhas zebradas.
  - Período sem movimentações: os cartões zerados e o texto "Nenhuma movimentação no período.", sem erro.
  - Rodapé: "Gestão de Frota · Página X de Y".
  - Gráficos não entram: são desenhados no app (`GraphicsView`) e não existem no servidor.
- `RelatoriosController.Exportar`: novo parâmetro `formato` (`csv` padrão, `pdf`; qualquer outro → 400 `{ mensagem }`). A consulta saiu para `CarregarLinhasExportacaoAsync`. Mesma autorização de antes (Administrador, Gestor de Frota, Financeiro; Motorista → 403). Nome do arquivo: `relatorio-financeiro-AAAAMMDD-AAAAMMDD.pdf`.

**App**
- `IRelatorioService.ExportarCsvAsync(filtro)` virou `ExportarAsync(filtro, FormatoExportacao)`; o enum `FormatoExportacao` está em `Models/Relatorio.cs`.
- `RelatoriosViewModel.ExportarAsync(string? formato)` recebe o formato pelo `CommandParameter`.
- `RelatoriosPage.xaml`: o botão único "Exportar período em CSV" virou dois, **PDF** (cheio) e **CSV (planilha)** (contorno), sob o título "Exportar o período filtrado". A folha de compartilhamento já aceita `application/pdf`.

**Docs**: exemplo no `GestaoFrota.API.http`, endpoint e decisão no `README.md`, linha da US14 e atualização de 03/11 no `estado-atual.md`.

## Como testar

1. `dotnet restore` e `dotnet build` na API e no App (o restore baixa o QuestPDF). `dotnet test` continua igual: nada do Domain mudou.
2. Swagger, logado como Administrador ou Financeiro: `GET /api/relatorios/exportar?meses=6&formato=pdf` → baixar e abrir o PDF.
   - Acentos certos ("Relatório", "Situação", "Pedágio", "Combustível").
   - Os totais do resumo batem com as três últimas linhas do CSV do mesmo período (`...&formato=csv` ou sem `formato`).
   - Nome do arquivo com as datas do período.
3. `?tudo=true&formato=pdf` com bastante movimentação: mais de uma página, cabeçalho azul da tabela repetido no topo de cada uma, "Página X de Y" no rodapé.
4. Período vazio, por exemplo `?ano=2020&formato=pdf`: PDF abre com cartões em R$ 0,00 e "Nenhuma movimentação no período."
5. `?formato=xls` → 400 com "Formato inválido. Use csv ou pdf."
6. Logado como Motorista → 403 (igual ao CSV).
7. Emulador Android: Relatórios → filtrar um período → **PDF** → abre a folha de compartilhamento com `relatorio-financeiro-...pdf`; salvar no Drive ou abrir num leitor. Repetir com **CSV (planilha)** para ver que o caminho antigo continua funcionando.

## O que não foi conferido pelo autor

- Compilação e execução (sem SDK .NET e sem emulador nesta máquina).
- As assinaturas do QuestPDF usadas (`Document.Create`, `Page`, `Table`/`Header`/`Cell`, `Row`/`RelativeItem`, `Text`/`Span`/`CurrentPageNumber`/`TotalPages`, `Background`/`BorderColor` com `Color`) foram conferidas por reflexão na DLL do pacote 2026.9.1, não por compilação. Se o compilador reclamar, o ponto de contato é só `RelatorioPdf.cs`.
- A fonte padrão do QuestPDF cobre os acentos do português; isso é o comportamento documentado, não foi visto num PDF gerado aqui.
- `CommandParameter` como `string` num `[RelayCommand]` com parâmetro: padrão do CommunityToolkit.Mvvm, não testado neste app.
