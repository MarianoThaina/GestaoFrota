# Estado atual do projeto

Retrato técnico do que está implementado, atualizado em 31/08/2026.

Serve para o time ter uma visão única do projeto sem precisar navegar o código,
e para planejar as próximas sprints sabendo o que já existe.

---

## Arquitetura

Solução .NET em quatro projetos, com separação por camada:

| Projeto | Responsabilidade |
|---|---|
| `GestaoFrota.Domain` | Entidades e enums. Sem dependência de infraestrutura |
| `GestaoFrota.Infrastructure` | EF Core, `AppDbContext`, configurações e migrations |
| `GestaoFrota.API` | ASP.NET Core, endpoints REST, autenticação e autorização |
| `GestaoFrota.App` | .NET MAUI — Android, iOS, macOS e Windows |

A separação está bem aplicada: o domínio não conhece o EF Core, a API não
conhece o app, e o app conversa com a API apenas por HTTP.

---

## Decisões de arquitetura que vale registrar

Algumas escolhas do projeto resolvem problemas que só apareceriam mais tarde.
Ficam documentadas aqui para não serem desfeitas sem intenção.

### Chave primária `Guid` gerada no cliente

`EntidadeBase` define `Id` como `Guid` com valor gerado na criação do objeto,
em vez de um inteiro autoincremental do banco.

Isso é o que viabiliza o requisito de **Offline-First**. Com identificador
sequencial do servidor, dois motoristas registrando viagens sem rede gerariam o
mesmo identificador e colidiriam na sincronização. Com `Guid`, cada registro
nasce único no próprio aparelho.

### Rastreabilidade em todas as entidades

`EntidadeBase` traz `DataCriacao` e `DataAtualizacao`, atendendo a exigência da
US03 de registrar automaticamente quando um lançamento foi incluído e alterado.

### Marcação de sincronização

`Movimentacao` e `Viagem` têm `SincronizadoOffline`, que é a base para a fila de
sincronização do app.

### Perfis conforme a US16

O enum `PerfilUsuario` define os quatro perfis exigidos: `Administrador`,
`GestorDeFrota`, `Financeiro` e `OperadorMotorista`. O diagrama inicial do
documento previa apenas dois — o código está mais completo que ele.

### Autorização validada no servidor

Os controllers usam `[Authorize(Roles = ...)]`, com os agrupamentos definidos em
`Security/Perfis.cs`. É o que a US18 exige: a permissão é conferida no momento
da execução, e não apenas escondida na interface.

### Integridade do histórico financeiro no próprio banco

As ligações de `Movimentacao` com `Categoria`, `FormaPagamento` e `Usuario` usam
`OnDelete(DeleteBehavior.Restrict)`. O banco impede exclusão em cascata, o que
reforça a estratégia de exclusão lógica.

Há também um check constraint `[Valor] > 0` na tabela de movimentações — a
validação de valor positivo existe na base, não só na tela.

### Fluxo de autenticação

O app abre o Google Sign-In, obtém o `id_token` e envia para
`POST /api/auth/google`. A API valida o token, cria ou atualiza o usuário e
devolve um JWT próprio, guardado pelo `TokenStore` em `SecureStorage`.

O aplicativo nunca manipula a senha do usuário, e a sessão persiste entre
aberturas — os dois critérios da US15.

---

## O que está implementado

### Banco de dados

Nove tabelas criadas na migration `InitialCreate`:

`Categorias` · `Dividas` · `FormasPagamento` · `Rotas` · `Usuarios` ·
`Veiculos` · `Viagens` · `Fretes` · `Movimentacoes`

Cada entidade tem sua classe de configuração em
`Infrastructure/Data/Configurations`.

### API

| Controller | Endpoints | Autorização |
|---|---|---|
| `AuthController` | `POST /auth/google`, `GET /auth/me` | Público / autenticado |
| `CategoriasController` | Listar, obter, criar, atualizar, desativar | Leitura: todos · Escrita: Administrador e Financeiro · Desativar: Administrador |
| `FormasPagamentoController` | Mesma estrutura | Mesma estrutura |
| `UsuariosController` | Listar, alterar perfil, alterar status | Administrador |
| `StatusController` | Verificação de disponibilidade | Público |

Swagger configurado com autenticação Bearer, o que permite testar os endpoints
autenticados sem o app.

**Exclusão lógica:** o `DELETE` de Categorias e Formas de Pagamento marca
`Ativo = false` em vez de remover o registro, preservando os lançamentos
históricos que apontam para eles.

### Aplicativo MAUI

