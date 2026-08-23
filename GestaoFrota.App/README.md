# GestaoFrota.App — App Mobile (.NET MAUI)

Este projeto contém o **código de negócio** do app mobile (Models, Services,
ViewModels, Views, Shell, DI). Ele foi escrito fora de um `dotnet new maui`
porque este ambiente não tem o SDK do .NET nem acesso ao NuGet — então não
dava para gerar/validar o projeto aqui.

## Passo a passo para colocar para rodar

1. **Gere o projeto MAUI oficial** (isso cria automaticamente `Platforms/`
   com o boilerplate de Android/iOS/MacCatalyst/Windows, ícones, splash
   screen, `Resources/`, etc.):

   ```bash
   dotnet new maui -n GestaoFrota.App
   ```

2. **Substitua/mescle** por cima do projeto gerado:
   - `GestaoFrota.App.csproj` (o daqui já tem os pacotes NuGet necessários)
   - As pastas `Models/`, `Services/`, `ViewModels/`, `Views/`
   - `App.xaml`, `App.xaml.cs`, `AppShell.xaml`, `AppShell.xaml.cs`,
     `MauiProgram.cs`, `Converters.cs`

3. **Adicione o projeto à solução:**

   ```bash
   dotnet sln GestaoFrota.slnx add GestaoFrota.App/GestaoFrota.App.csproj
   ```
   (o `GestaoFrota.slnx` já foi atualizado aqui com a referência.)

4. **Configure o Google Sign-In** em `Services/AppConfig.cs`:
   - Crie um OAuth Client ID no [Google Cloud Console](https://console.cloud.google.com/apis/credentials)
     (tipo "Android"/"iOS" ou "Web", conforme a plataforma).
   - Preencha `GoogleClientId`.
   - Registre o esquema de redirecionamento (`com.suaempresa.gestaofrota://oauth2redirect`)
     como *intent filter* no `AndroidManifest.xml` (Android) e como *URL Scheme*
     no `Info.plist` (iOS/MacCatalyst) — o `dotnet new maui` já cria esses
     arquivos, você só precisa adicionar o intent filter/URL scheme.
   - No backend (`GestaoFrota.API/appsettings.json`), configure o mesmo
     `Authentication:Google:ClientId`.

5. **Ajuste a URL da API** em `Services/AppConfig.cs` (`ApiBaseUrl`) conforme
   seu ambiente (emulador Android usa `10.0.2.2`, dispositivo físico precisa
   do IP da máquina na rede local, etc.).

6. **Rode:**

   ```bash
   dotnet build -t:Run -f net10.0-android
   ```

## O que já está implementado (Sprints 1 e 2)

- **Arquitetura**: MVVM com `CommunityToolkit.Mvvm`, separação em
  Models/Services/ViewModels/Views, injeção de dependência via `MauiProgram`.
- **Login Google (OAuth2)**: `GoogleAuthService` usa `WebAuthenticator` para
  autenticar no Google e troca o `id_token` pelo JWT da API
  (`POST /api/auth/google`). Sessão salva com `SecureStorage`.
- **RBAC**: o perfil do usuário logado (`UsuarioLogado.Perfil`) controla a
  exibição de ações de escrita nas telas (ex.: `PodeGerenciar` em
  `CategoriasViewModel`/`FormasPagamentoViewModel`). A validação real e
  definitiva continua sendo feita no backend via `[Authorize(Roles = ...)]`.
- **Offline-First**: `LocalDatabaseService` usa SQLite local como cache;
  `CategoriaService`/`FormaPagamentoService` sempre leem do cache local e
  sincronizam com a API em segundo plano quando há conexão. Registros criados
  offline ficam marcados como `PendenteSincronizacao` até serem confirmados.
- **Protótipo de UI/UX**: telas de Login, Categorias e Formas de Pagamento
  funcionais (XAML), com Shell de navegação por abas.

## O que falta (próximas sprints, conforme roadmap do PDF)

- Sprint 5–6: cadastro de frota, rotas, viagens, hodômetros.
- Sprint 6: vínculo entre movimentações financeiras e viagens.
- Sprint 7: dashboards, gráficos, exportação de relatórios.
- Sprint 8: testes de usabilidade, revisão de UI, lançamento.
- Rotina de sincronização em segundo plano para reenviar registros
  `PendenteSincronizacao` automaticamente quando a conectividade voltar
  (hoje a sincronização de leitura acontece a cada `ListarAsync`, mas o
  reenvio automático dos pendentes ainda precisa ser implementado).
