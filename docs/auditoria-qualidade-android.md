# GestaoFrota — Auditoria pelos critérios de qualidade principal do Android

**Base:** https://developer.android.com/docs/quality-guidelines/core-app-quality (69 critérios, 5 áreas)
**Data:** 26/11/2026 · **App:** `com.gestaofrota.app` 1.0 · minSdk 23 · targetSdk 36 · .NET MAUI 10
**Como foi feito:** leitura do código e do manifesto final gerado no build Release + testes no emulador (Android 16, API 36) com o .apk Release, nos temas claro e escuro.

Legenda: **Atende** · **Corrigido** nesta auditoria · **Parcial** (risco conhecido) · **N/A** (não se aplica ao app)

## Resumo

| Área | Critérios | Atende | Corrigido | Parcial | N/A |
|---|---|---|---|---|---|
| Experiência do usuário | 18 | 9 | 4 | 2 | 3 |
| Funcionalidade | 11 | 2 | 0 | 0 | 9 |
| Desempenho e estabilidade | 11 | 5 | 2 | 3 | 1 |
| Privacidade e segurança | 23 | 9 | 5 | 2 | 7 |
| Google Play | 6 | 0 | 0 | 0 | 6 |
| **Total** | **69** | **25** | **11** | **7** | **26** |

Nenhum critério aplicável ficou reprovado. Os parciais estão explicados abaixo, com o motivo de não terem sido mexidos a 4 dias da entrega.

**Correções desta auditoria:** commit `2912a2e` (branch `fix/qualidade-android`), 32 arquivos, 78/78 testes, conferido no emulador.

---

## 1. Experiência do usuário