| Camada | Arquivos |
|---|---|
| Telas | `LoginPage`, `DashboardPage`, `CategoriasPage`, `FormasPagamentoPage` |
| ViewModels | `BaseViewModel`, `LoginViewModel`, `CategoriasViewModel`, `FormasPagamentoViewModel` |
| Serviços | `GoogleAuthService`, `TokenStore`, `AuthHeaderHandler`, `CategoriaService`, `FormaPagamentoService`, `LocalDatabaseService`, `ConnectivityService`, `AppConfig` |
| Modelos | `Categoria`, `FormaPagamento`, `PerfilUsuario`, `UsuarioLogado` |

A camada de serviços está mais avançada que a de telas: já existem base local,
detecção de conectividade e injeção automática do token nas requisições.

**Autorização na interface:** `CategoriasViewModel` e `FormasPagamentoViewModel`
expõem `PodeGerenciar`, que esconde o formulário de criação para perfis sem
permissão de escrita.

---

## Situação por user story

| US | Descrição | Situação |
|---|---|---|
| US01 | Categorias de movimentação | API completa. No app: criar e listar. **Faltam** editar, desativar e o conjunto padrão pré-cadastrado |
| US02 | Formas de pagamento | Mesma situação da US01 |
| US03 | Registro de movimentações | Entidade criada. **Faltam** campos de status e datas (ver `pendencias-do-modelo.md`). Tela não iniciada |
| US04 | Dívidas e parcelamentos | Entidade criada. Regra de geração automática das parcelas e tela não iniciadas |
| US05 | Motor de saldo e previsão | Não iniciado. **Depende** dos campos da US03 |
| US06 | Cadastro de frota | Implementado: `VeiculosController` (placa antiga/Mercosul validada, normalizada e única; capacidade de carga) e `FrotaPage` + `VeiculoFormPage` no app |
| US06-b | Registro de manutenção | Implementado: entidade `Manutencao`, `ManutencoesController` (gera a despesa e põe o veículo "Em manutenção" na mesma transação; `concluir` devolve a "Disponível") e `ManutencaoFormPage` |
| US07 | Rotas | Implementado: `RotasController` e `RotasPage` (criar, editar inline, desativar) |
| US08 | Viagens e hodômetro | Implementado: `ViagensController` (abrir, encerrar com `final > inicial`, cancelar; quilometragem rodada **gravada**; Motorista só vê as próprias), `ViagensPage`, `ViagemFormPage` e `EncerrarViagemPage`. Abrir e encerrar funcionam **offline** com fila de reenvio idempotente |
| US09 | Vínculo financeiro com viagem | Implementado na API (`viagemId` e `quantidadeLitros` em `POST/PUT /api/movimentacoes`) e no formulário do app (picker de viagem; litros aparecem na categoria de combustível) |
| US10 | Estatísticas de rota e veículo | Não iniciado |
| US11 | Painel de indicadores | Implementado em `RelatoriosPage` (resultado do período, entradas, saídas, a receber e a pagar no mês, pendentes em atraso, maior despesa, variação de saídas). O Início mantém saldo e projeção |
| US12 | Gráficos de fluxo de caixa | Implementado: colunas de entradas x saídas por mês e rosca de despesas por categoria, desenhados com `GraphicsView` (sem biblioteca externa). Dados de `GET /api/relatorios/resumo` |
| US13 | Ranking de eficiência | Não iniciado |
| US14 | Exportação de relatório | Implementado em CSV (`GET /api/relatorios/exportar`), compartilhado pelo app via folha de compartilhamento do sistema. PDF não iniciado |
| US15 | Login com Google | Fluxo implementado nas duas pontas. **Falta** o Client ID de produção (ver abaixo) |
| US16 | Perfis de usuário | Quatro perfis definidos, API de gestão pronta. **Faltam** a tela no app e o estado "Aguardando Aprovação" |
| US17 | Navegação contextual | Menu por perfil implementado (`AppShell.ConfigurarMenuPorPerfil`). O Motorista vê Viagens e Frota (consulta) e **abre direto em Viagens** |
| US18 | Segurança de rotas | Implementado na API com `[Authorize(Roles = ...)]`. O Motorista só recebe as próprias viagens em `GET /api/viagens` (filtro no servidor, não só na tela) |

---

## Configuração necessária para executar

Três valores precisam ser preenchidos antes do projeto rodar de ponta a ponta.
Hoje estão como texto de exemplo:

| Onde | Chave | O que é |
|---|---|---|
| `GestaoFrota.API/appsettings.json` | `Authentication:Google:ClientId` | Client ID OAuth2 do Google Cloud Console |
| `GestaoFrota.API/appsettings.json` | `Jwt:Key` | Chave de assinatura do JWT, mínimo 32 caracteres |
| `GestaoFrota.App/Services/AppConfig.cs` | `GoogleClientId` e `GoogleRedirectUri` | Mesmo Client ID e o esquema de redirecionamento registrado nas plataformas |

