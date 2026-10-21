# Gestão de Frota

Aplicativo mobile (.NET MAUI) + API REST (ASP.NET Core) + SQL Server (EF Core) para gestão financeira e controle de frota.

Esta etapa entrega o **núcleo financeiro**: movimentações, dívidas/financiamentos com geração automática de parcelas, saldo atual e projeção de caixa, com autenticação Google (SSO) e RBAC.

```
GestaoFrota.Domain          entidades, enums e a CalculadoraFinanceira (regras puras)
GestaoFrota.Infrastructure  AppDbContext, configurações EF e migrations
GestaoFrota.API             controllers REST, JWT, RBAC, Swagger
GestaoFrota.App             app .NET MAUI (Android) - MVVM, SQLite offline, menu por perfil
GestaoFrota.Web             páginas web de cadastro (Categorias, Formas, Usuários)
docs/                       protótipo HTML e documentação do projeto
```

## O que foi implementado nesta etapa

| Requisito | Onde |
|---|---|
| Entradas/Saídas com valor, descrição, categoria, forma de pagamento e vencimento | `MovimentacoesController`, `MovimentacaoFormPage` |
| Status Pendente x Pago/Recebido; Pago exige data de pagamento | Validação na API + check constraint `CK_Movimentacao_PagoExigeDataPagamento` |
| Comprovante por câmera ou galeria | `MediaPicker` no app; `POST /api/movimentacoes/{id}/comprovante` (JPEG/PNG/WEBP/HEIC até 5 MB, tipo detectado pelo conteúdo) |
| Validações (sem valores negativos/inconsistências) | Client (`ParserMoeda`, formulário) e servidor (`[Range]`, `IValidatableObject`, check `[Valor] > 0`) |
| Dívidas: parcela, quantidade, 1º vencimento, quitação antecipada | `DividasController`, `DividaFormPage` |
| N parcelas pendentes geradas em transação com rollback | `DividasController.Criar` (`BeginTransactionAsync` + `RollbackAsync`) |
| Saldo atual (somente Pago/Recebido) | `GET /api/financeiro/saldo` |
| Projeção por mês de vencimento (saldo + pendentes) | `GET /api/financeiro/projecao` |
| Menu por perfil (Motorista sem módulos financeiros) | `AppShell.ConfigurarMenuPorPerfil` + `[Authorize(Roles=...)]` na API |

Movimentações **estornadas** nunca entram no saldo nem na projeção.

## Pré-requisitos

- .NET SDK 10 com a workload MAUI/Android (`dotnet workload install maui-android`)
- SQL Server (LocalDB, Express ou Docker)
- Android SDK + um emulador (AVD) com **câmera virtual** habilitada
- Projeto OAuth no Google Cloud (para o login) - ver a seção abaixo

## Configuração (sem versionar segredos)

`appsettings.json` guarda apenas *placeholders*. Informe os valores reais por **user-secrets** ou variáveis de ambiente:

```bash
cd GestaoFrota.API
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "uma-chave-aleatoria-com-no-minimo-32-caracteres"
dotnet user-secrets set "Authentication:Google:ClientId" "SEU_ID.apps.googleusercontent.com"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=GestaoFrota;Trusted_Connection=True;TrustServerCertificate=True;"
```

Equivalentes por variável de ambiente: `Jwt__Key`, `Authentication__Google__ClientId`, `ConnectionStrings__DefaultConnection`.
**Nunca** faça commit de chaves, senhas ou connection strings reais (`appsettings.Development.json` já está no `.gitignore`).

No app, edite `GestaoFrota.App/Services/AppConfig.cs`:

- `GoogleClientId` com o mesmo Client ID usado na API;
- `ApiBaseUrl` já aponta para `http://10.0.2.2:5137/api/` (o emulador enxerga o `localhost` do PC por `10.0.2.2`).

### Login Google (SSO)