| ID | Critério | Situação | Evidência |
|---|---|---|---|
| Consistent_UX | Experiência consistente | Atende | Roteiro B–F completo no emulador; modo avião testado (viagem offline sincroniza). |
| App_Switcher | Volta pelo Recentes | Atende | Formulário preenchido → Home → Recentes: dados intactos. |
| Sleep_Resume | Suspende e retoma | Atende | Tela desligada e ligada: app volta na mesma tela. |
| Lock_Resume | Bloqueia e retoma | Atende | Mesmo teste com tela de bloqueio. |
| Display_State_Parity | Mesmas funções em retrato e paisagem | Atende | Rotação no formulário: mesmos campos. |
| Fullscreen_Display | Preenche a janela | Atende | Paisagem ocupa a largura toda, sem tarjas. |
| Orientation_Transitions | Rotação sem perder estado | Atende | Valor e descrição digitados mantidos após girar e voltar. |
| Graphic_Quality | Visual sem distorção | Atende | Ícones e gráficos desenhados em vetor (GraphicsView). Desde 26/11 o app não usa emoji: o caminhão do Início também é vetor. |
| Line_Length | 45–75 caracteres por linha | Parcial | Em paisagem/tablet os formulários ocupam a largura toda (linhas longas). Fora do escopo: o app é usado em retrato no celular. |
| Theme_Support | Legível no claro e no escuro | Corrigido | Antes: modo escuro deixava campos invisíveis. Agora tema claro forçado (PR #29) — conferido em 11 telas com o sistema no escuro. |
| Back_Button_Nav | Voltar padrão | Corrigido | Antes: Voltar em qualquer módulo fechava o app. Agora fecha o menu, volta ao Início e só então sai. |
| Back_Gesture_Nav | Gesto de voltar | Atende | Mesmo tratamento (o gesto chega como Voltar). |
| State_Preservation | Preserva estado | Atende | Ver App_Switcher; sessão volta após fechar o app (E1). |
| Notification_Quality | Notificações | N/A | App não envia notificações. |
| Conversation_Quality | Apps de mensagem | N/A | Não é app de mensagem. |
| Touch_Target_Size | Alvos de 48 dp | Corrigido | Botões de ação das listas tinham 34–36 dp; agora 48 dp (Viagens, Movimentações, Frota, Relatórios, Dívida). |
| Visual_Contrast | Contraste 4,5:1 | Corrigido | "Gray" 3,95:1 → #5F6368 6,05:1; verde 4,21 → 5,95; laranja 3,09 → 5,16; dourado 3,25 → 5,33; vermelho 3,96 → 5,80; cinza do gráfico 2,44 → ok. |
| Content_Description | Descrição de acessibilidade | Parcial | Adicionada nas imagens do comprovante e nos 2 gráficos. Botões têm texto (lido pelo leitor de tela). Não passei o Accessibility Scanner. |

## 2. Funcionalidade

| ID | Critério | Situação | Evidência |
|---|---|---|---|
| Audio_* (6) | Áudio | N/A | App não toca áudio. |
| Video_* (3) | Vídeo | N/A | App não tem vídeo. |
| System_Sharesheet | Compartilhar pelo sistema | Atende | PDF e CSV abrem o compartilhamento do Android ("Sharing 1 file", print 25). |
| Background_Service_Optimization | Sem serviço longo em segundo plano | Atende | Manifesto final: só receptores do MAUI e `KeepAliveService` do WebAuthenticator (ativo só durante o login). A sincronização offline roda com o app aberto. |

## 3. Desempenho e estabilidade

| ID | Critério | Situação | Evidência |
|---|---|---|---|
| App_Startup_Time | Abre em até 2 s ou mostra progresso | Corrigido | Abertura a frio 2,6–4,2 s no emulador; agora mostra "Verificando sessão…" com indicador (PR #27). |
| Rendering_Performance | 60 fps | Parcial | Emulador com GPU por software perde quadros na abertura. Não medido em aparelho real. |
| StrictMode_Compliance | Sem avisos do StrictMode | Parcial | Não testado (StrictMode não se liga num app MAUI sem código nativo extra). |
| Stability_ANR | Sem crash nem ANR | Atende | Nenhum crash no uso. Os 2 ANR vistos foram toques durante abertura a frio no emulador lento. *Atenção:* um .apk gerado sem limpar `obj/` travou na abertura (`UnsatisfiedLinkError`); gerado do zero, abre normalmente. Gerar sempre com build limpo. |
| Android_Platform_Compatibility | Roda no Android mais recente | Atende | Testado no Android 16 (API 36). |
| Target_SDK_Version | targetSdk atual | Atende | 36. |
| Compile_SDK_Version | compileSdk atual | Atende | 36 (net10.0-android). |
| SDK_Maintenance | SDKs atualizados | Parcial / Corrigido | **Corrigido na API:** System.Security.Cryptography.Xml e .Pkcs (vulnerabilidades altas, transitivas do SqlClient) → 10.0.15. **Pendente no app:** SQLitePCLRaw 2.1.10 tem alerta alto (GHSA-2m69) e a 2.1.11 ainda acusa o mesmo; aguarda versão corrigida. MAUI 10 → 11 e demais majors não atualizados de propósito (risco a 4 dias da entrega). |
| Non_SDK_Interfaces | Sem APIs ocultas | Atende | Só APIs do MAUI/AndroidX; sem reflexão em classes do sistema. |
| Production_Build_Quality | Sem libs de debug no Release | Corrigido | `Microsoft.Extensions.Logging.Debug` agora só entra no build de Debug. |
| Power_Management | Doze / App Standby | N/A | Sem alarmes, jobs ou sincronização em segundo plano. |

## 4. Privacidade e segurança

| ID | Critério | Situação | Evidência |
|---|---|---|---|
| Minimize_Permissions | Mínimo de permissões | Corrigido | Removida READ_MEDIA_IMAGES (o seletor de fotos do Android 13+ dispensa); WRITE_EXTERNAL_STORAGE só até API 28. Ficam INTERNET, ACCESS_NETWORK_STATE, CAMERA e leitura de armazenamento até API 32. |
| Sensitive_Permissions | Sensíveis só no essencial | Atende | Só câmera, para o comprovante. Sem localização, contatos, SMS etc. |
| Runtime_Permissions | Pede na hora do uso | Atende | Câmera pedida só ao tocar em "Câmera" (print 14). |
| Permission_Rationale | Explica o porquê | Corrigido | Agora mostra a explicação antes de pedir de novo, quando o Android indica. |
| Graceful_Degradation | Funciona se negar | Corrigido | Negada: mensagem sugerindo a Galeria. Negada de vez: oferece "Abrir configurações" (print 70) e abre as Configurações do app. |
| Sensitive_Data_Storage | Dados sensíveis no interno | Atende | Token no SecureStorage (Android Keystore); cache em SQLite no armazenamento interno do app. |
| Sensitive_Data_Logging | Nada pessoal em log | Atende | Log do sistema varrido durante o uso: nenhum token, e-mail ou id_token. Só `Debug.WriteLine`, que não aparece no Release. |
| Hardware_IDs | Sem IMEI etc. | Atende | Nenhum uso de IDs de hardware. |
| App_Data_Backup | Backup na nuvem | Corrigido | Decisão: `allowBackup=false`. Os dados vivem no servidor; o SecureStorage não restaura em outro aparelho, e restaurar o cache sem a sessão causaria inconsistência. Num aparelho novo, o login pelo Google recupera tudo. |
| App_Logins_Restoration | Restaurar login | N/A | Login é Google OAuth sem senha; num aparelho novo é 1 toque. |
| Autofill_Hints | Dicas de preenchimento | N/A | Não há campo de senha ou credencial no app. |
| Credential_Manager | Credential Manager | Parcial | Login usa o navegador (WebAuthenticator) com OAuth do Google, não o Credential Manager. Trocar exige reescrever o login — fora do prazo. |
| Biometric_Authentication | Biometria em operações financeiras | N/A | Não exigido pelo escopo do trabalho; anotado como melhoria futura. |
| Component_Export | `exported` explícito | Atende | Todos os componentes do manifesto final com `android:exported`. Exportados só: MainActivity (launcher), WebAuthenticationCallbackActivity (retorno do login) e ProfileInstallReceiver (protegido por `DUMP`). |
| Component_Permissions | Intents seguros | Atende | Callback do login só aceita o esquema `com.gestaofrota.app`; o código recebido é validado pela API. |
| Component_Protection | Providers protegidos | Atende | FileProvider e demais providers com `exported=false`. |
| Network_Security_Traffic | Só HTTPS | Corrigido | Agora bloqueado por configuração em todas as versões (antes, Android 6–8 ainda aceitavam HTTP). |
| Network_Security_Configuration | Config declarada | Corrigido | `Resources/xml/network_security_config.xml` no manifesto. |
| Security_Provider_Initialization | Provider do Play Services | N/A | App não usa Google Play services; TLS pelo próprio .NET. |
| WebView_Asset_Loader | WebViews seguras | N/A | App não tem WebView (o login abre o navegador do sistema). |
| WebView_JavaScript | JS em WebView | N/A | Idem. |
| App_Bundles | Android App Bundle | Parcial | Distribuição por .apk direto (trabalho acadêmico, fora da Play Store). Para a loja, gerar `.aab` (`AndroidPackageFormat=aab`). Não carrega código de fora do pacote. |
| Cryptographic_Algorithms | Cripto da plataforma | Atende | Nenhum algoritmo próprio; JWT e TLS pelas bibliotecas padrão. |

## 5. Google Play

| ID | Situação |
|---|---|
| Play_Content_Policies, Play_Content_Rating, Play_Feature_Graphic, Play_Device_References, Play_Misleading_Content, Play_User_Reviews | N/A. App não está publicado na Play Store (distribuição por .apk para a avaliação). |

---

## O que fica como recomendação (não feito)
1. **Accessibility Scanner** no celular real, para fechar Content_Description e contraste de forma automática.
2. **SQLitePCLRaw**: atualizar quando sair versão corrigida do GHSA-2m69.
3. **Credential Manager** e **biometria**: melhorias de produto para uma versão futura.
4. **.aab** se um dia for para a Play Store.
5. Medir **renderização** e **abertura** em aparelho real (o emulador usa GPU por software).
