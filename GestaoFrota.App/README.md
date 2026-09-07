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

## Revisão do fluxo de login + sessão (item 5 do checklist / seção 8 da Documentação Técnica)

Esta revisão fechou as lacunas de código que impediam o fluxo descrito na
seção 8 de funcionar de ponta a ponta:

1. **Bug corrigido em `LoginViewModel`**: após o login, o código trocava
   `Application.Current.MainPage`, que **não tem efeito** no modelo
   multi-window do MAUI usado em `App.xaml.cs` (que cria e controla a
   `Window` diretamente). A troca de tela agora é feita em
   `Application.Current.Windows[0].Page`, que é a `Window` real exibida.
   Sem esse ajuste, o botão "Entrar com Google" completava a autenticação
   (token salvo), mas a tela continuava presa no Login — exatamente o tipo
   de falha que só aparece ao rodar no emulador.
2. **Item 7 da seção 8 implementado** (`GET /api/auth/me`): antes, o app só
   checava se havia *algum* token salvo (`EstaAutenticadoAsync`) para pular
   a tela de login. Agora `IAuthService.ValidarSessaoAsync()` chama
   `GET /api/auth/me` de fato — confirma no backend que o token não expirou
   e que o usuário continua ativo — e só então libera o `AppShell`. Se a API
   responder 401, a sessão local é limpa e o usuário volta ao login. Se a
   API estiver inacessível (offline), a sessão local é mantida, seguindo o
   padrão offline-first já usado no restante do app.
3. **Logout de ponta a ponta**: `DashboardPage` agora mostra o usuário
   logado (nome, e-mail, perfil) via `DashboardViewModel` e tem um botão
   "Sair" que chama `LogoutAsync()` e volta para o `LoginPage`. Isso fecha o
   card "Testar logout" do Trello e dá uma evidência visual fácil de
   gravar (login → dashboard com dados do usuário → sair → volta ao login).

### O que ainda depende do scaffold oficial (`dotnet new maui`)

Como não há SDK do .NET neste ambiente, os itens abaixo só podem ser feitos
depois do passo 1 do "Passo a passo" acima, direto nos arquivos gerados:

- **Callback do OAuth no Android** — crie
  `Platforms/Android/WebAuthenticationCallbackActivity.cs`:

  ```csharp
  using Android.App;
  using Android.Content.PM;
  using Microsoft.Maui.Authentication;

  namespace GestaoFrota.App.Platforms.Android;

  [Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
  [IntentFilter(new[] { global::Android.Content.Intent.ActionView },
      Categories = new[] { global::Android.Content.Intent.CategoryDefault, global::Android.Content.Intent.CategoryBrowsable },
      DataScheme = "com.suaempresa.gestaofrota")]
  public class WebAuthenticationCallbackActivity : WebAuthenticatorCallbackActivity
  {
  }
  ```

- **Callback do OAuth no iOS/MacCatalyst** — em `Platforms/iOS/AppDelegate.cs`
  (e no `MacCatalyst/AppDelegate.cs`):

  ```csharp
  public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
      => Microsoft.Maui.Authentication.WebAuthenticator.CallbackActivity is null
          ? base.OpenUrl(app, url, options)
          : Platform.OpenUrl(app, url, options);
  ```

  E em `Platforms/iOS/Info.plist` (e MacCatalyst):

  ```xml
  <key>CFBundleURLTypes</key>
  <array>
    <dict>
      <key>CFBundleURLSchemes</key>
      <array>
        <string>com.suaempresa.gestaofrota</string>
      </array>
    </dict>
  </array>
  ```

- **Credenciais reais**: substituir `SEU_GOOGLE_CLIENT_ID.apps.googleusercontent.com`
  em `Services/AppConfig.cs` e `Authentication:Google:ClientId` em
  `GestaoFrota.API/appsettings.json` pelo Client ID real criado no Google
  Cloud Console, e `Jwt:Key` em `appsettings.json` por uma chave forte
  (mínimo 32 caracteres) — os valores atuais são placeholders de
  desenvolvimento e o login real não funciona sem eles.

### Para fechar o item 5 do checklist definitivamente

Depois dos passos acima, rode `dotnet build -t:Run -f net10.0-android` com
o emulador aberto e grave: tela de login → toque em "Entrar com Google" →
tela de consentimento do Google → volta ao app já no Dashboard mostrando
nome/e-mail/perfil → feche e reabra o app (sessão deve persistir sem pedir
login de novo) → toque em "Sair" (deve voltar ao Login). Esse vídeo/prints é
a evidência que os dois documentos citam como pendente.

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
