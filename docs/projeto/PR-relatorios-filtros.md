# Filtros de período e toque nos gráficos (sobras das US11 e US12)

Fecha os três itens que o levantamento de 20/10 apontou como faltantes na tela
de Relatórios: filtro por mês, ano e período livre; períodos de 1 ano, 2 anos e
máximo; toque no gráfico mostrando o valor exato.

## O que entra

**Domínio**
- `CalculadoraRelatorios.FluxoPorMes`, `DespesasPorCategoria` e `Indicadores` ganham sobrecargas com `inicio`/`fim` explícitos (início inclusivo, fim exclusivo, qualquer dia). As versões `(hoje, meses)` continuam e delegam, então os testes do PR #15 seguem valendo.
- Nos indicadores, "a receber/a pagar no mês", "em atraso" e a variação de saídas olham o **mês corrente** independentemente do filtro: são indicadores do momento, não do período.

**API**
- `Services/PeriodoRelatorio.cs`: resolve os parâmetros `meses`, `tudo`, `ano`, `mes`, `de`, `ate` num período com rótulo. Prioridade: livre > mês/ano > ano > tudo > meses. "Tudo" começa no vencimento mais antigo (ou na primeira viagem, no `/frota`).
- `/resumo`, `/frota` e `/exportar` aceitam os mesmos parâmetros. A resposta do resumo ganhou `periodoRotulo`; o CSV ganhou a primeira linha com o período e o nome do arquivo com as datas.

**App**
- `Models/FiltroPeriodo.cs` (record) vira query string e rótulo.
- `RelatoriosPage`: atalhos 3 m / 6 m / 1 ano / 2 anos / Tudo; botão "Personalizar" abre o painel com pickers de mês (ou "Ano inteiro") e ano, e dois `DatePicker` para período livre (limite de 3 anos; acima disso, "Tudo").
- Toque: `GraphicsView.StartInteraction` entrega o ponto; `GraficoColunasDrawable.IndiceNoToque` converte X em mês e `GraficoRoscaDrawable.IndiceNoToque` converte o ângulo em fatia. O mês tocado fica destacado com os valores acima das colunas e uma linha de texto embaixo; a fatia tocada esmaece as outras e mostra categoria e percentual no centro. Tocar de novo limpa. A legenda da rosca também seleciona (tap).
- Gráfico de colunas com mais de 12 meses mostra um rótulo sim, outro não, para 24 meses e "Tudo" não virarem uma faixa ilegível.

## Como testar

1. `dotnet build` na API e no App. Depois `dotnet test` (os testes de relatórios do PR #15 continuam passando: as sobrecargas antigas delegam para as novas).
2. Swagger: `GET /api/relatorios/resumo?ano=2026&mes=10` → `periodoInicio` 01/10, `periodoFim` 31/10, `fluxoMensal` com 1 item, `periodoRotulo` "10/2026". `?ano=2026` → 12 itens. `?de=2026-09-15&ate=2026-10-20` → 2 itens (set e out), `quantidadeLancamentos` só do intervalo. `?tudo=true` → começa no mês do vencimento mais antigo. `?meses=24` → 24 itens.
3. `GET /api/relatorios/exportar?de=2026-09-15&ate=2026-10-20`: primeira linha "Periodo;15/09/2026 a 20/10/2026;15/09/2026;20/10/2026", nome `relatorio-financeiro-20260915-20261020.csv`.
4. Emulador: tocar "2 anos" e "Tudo" e ver a quantidade de colunas mudar. "Personalizar" → Outubro/2026 → Aplicar: um mês só no gráfico, rótulo "Out/2026". Mês "Ano inteiro" + 2025 → 12 colunas. Período livre com a data final antes da inicial → mensagem de erro sem chamar a API.
5. Tocar numa coluna: fundo claro no mês, valores acima do par e a linha "Out/2026: + R$ ... · - R$ ... · saldo ...". Tocar de novo limpa. Tocar na rosca: as outras fatias esmaecem e o centro mostra a categoria e o %. Tocar na legenda faz o mesmo.
6. Exportar com "Tudo": o CSV traz todas as movimentações não estornadas.

## O que não foi conferido pelo autor

- Compilação e execução (sem SDK .NET e sem emulador).
- O nome do evento `StartInteraction` e o tipo `TouchEventArgs` com `Touches` (`PointF[]`) do `GraphicsView`; se o compilador reclamar, é o único ponto de contato com a API de toque, em `RelatoriosPage.xaml.cs`.
- O sentido do ângulo em `GraficoRoscaDrawable.IndiceNoToque` assume o mesmo sentido horário a partir do topo que o desenho usa; se as fatias do desenho estiverem espelhadas (risco já anotado no PR #13), o toque espelha junto e os dois se corrigem no mesmo lugar.
