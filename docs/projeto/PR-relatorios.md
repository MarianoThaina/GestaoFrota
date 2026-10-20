# Relatórios financeiros (US11, US12, US14)

## O que entra

**API**
- `GET /api/relatorios/resumo?meses=6`: fluxo mensal (entradas, saídas, saldo do mês, realizado), despesas por categoria (total e %) e indicadores (resultado do período, a receber e a pagar no mês, pendentes em atraso, maior despesa, variação de saídas contra o mês anterior). Resposta única para a tela fazer uma requisição só.
- `GET /api/relatorios/exportar?meses=6`: CSV das movimentações do período, separador `;` e BOM UTF-8 (abre no Excel em português sem configuração), com totais no rodapé.
- Regra de agregação pura em `GestaoFrota.Domain/Services/CalculadoraRelatorios.cs`, sem acesso a banco, no mesmo modelo da `CalculadoraFinanceira`.
- Só Administrador, Gestor de Frota e Financeiro. Motorista recebe 403.

**App (MAUI Android)**
- `RelatoriosPage`: seletor de 3 / 6 / 12 meses, cards de indicadores, gráfico de colunas de entradas x saídas por mês, rosca de despesas por categoria (até 5 categorias + "Outras"), botão de exportar que abre a folha de compartilhamento do Android.
- Gráficos desenhados com `GraphicsView` nativo (`GestaoFrota.App/Controls/`), sem pacote novo.
- Item "Relatórios" no menu lateral, escondido para o Motorista no mesmo método que esconde os outros módulos financeiros.

**Docs**
- Tabela de user stories em `docs/projeto/estado-atual.md`, endpoints e decisões no `README.md`, exemplos no `.http`.

## Decisões

- Período = últimos N meses **incluindo o atual**, agrupado por **data de vencimento** (competência), igual ao filtro da lista de movimentações. Pagos e pendentes entram; estornadas nunca.
- Ranking de rotas e eficiência por veículo (US10 e US13) ficam de fora: a frota ainda não tem endpoint. A tela mostra o bloco como "aguardando módulo de frota" em vez de dado falso.
- PDF não entra; CSV cumpre a US14 sem dependência nova.

## Como testar

1. `dotnet build GestaoFrota.API` e `dotnet build GestaoFrota.App -f net10.0-android`. O código foi escrito sem SDK na máquina de autoria, então a primeira compilação é o primeiro teste.
2. Subir a API, logar no Swagger com um JWT de Administrador e chamar `GET /api/relatorios/resumo?meses=6`. Conferir: lista `fluxoMensal` com exatamente 6 itens (meses zerados incluídos), `despesasPorCategoria` ordenada da maior para a menor somando 100%, `indicadores.quantidadeLancamentos` igual ao número de movimentações não estornadas do período.
3. Chamar `GET /api/relatorios/exportar?meses=6`, salvar e abrir no Excel: colunas separadas, acentos corretos, totais no rodapé.
4. Logar com um JWT de Motorista: os dois endpoints devem responder 403.
5. No emulador, como Administrador: abrir *Relatórios* no menu. Trocar entre 3 / 6 / 12 meses e ver o gráfico mudar de quantidade de colunas. Lançar uma despesa nova em outra categoria e puxar para atualizar: a rosca ganha uma fatia.
6. Tocar em *Exportar período em CSV*: deve abrir a folha de compartilhamento do Android com o arquivo.
7. Logar como Motorista: *Relatórios* não aparece no menu.

## O que não foi conferido pelo autor

- Compilação e execução (sem SDK .NET e sem emulador na máquina de autoria).
- Sentido do arco da rosca no Android (`DrawArc` com `clockwise: true` partindo de 90°). Se as fatias saírem espelhadas, inverter o sinal de `varredura` em `GraficoRoscaDrawable`.