1. Google Cloud Console > APIs e serviços > Credenciais > criar ID do cliente OAuth.
2. Registre o esquema de redirecionamento `com.suaempresa.gestaofrota://oauth2redirect` (igual a `AppConfig.GoogleRedirectUri` e ao `WebAuthenticationCallbackActivity`).
3. Use o mesmo Client ID no app e na API (a API valida a audiência do `id_token`).

Não há confirmação de e-mail própria: quem autentica é o Google, que já entrega o e-mail verificado dentro do `id_token`. Contas novas entram como **Motorista** (`OperadorMotorista`) e só um Administrador promove perfis.

## Banco de dados

```bash
dotnet ef database update -p GestaoFrota.Infrastructure -s GestaoFrota.API
```

Isso cria o schema e semeia as categorias padrão (Frete, Combustível, Pedágio, Manutenção, Salários, Financiamento) e as formas de pagamento (Pix, Boleto, Transferência TED, Cartão Corporativo).

As migrations `20261011120000_FinanceiroCore` e `20261021120000_ModuloFrota` foram escritas à mão a partir do snapshot. Se preferir gerar a sua, apague-a (e reverta as mudanças do `AppDbContextModelSnapshot` se já a aplicou) e rode:

```bash
dotnet ef migrations add ModuloFrota -p GestaoFrota.Infrastructure -s GestaoFrota.API
```

Em ambiente hospedado, sem terminal, defina `Database__MigrateOnStartup=true` para aplicar as migrations ao iniciar a API.

## Executando

```bash
# 1) API
cd GestaoFrota.API
dotnet run --launch-profile http        # http://localhost:5137  (Swagger em /swagger)

# 2) App no emulador Android (com o emulador já aberto)
cd GestaoFrota.App
dotnet build -t:Run -f net10.0-android
```

### Criar os perfis para a demonstração

1. Entre no app com a conta que será **Administrador** (cria o usuário automaticamente como Motorista).
2. Promova-a direto no banco, uma única vez:

   ```sql
   UPDATE Usuarios SET Perfil = 'Administrador' WHERE Email = 'seu-email@gmail.com';
   ```
   (A coluna `Perfil` guarda o nome do enum como texto: `Administrador`, `GestorDeFrota`, `Financeiro` ou `OperadorMotorista`.)
3. Entre com uma segunda conta Google: ela fica como **Motorista**. Use-a para mostrar o menu restrito.
4. Daí em diante, o Administrador altera perfis em *Usuários* dentro do app.

## Roteiro do vídeo (emulador Android)

1. **Login e RBAC (Administrador):** "Entrar com Google" > mostrar o menu lateral completo (Início, Movimentações, Dívidas e Financiamentos, Categorias, Formas de Pagamento, Usuários, Sair).
2. **Cadastros base:** abrir *Categorias* e *Formas de Pagamento*.
3. **Navegação dinâmica:** *Sair* > entrar com a conta de **Motorista** > o menu só mostra *Início* e *Sair*, sem módulos financeiros.
4. **Fluxo financeiro:** sair e voltar como Administrador > *Movimentações* > *Nova movimentação*:
   - uma **Receita** já recebida (com data de recebimento);
   - uma **Despesa** pendente, anexando um **comprovante** (câmera ou galeria);
   - abrir o comprovante na lista.
5. **Financiamento:** *Dívidas e Financiamentos* > *Novo financiamento* (ex.: 12 x R$ 3.850,00, 1º vencimento no mês seguinte, quitação antecipada informada) > *Salvar e gerar parcelas*.
6. **Verificação:** a tela da dívida lista as 12 parcelas pendentes (uma por mês). Em *Movimentações* elas aparecem no histórico. No *Início*, mostrar o **Saldo Atual** (só pagos/recebidos) e a **Projeção de caixa** mês a mês. Marque uma parcela como paga e puxe para atualizar para ver o saldo mudar.

## Endpoints principais

Enums trafegam como número (Tipo: 1 Receita, 2 Despesa; Status: 1 Pendente, 2 Pago). Exemplos prontos em `GestaoFrota.API/GestaoFrota.API.http`.

