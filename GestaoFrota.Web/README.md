# Front-end web — Gestão de Frota

Site responsivo em HTML, CSS e JavaScript puro, sem framework nem
dependência instalada. Consome a `GestaoFrota.API`.

Em telas grandes o menu é uma barra lateral fixa. Abaixo de 900px ele desce e
vira a barra inferior do protótipo de celular. É o mesmo HTML nos dois casos.

## Estrutura

```
GestaoFrota.Web/
├── index.html          Login com Google
├── categorias.html     Cadastro de categorias
├── formas-pagamento.html  Cadastro de formas de pagamento
├── usuarios.html       Perfis e acesso ao sistema
├── css/
│   └── estilo.css      Tokens do protótipo + layout + componentes
└── js/
    ├── config.js       Endereço da API e Client ID do Google
    ├── sessao.js       Guarda o JWT e responde as perguntas de permissão
    ├── api.js          fetch com Authorization e tratamento de erro
    ├── layout.js       Monta a barra lateral conforme o perfil
    ├── login.js        Fluxo do Google Sign-In
    ├── demo.js         Dados falsos e login simulado (modo demonstração)
    ├── categorias.js   Lógica da tela de categorias
    ├── formas-pagamento.js
    └── usuarios.js
```

Cada tela nova é um arquivo `.html` que reaproveita `estilo.css`, `api.js`,
`sessao.js` e `layout.js`, mais um `.js` próprio.

## Como rodar

O site precisa ser servido por HTTP. Abrir o arquivo com dois cliques não
funciona: o Google Sign-In recusa origens `file://`.

No VS Code, instale a extensão **Live Server** (autor Ritwick Dey), clique com
o botão direito em `index.html` e escolha *Open with Live Server*. O navegador
abre sozinho e recarrega a cada arquivo salvo.

Sem VS Code, qualquer servidor estático serve:

```bash
python -m http.server 5500
```

## Modo demonstração

O arquivo `js/config.js` começa com `MODO_DEMO: true`. Nesse modo o site roda
sozinho, sem API, sem banco e sem Google:

- a tela de login mostra um botão por perfil, em vez do botão do Google
- as categorias vêm de uma lista falsa em `js/demo.js`, e criar, editar e
  desativar funcionam de verdade, só que na memória do navegador

É o modo para trabalhar no visual e para apresentar o trabalho sem depender
de nada estar no ar. Recarregar a página volta os dados ao estado inicial.

Entre por perfis diferentes para ver o menu mudar: o Operador / Motorista não
recebe os itens de Financeiro, e só o Administrador vê o botão "Desativar" na
lista.

Para ligar na API de verdade, mude `MODO_DEMO` para `false` e siga a seção
abaixo. Com a API rodando em paralelo (`dotnet run` na pasta
`GestaoFrota.API`), o CORS já está liberado para qualquer origem, então não
precisa mexer em nada do lado do servidor.

## Configurar o login Google

Três valores precisam bater entre si. Se algum divergir, o login falha com
"Invalid token".

**1. Criar as credenciais**

No [Google Cloud Console](https://console.cloud.google.com/apis/credentials),
crie uma credencial do tipo *ID do cliente OAuth 2.0* → *Aplicativo da Web*, e
cadastre em **Origens JavaScript autorizadas**:

```
http://localhost:5500
```

Exatamente a porta em que você vai servir o site. Sem barra no final.

**2. Colocar o Client ID nos dois lados**

| Arquivo | Campo |
|---|---|
| `GestaoFrota.Web/js/config.js` | `GOOGLE_CLIENT_ID` |
| `GestaoFrota.API/appsettings.json` | `Authentication:Google:ClientId` |

Precisa ser o mesmo valor nos dois. A API valida se o token do Google foi
emitido para aquele Client ID (`GoogleAuthService`, propriedade `Audience`).

**3. Preencher a chave do JWT**

O `appsettings.json` ainda está com `SUBSTITUA_POR_UMA_CHAVE_SECRETA_FORTE...`
em `Jwt:Key`. Troque por uma chave de 32 caracteres ou mais, ou a API não sobe.

## Duas armadilhas da API

**Enums são números.** O `Program.cs` não registra o
`JsonStringEnumConverter`, então a API devolve e espera `tipo: 1`, não
`tipo: "Receita"`. Os mapas em `config.js` fazem essa tradução:

| Valor | Tipo de categoria | Perfil |
|---|---|---|
| 1 | Receita | Administrador |
| 2 | Despesa | Gestor de frota |
| 3 | — | Financeiro |
| 4 | — | Operador / Motorista |

**`DELETE` não apaga.** O `DELETE /api/categorias/{id}` faz exclusão lógica:
marca `Ativo = false` para preservar o histórico financeiro. Por isso o botão
se chama "Desativar" e a categoria continua na lista com a etiqueta "Inativa".

## O que a API já expõe

| Recurso | Endpoints | Permissão |
|---|---|---|
| `api/auth` | `POST /google`, `GET /me` | aberto / autenticado |
| `api/categorias` | GET, GET/{id}, POST, PUT, DELETE | ler: todos · gravar: Admin e Financeiro · DELETE: só Admin |
| `api/formaspagamento` | GET, GET/{id}, POST, PUT, DELETE | idem |
| `api/usuarios` | GET, PUT/{id}/perfil, PUT/{id}/status | só Administrador |
| `api/status` | GET | público |

## O que ainda falta na API

Movimentações, veículos, viagens, dívidas e relatórios **não têm controller**.
As entidades existem em `GestaoFrota.Domain` e as tabelas estão nas migrations,
mas nada disso está exposto por HTTP.

No menu lateral esses itens aparecem apagados, com o motivo no `title`. Assim
que os endpoints existirem, basta trocar `pronta: false` para `true` na lista
`ITENS_MENU`, em `js/layout.js`, e criar a tela.

## Permissões

`sessao.js` esconde os botões que o perfil não pode usar, e `layout.js` monta
o menu conforme o perfil — o Operador / Motorista não recebe os itens de
Financeiro, como pede a US16.

Isso é conveniência de interface, não segurança. Quem decide de verdade é o
`[Authorize(Roles = ...)]` no servidor. Se alguém burlar a tela, a API
responde 403 e o `api.js` mostra a mensagem.
