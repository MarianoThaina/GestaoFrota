/* =========================================================================
   CONFIGURAÇÃO

   Único arquivo que você precisa editar para apontar o site para outra
   API ou outra conta do Google.
   ========================================================================= */

const CONFIG = {

  // Modo demonstração.
  //
  //   true  → o site roda sozinho, com dados falsos na memória e login
  //           simulado. Serve para trabalhar no visual sem depender da API.
  //   false → o site fala com a API de verdade e exige login pelo Google.
  //
  // Deixe false na entrega final (já está false).
  MODO_DEMO: false,

  // Endereço da API. O perfil http do projeto sobe em 5137 e o https em 7201
  // (GestaoFrota.API/Properties/launchSettings.json).
  //
  // Use o https se o navegador já confiar no certificado de desenvolvimento;
  // caso contrário o http evita o aviso de certificado inválido.
  API: 'https://gestaofrota.runasp.net',

  // Client ID do Google, do tipo "Aplicativo da Web".
  //
  // Precisa ser exatamente o mesmo valor de
  // GestaoFrota.API/appsettings.json > Authentication:Google:ClientId,
  // porque a API valida se o token foi emitido para esse ClientId.
  GOOGLE_CLIENT_ID: '869635075019-me5auvsrffljgkk40oc9jfucd91sblls.apps.googleusercontent.com'
};

// Os enums chegam da API como número, não como texto: o Program.cs não
// registra o JsonStringEnumConverter. Estes mapas fazem a tradução.

const TIPO_CATEGORIA = {
  RECEITA: 1,
  DESPESA: 2
};

const NOME_TIPO_CATEGORIA = {
  1: 'Receita',
  2: 'Despesa'
};

const PERFIL = {
  ADMINISTRADOR: 1,
  GESTOR_DE_FROTA: 2,
  FINANCEIRO: 3,
  OPERADOR_MOTORISTA: 4
};

const NOME_PERFIL = {
  1: 'Administrador',
  2: 'Gestor de frota',
  3: 'Financeiro',
  4: 'Operador / Motorista'
};