| Método | Rota | Perfis |
|---|---|---|
| GET | `/api/movimentacoes` | Administrador, Gestor, Financeiro |
| POST/PUT | `/api/movimentacoes` | Administrador, Financeiro |
| PATCH | `/api/movimentacoes/{id}/pagar` e `/estornar` | Administrador, Financeiro |
| POST/GET/DELETE | `/api/movimentacoes/{id}/comprovante` | escrita: Adm/Fin; leitura: Adm/Gestor/Fin |
| GET/POST | `/api/dividas`, `/api/dividas/{id}` | leitura: Adm/Gestor/Fin; POST: Adm/Fin |
| GET | `/api/financeiro/saldo`, `/api/financeiro/projecao?meses=12` | Administrador, Gestor, Financeiro |
| GET | `/api/relatorios/resumo?meses=6` (fluxo mensal, despesas por categoria, indicadores) | Administrador, Gestor, Financeiro |
| GET | `/api/relatorios/exportar?meses=6` (CSV do período) | Administrador, Gestor, Financeiro |

O Motorista recebe **403** em todos esses endpoints.

### Frota (US06, US06-b, US07, US08, US09)

Enums: StatusVeiculo 1 Disponivel, 2 EmViagem, 3 EmManutencao · StatusViagem 1 EmAndamento, 2 Concluida, 3 Cancelada.

| Método | Rota | Perfis |
|---|---|---|
| GET | `/api/veiculos`, `/api/veiculos/{id}` (filtros `status`, `ativo`) | todos |
| POST/PUT | `/api/veiculos` (placa antiga ou Mercosul, normalizada e única; capacidade de carga em t) | Administrador, Gestor |
| DELETE | `/api/veiculos/{id}` (desativação lógica; recusa veículo em viagem) | Administrador |
| GET | `/api/rotas`, `/api/rotas/{id}` (filtro `ativa`) | todos |
| POST/PUT | `/api/rotas` | Administrador, Gestor |
| DELETE | `/api/rotas/{id}` (desativação lógica) | Administrador |
| GET | `/api/viagens` (filtros `status`, `veiculoId`, `motoristaId`, `rotaId`, `de`, `ate`) | todos; o **Motorista só recebe as próprias** |
| GET | `/api/viagens/motoristas` (usuários que podem dirigir) | Administrador, Gestor |
| POST | `/api/viagens` (abrir; `id` opcional gerado no aparelho, reenvio idempotente) | Administrador, Gestor, Motorista (só para si) |
| PATCH | `/api/viagens/{id}/encerrar` (hodômetro final > inicial, senão 400; grava km rodada) | Administrador, Gestor, o motorista da viagem |
| PATCH | `/api/viagens/{id}/cancelar` | Administrador, Gestor |
| GET | `/api/manutencoes`, `/api/manutencoes/{id}` (filtros `veiculoId`, `abertas`) | Administrador, Gestor, Financeiro |
| POST | `/api/manutencoes` (gera a despesa e põe o veículo em manutenção, em transação) | Administrador, Gestor |
| PATCH | `/api/manutencoes/{id}/concluir` (veículo volta a Disponível) | Administrador, Gestor |

`POST /api/movimentacoes` aceita agora `viagemId` e `quantidadeLitros` (US09); `GET /api/movimentacoes?viagemId=` filtra por viagem.

| GET | `/api/relatorios/frota?meses=6` (US10/US13: rentabilidade, custo/km e km/l por rota e por veículo; top 3 rotas lucrativas e top 3 veículos eficientes; veículos em viagem) | Administrador, Gestor, Financeiro |

## Decisões de projeto

