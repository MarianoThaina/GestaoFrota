# Aprovação de usuários com três estados (US16)

Resolve o item 7 de `pendencias-do-modelo.md` e a última lacuna da US16 no
levantamento de 20/10: quem faz login pela primeira vez fica "Aguardando
Aprovação" e só entra depois que um Administrador libera.

## O que entra

**Domínio e banco**
- `SituacaoUsuario` (AguardandoAprovacao, Ativo, Inativo) no lugar de `Usuario.Ativo`; `Ativo` continua como propriedade de leitura (não é coluna).
- Migration `20261021130000_SituacaoUsuario`: cria `Situacao` com padrão `Ativo`, marca `Inativo` quem tinha `Ativo = 0`, apaga a coluna antiga, cria índice. Quem já entrava continua entrando. `Down` refaz o booleano.

**API**
- `AuthController`: usuário novo nasce Motorista + Aguardando Aprovação e a resposta é **403** `{ mensagem, situacao }`, sem token. Inativo continua 401. `GET /auth/me` devolve 403 para quem não está Ativo.
- **Bootstrap**: o primeiro usuário da base entra como Administrador ativo; o mesmo vale para os e-mails em `Authentication:AdministradoresIniciais` (array no `appsettings.json`, ou `Authentication__AdministradoresIniciais__0` por variável). Sem isso ninguém poderia aprovar o primeiro Administrador, e o passo de `UPDATE` no banco do README deixa de ser necessário.
- `UsuariosController`: listagem com pendentes primeiro e filtro `?situacao=`, `GET pendentes/quantidade`, `PUT {id}/aprovar` (perfil opcional), `PUT {id}/situacao`; `PUT {id}/status` mantido por compatibilidade com o site web. Administrador não desativa o próprio acesso nem remove o próprio perfil de Administrador. `UsuarioResponse` ganhou `situacao` e `dataCriacao` (manteve `ativo`).
- `ViagensController` passa a checar `Situacao == Ativo` ao listar motoristas e ao abrir viagem para alguém.

**App**
- Tela de login mostra a mensagem da API ("Seu cadastro foi recebido e aguarda liberação por um administrador." ou "Usuário desativado...") em vez do erro genérico. Fechar o navegador do Google sem concluir não mostra erro.
- `ValidarSessaoAsync` trata 403 como 401: sessão salva é encerrada quando o usuário é desativado ou devolvido a pendente.
- *Usuários*: aviso com a contagem de pendentes, cartão com situação colorida, botão **Aprovar cadastro** (usa o perfil escolhido no picker, com confirmação) para pendentes; *Salvar perfil* e *Desativar/Reativar* para os demais, com confirmação ao desativar.

## Como testar

1. `dotnet build` na API e no App; `dotnet ef database update`. Conferir que `Usuarios.Ativo` sumiu e `Situacao` existe com os valores convertidos.
2. Banco vazio de usuários: primeiro login Google → 200 com token e `perfil: 1` (Administrador). `GET /api/usuarios` mostra `situacao: 2`.
3. Segunda conta Google → **403** com `mensagem` de aguardando aprovação; no app, a tela de login mostra a frase e não entra. Repetir o login: mesmo 403 (o registro não é duplicado).
4. Como Administrador: `GET /api/usuarios?situacao=1` lista a segunda conta; `PUT /api/usuarios/{id}/aprovar` com `{ "perfil": 4 }` → 204. A segunda conta entra como Motorista. Repetir o `aprovar` → 409.
5. `PUT /api/usuarios/{meu-id}/situacao` com `{ "situacao": 3 }` como o próprio Administrador → 409. Em outro usuário → 204; o login dele passa a dar 401 "Usuário desativado"; com a sessão dele ainda salva no aparelho, abrir o app leva de volta ao login.
6. No emulador, *Usuários*: aviso "1 cadastro aguardando aprovação", cartão laranja com *Aprovar cadastro*; depois de aprovar, o cartão fica verde com *Salvar perfil* e *Desativar usuário*.
7. `appsettings.Development.json` com `"AdministradoresIniciais": ["x@gmail.com"]`: login dessa conta nova entra como Administrador ativo mesmo com a base já populada.

## O que não foi conferido pelo autor

- Compilação e execução (sem SDK .NET e sem emulador).
- Migration e snapshot escritos à mão; `dotnet ef migrations add Teste` deve vir vazia (apagar depois).
- `builder.Ignore(u => u.Ativo)` sobre propriedade só de leitura: o EF Core já ignora propriedades sem setter, então a linha é redundante, não errada; se o compilador ou o EF reclamarem, basta removê-la.
- O site web (`GestaoFrota.Web`) ainda usa `ativo` da resposta de usuários e o endpoint `/status`; os dois foram mantidos, mas a tela de lá não mostra "Aguardando aprovação".
