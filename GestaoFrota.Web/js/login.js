/* =========================================================================
   LOGIN

   Em modo normal, o fluxo tem três passos:

     1. O Google Identity Services mostra o botão e, depois que a pessoa
        escolhe a conta, devolve um id_token.
     2. Esse id_token vai para POST /api/auth/google.
     3. A API confere o token com o Google, encontra ou cria o usuário, e
        devolve o JWT próprio dela — que é o que usamos daqui em diante.

   A API valida se o id_token foi emitido para o mesmo ClientId configurado
   no appsettings.json dela (GoogleAuthService, propriedade Audience). Por
   isso os dois precisam ter o mesmo valor.

   Em modo demonstração, o Google sai de cena e a tela mostra um botão por
   perfil, para você ver como o menu muda em cada um.
   ========================================================================= */

const aviso = document.getElementById('aviso');
const area = document.getElementById('botao-google');

function mostrarErro(mensagem) {
  aviso.textContent = mensagem;
  aviso.hidden = false;
}

async function aoReceberTokenDoGoogle(resposta) {
  aviso.hidden = true;

  try {
    const sessao = await Auth.entrarComGoogle(resposta.credential);
    Sessao.salvar(sessao);
    window.location.href = 'categorias.html';
  } catch (erro) {
    mostrarErro(erro.message);
  }
}

function montarListaDePerfis() {
  const area = document.getElementById('area-perfis');

  const separador = document.createElement('div');
  separador.className = 'separador-login';
  separador.innerHTML = '<span>ou entre como</span>';
  area.appendChild(separador);

  // As pessoas vêm de js/demo.js, para a lista contar uma história em vez
  // de mostrar quatro nomes de perfil soltos.
  const lista = document.createElement('ul');
  lista.className = 'lista';

  for (const pessoa of PESSOAS_DEMO) {
    const item = document.createElement('li');
    item.className = 'lista-item';

    const botao = document.createElement('button');
    botao.className = 'lista-abrir';

    const avatar = document.createElement('span');
    avatar.className = 'icone-categoria';
    avatar.innerHTML =
      '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" ' +
      'stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">' +
      '<circle cx="12" cy="8" r="3.5"/><path d="M5 20a7 7 0 0 1 14 0"/></svg>';

    const dados = document.createElement('div');
    dados.className = 'lista-dados';

    const nome = document.createElement('div');
    nome.className = 'lista-titulo';
    nome.textContent = pessoa.nome;

    const cargo = document.createElement('div');
    cargo.className = 'lista-nota';
    cargo.textContent = NOME_PERFIL[pessoa.perfil];

    dados.appendChild(nome);
    dados.appendChild(cargo);
    botao.appendChild(avatar);
    botao.appendChild(dados);
    botao.addEventListener('click', () => entrarComoDemo(pessoa));

    item.appendChild(botao);
    lista.appendChild(item);
  }

  area.appendChild(lista);
}

function montarBotaoGoogle() {
  if (CONFIG.GOOGLE_CLIENT_ID.startsWith('SEU_GOOGLE_CLIENT_ID')) {
    mostrarErro('Falta configurar o Client ID do Google em js/config.js.');
    return;
  }

  google.accounts.id.initialize({
    client_id: CONFIG.GOOGLE_CLIENT_ID,
    callback: aoReceberTokenDoGoogle
  });

  google.accounts.id.renderButton(area, {
    theme: 'outline',
    size: 'large',
    text: 'signin_with',
    locale: 'pt-BR',
    width: 320
  });
}

// Se a sessão anterior ainda vale, pula o login.
if (!Sessao.expirada()) {
  window.location.href = 'categorias.html';
}

if (CONFIG.MODO_DEMO) {
  document.getElementById('nota-google').hidden = true;
  montarListaDePerfis();
} else {
  // O script do Google carrega com `defer`, então esperamos a página terminar.
  window.addEventListener('load', montarBotaoGoogle);
}