- **Offline-first mantido:** categorias e formas continuam em cache SQLite; o histórico de movimentações também fica em cache para consulta offline. Lançar, pagar, estornar, cadastrar dívida e consultar saldo exigem conexão (o app avisa em vez de fingir que salvou).
- **Navegação Flyout (menu lateral):** comporta os 6 módulos do Administrador sem estourar a barra inferior e permite ocultar por perfil tudo o que o Motorista não pode usar.
- **Comprovante no banco** (`varbinary(max)`): evita perder arquivos em hospedagens gratuitas com disco efêmero.
- **Regras de cálculo isoladas** em `GestaoFrota.Domain/Services/CalculadoraFinanceira.cs` e `CalculadoraRelatorios.cs`, sem dependência de banco.
- **Gráficos sem biblioteca externa:** colunas e rosca da tela de Relatórios são desenhadas com o `GraphicsView` do próprio MAUI (`GestaoFrota.App/Controls/`), o que mantém o app sem dependência nova e com o desenho legível linha a linha.
- **Relatórios agrupados por vencimento:** o período (3, 6 ou 12 meses, incluindo o atual) usa a data de vencimento como competência, igual ao filtro da lista de movimentações. Pagos e pendentes entram; estornadas nunca. A exportação (US14) sai em CSV com `;` e BOM UTF-8, que o Excel em português abre sem configuração.
- **Viagem é a única escrita offline-first (US08):** o `Id` da viagem nasce no aparelho (Guid), então abrir e encerrar funcionam sem rede: o registro fica no SQLite com `AberturaPendente`/`EncerramentoPendente` e o `ViagemService` reenvia ao abrir a tela de Viagens com conexão. A API trata o reenvio como idempotente (mesmo `Id` devolve a viagem existente; mesmo hodômetro final devolve 200). Veículos e rotas ficam em cache para o motorista escolher sem internet. Manutenção e lançamentos continuam exigindo conexão porque mexem no caixa.
- **Quilometragem rodada gravada, não calculada:** no encerramento a API valida `final > inicial` (também protegido por check constraint), grava `QuilometragemRodadaKm` e atualiza o `KmAtual` do veículo. Assim os relatórios da US10 somam direto no banco.
- **Manutenção gera a despesa no mesmo commit (US06-b):** manutenção, movimentação de saída na categoria "Manutenção" e status do veículo entram numa transação; se algo falhar, nada é salvo. A categoria padrão é a semeada pela `InitialCreate`; o cliente pode informar outra.
- **Estatísticas contam pelo vínculo (US09 → US10):** rentabilidade de uma viagem = entradas vinculadas − saídas vinculadas; custo/km = saídas vinculadas ÷ km rodados; km/l = km rodados ÷ litros dos abastecimentos vinculados. Viagens canceladas ficam de fora; em andamento entram só no financeiro (sem km ainda). O ranking (US13) ordena rotas por rentabilidade total e veículos por menor custo/km, e omite veículo sem km no período em vez de mostrar custo zero. `Frete.ValorFrete` **não** entra na conta, para não somar a receita duas vezes; o time ainda precisa decidir o papel desse campo.
- **Placa normalizada:** guardada em maiúscula e sem hífen (`ValidadorPlaca` no domínio), para o índice único não aceitar "ABC-1234" e "ABC1234" como placas diferentes. O app mostra "ABC-1234" para a antiga e "ABC1D23" para a Mercosul.

## Repositório

- Torne o repositório **público** e proteja a branch `main` (GitHub > Settings > Branches > *Add branch protection rule*: exigir pull request e bloquear push direto).
- Revise o histórico antes de publicar: se algum segredo já foi commitado, **troque a chave** (não basta apagar o arquivo).

## Limitações conhecidas

- O código desta etapa foi escrito sem acesso a um SDK .NET/emulador; faça `dotnet build` e um teste completo no emulador antes de gravar o vídeo.
- Imagens maiores que 5 MB são recusadas (sem compressão automática).
- A quitação antecipada é armazenada e exibida, mas ainda não há operação que quite a dívida em um único pagamento.
- A fila offline cobre só viagens (abrir e encerrar). Categorias criadas offline continuam marcadas como pendentes sem rotina de reenvio.
- `GestaoFrota.Database/create_tables.sql` está em PostgreSQL enquanto a API usa SQL Server; o schema real é o das migrations.