Enquanto o Client ID não for criado, o login com Google não completa — e como o
aplicativo abre na tela de login, nenhuma outra tela fica acessível.

**Sugestão:** manter os valores reais fora do controle de versão, em
`appsettings.Development.json` ou em variáveis de ambiente, já que o
repositório é público.

---

## Próximos passos sugeridos

Ordem que aproveita melhor o que já existe:

1. **Criar o Client ID no Google Cloud** e preencher as três configurações.
   Destrava a execução do aplicativo inteiro.
2. **Definir os campos pendentes de `Movimentacao`** — ver
   `pendencias-do-modelo.md`. Fazer isso com o banco ainda vazio evita migration
   sobre dados.
3. **Completar as telas de Categorias e Formas de Pagamento** com editar e
   desativar. A API já tem os endpoints.
4. **Criar a tela de gestão de usuários** no app. O `UsuariosController` já está
   pronto.
5. **Adaptar o menu por perfil** conforme a US17.
6. Seguir o roadmap: Movimentações, Dívidas, Frota, Viagens e Relatórios.

---

## Como este documento foi produzido

Leitura do código-fonte da branch `main`, em 31/08/2026. **Nada foi compilado
nem executado** — não houve validação de build, de migration ou de execução do
aplicativo.

Arquivos lidos integralmente: entidades e enums do domínio, `AuthController`,
`UsuariosController`, `CategoriasController`, `Program.cs`, `appsettings.json`,
`AppShell.xaml` e code-behind, `App.xaml` e code-behind, `AppConfig`,
`GoogleAuthService` do app, `CategoriasPage.xaml`, `MovimentacaoConfiguration`.

Arquivos não lidos em detalhe: `LoginPage.xaml`, `FormasPagamentoPage.xaml`,
`LocalDatabaseService`, `AuthHeaderHandler`, `JwtTokenService`,
`ConnectivityService`, os DTOs e as demais configurações do EF Core.

## Atualização (11/10/2026) - núcleo financeiro

Implementados: movimentações (status, vencimento, pagamento, comprovante), dívidas com geração transacional de parcelas, saldo atual e projeção de caixa (API e app), menu Flyout por perfil, plataforma Android do app. Detalhes e roteiro do vídeo no `README.md` da raiz.

## Atualização (20/10/2026) - relatórios

Implementados: `RelatoriosController` (`GET /api/relatorios/resumo?meses=N` e
`GET /api/relatorios/exportar?meses=N`), a regra de agregação pura em
`GestaoFrota.Domain/Services/CalculadoraRelatorios.cs` e a tela `RelatoriosPage`
no app (seletor de 3/6/12 meses, indicadores, gráfico de colunas, rosca por
categoria, exportação CSV). O item "Relatórios" entra no menu só para
Administrador, Gestor de Frota e Financeiro.

Período = os últimos N meses incluindo o atual, agrupado pela **data de
vencimento** (competência), igual ao filtro da lista de movimentações.
Pagos e pendentes entram; estornadas nunca.

Fora desta etapa: ranking de rotas e eficiência por veículo (US10 e US13)
seguem bloqueados pela ausência de endpoints de frota; a tela mostra o bloco
como "aguardando módulo de frota" em vez de dado falso.

Escrito sem SDK .NET disponível na máquina de autoria: **não compilado**.
Validar com `dotnet build` nos projetos API e App antes do merge.

## Atualização (21/10/2026) - módulo de frota

Implementados (Sprints 5 e 6 do roadmap): migration `20261021120000_ModuloFrota`
(capacidade de carga no veículo, `QuilometragemRodadaKm` na viagem,
`QuantidadeLitros` na movimentação e a tabela `Manutencoes`),
`VeiculosController`, `RotasController`, `ViagensController` e
`ManutencoesController`, regras puras em `Domain/Services/ValidadorPlaca.cs` e
`RegrasViagem.cs`, e no app as telas Frota, Veículo, Rotas, Viagens, Abrir
viagem, Encerrar viagem e Registrar manutenção, com os itens de menu por perfil.
Detalhes, decisões e roteiro de teste em `docs/projeto/PR-frota.md`.

Resolvidos de `pendencias-do-modelo.md`: itens 2 (litros), 4 (Manutenção),
5 (capacidade de carga) e 6 (quilometragem persistida).

Continuam fora: US10 e US13 (estatísticas e ranking, Sprint 7), item 7
(situação do usuário com três estados, frente de autenticação), item 8
(decisão Frete × Movimentação, do time), fila offline para o financeiro, Client
ID real do Google e o script PostgreSQL em `GestaoFrota.Database/`.

Escrito sem SDK .NET na máquina de autoria: **não compilado**. Validar com
`dotnet build` nos projetos API e App e `dotnet ef database update` antes do merge.
