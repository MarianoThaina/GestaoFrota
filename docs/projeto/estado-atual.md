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
| US06 | Cadastro de frota | Entidade criada. Tela não iniciada |
| US06-b | Registro de manutenção | Entidade específica ainda não existe |
| US07 | Rotas | Entidade criada. Tela não iniciada |
| US08 | Viagens e hodômetro | Entidade criada, com a distância calculada. Tela não iniciada |
| US09 | Vínculo financeiro com viagem | `Movimentacao.ViagemId` existe. Campo de litros pendente |
| US10 | Estatísticas de rota e veículo | Não iniciado |
| US11 | Painel de indicadores | `DashboardPage` existe como tela de boas-vindas. Indicadores previstos para a Sprint 7 |
| US12 | Gráficos de fluxo de caixa | Não iniciado |
| US13 | Ranking de eficiência | Não iniciado |
| US14 | Exportação de relatório | Não iniciado |
| US15 | Login com Google | Fluxo implementado nas duas pontas. **Falta** o Client ID de produção (ver abaixo) |
| US16 | Perfis de usuário | Quatro perfis definidos, API de gestão pronta. **Faltam** a tela no app e o estado "Aguardando Aprovação" |
| US17 | Navegação contextual | Autorização aplicada dentro das páginas. A adaptação do menu por perfil ainda não foi implementada |
| US18 | Segurança de rotas | Implementado na API com `[Authorize(Roles = ...)]` |

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
