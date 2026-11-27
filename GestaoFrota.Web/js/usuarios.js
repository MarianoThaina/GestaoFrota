/* =========================================================================
   USUÁRIOS E PERMISSÕES

   Responde ao item 10 do entregável: a tela onde o administrador gerencia
   os perfis de acesso.

   A API expõe, tudo restrito a Administrador:
     GET  /api/usuarios              lista todo mundo
     POST /api/usuarios              cria conta com perfil e senha inicial
     PUT  /api/usuarios/{id}/perfil  troca o perfil
     PUT  /api/usuarios/{id}/senha   define ou redefine a senha
     PUT  /api/usuarios/{id}/status  ativa ou desativa o acesso

   Duas formas de a conta nascer: o Administrador cria aqui (e-mail e senha,
   já com o perfil certo) ou a pessoa entra pelo Google pela primeira vez
   (nasce Motorista). Se a conta criada aqui entrar depois pelo Google com o
   mesmo e-mail, a API vincula as duas.

   O estado "Aguardando aprovação" que aparece em
   docs/prototipo/imagens/14-usuarios-permissoes.png não foi implementado,
   porque a entidade Usuario tem apenas Ativo, que é sim ou não. É uma
   pendência já registrada no estado-atual.md do projeto.
   ========================================================================= */

if (iniciarTela('usuarios.html')) {

  const lista = document.getElementById('lista');
  const areaLista = document.getElementById('area-lista');
  const aviso = document.getElementById('aviso');

  const eu = Sessao.usuario();

  let usuarios = [];


  function avisar(mensagem, estilo) {
    aviso.className = 'aviso aviso-' + estilo;
    aviso.textContent = mensagem;
    aviso.hidden = false;
  }


  /* --- Carregar e desenhar --------------------------------------------- */

  async function carregar() {
    lista.innerHTML = '<li class="vazio">Carregando...</li>';

    try {
      usuarios = await Usuarios.listar();
      desenhar();
    } catch (erro) {
      lista.innerHTML = '';
      avisar(erro.message, 'erro');
    }
  }

  function desenhar() {
    aviso.hidden = true;
    lista.innerHTML = '';

    for (const usuario of usuarios) {
      lista.appendChild(criarItem(usuario));
    }
  }

  function criarItem(usuario) {
    const souEu = usuario.id === eu.id;

    const item = document.createElement('li');
    item.className = 'lista-item' + (usuario.ativo ? '' : ' lista-item-inativo');

    const avatar = document.createElement('span');
    avatar.className = 'avatar';
    avatar.textContent = iniciais(usuario.nome);

    const dados = document.createElement('div');
    dados.className = 'lista-dados';

    const titulo = document.createElement('div');
    titulo.className = 'lista-titulo';
    titulo.textContent = usuario.nome;

    if (souEu) {
      const etiqueta = document.createElement('span');
      etiqueta.className = 'etiqueta etiqueta-primaria';
      etiqueta.textContent = 'você';
      titulo.appendChild(etiqueta);
    }

    const email = document.createElement('div');
    email.className = 'lista-nota';
    email.textContent = usuario.email;

    const formas = document.createElement('div');
    formas.className = 'lista-nota';
    formas.textContent = formasDeEntrar(usuario);

    const botaoSenha = document.createElement('button');
    botaoSenha.type = 'button';
    botaoSenha.className = 'botao botao-texto';
    botaoSenha.textContent = usuario.temSenha ? 'Redefinir senha' : 'Definir senha';
    botaoSenha.addEventListener('click', () => abrirDialogo(usuario));

    dados.appendChild(titulo);
    dados.appendChild(email);
    dados.appendChild(formas);
    dados.appendChild(botaoSenha);

    // Seletor de perfil.
    const perfil = document.createElement('select');
    perfil.className = 'seletor-perfil';
    perfil.setAttribute('aria-label', 'Perfil de ' + usuario.nome);

    for (const valor of [1, 2, 3, 4]) {
      const opcao = document.createElement('option');
      opcao.value = valor;
      opcao.textContent = NOME_PERFIL[valor];
      opcao.selected = valor === usuario.perfil;
      perfil.appendChild(opcao);
    }

    // Um administrador não rebaixa a si mesmo: ele perderia o acesso a esta
    // tela no meio do caminho, e talvez não sobrasse nenhum administrador.
    perfil.disabled = souEu;
    perfil.addEventListener('change', () => trocarPerfil(usuario, perfil));

    // Interruptor de acesso.
    const interruptor = document.createElement('label');
    interruptor.className = 'interruptor';

    const caixa = document.createElement('input');
    caixa.type = 'checkbox';
    caixa.checked = usuario.ativo;
    caixa.disabled = souEu;
    caixa.setAttribute('aria-label', 'Acesso de ' + usuario.nome);
    caixa.addEventListener('change', () => trocarStatus(usuario, caixa));

    const trilho = document.createElement('span');
    trilho.className = 'interruptor-trilho';

    interruptor.appendChild(caixa);
    interruptor.appendChild(trilho);

    item.appendChild(avatar);
    item.appendChild(dados);
    item.appendChild(perfil);
    item.appendChild(interruptor);

    return item;
  }


  /* --- Ações ------------------------------------------------------------ */

  async function trocarPerfil(usuario, seletor) {
    const novoPerfil = Number(seletor.value);

    try {
      await Usuarios.alterarPerfil(usuario.id, novoPerfil);
      usuario.perfil = novoPerfil;
      avisar(usuario.nome + ' agora é ' + NOME_PERFIL[novoPerfil] + '.', 'sucesso');
    } catch (erro) {
      seletor.value = usuario.perfil;
      avisar(erro.message, 'erro');
    }
  }

  async function trocarStatus(usuario, caixa) {
    const novoEstado = caixa.checked;

    try {
      await Usuarios.alterarStatus(usuario.id, novoEstado);
      usuario.ativo = novoEstado;
      desenhar();
      avisar(
        novoEstado
          ? usuario.nome + ' voltou a ter acesso.'
          : usuario.nome + ' perdeu o acesso ao sistema.',
        'sucesso'
      );
    } catch (erro) {
      caixa.checked = !novoEstado;
      avisar(erro.message, 'erro');
    }
  }


  function formasDeEntrar(usuario) {
    if (usuario.temSenha && usuario.temGoogle) return 'Entra com: e-mail e senha ou Google';
    if (usuario.temSenha) return 'Entra com: e-mail e senha';
    if (usuario.temGoogle) return 'Entra com: Google';
    return 'Ainda sem forma de entrar';
  }


  /* --- Diálogo: novo usuário ou definir senha -------------------------- */

  const dialogo = document.getElementById('dialogo');
  const formUsuario = document.getElementById('form-usuario');
  const dialogoAviso = document.getElementById('dialogo-aviso');
  const camposNovo = document.getElementById('campos-novo');
  const campoNome = document.getElementById('campo-nome');
  const campoEmail = document.getElementById('campo-email');
  const campoPerfil = document.getElementById('campo-perfil');
  const campoSenha = document.getElementById('campo-senha');
  const botaoSalvar = document.getElementById('botao-salvar');

  // null = criando conta nova; um usuário = definindo a senha dele.
  let usuarioDaSenha = null;

  for (const valor of [1, 2, 3, 4]) {
    const opcao = document.createElement('option');
    opcao.value = valor;
    opcao.textContent = NOME_PERFIL[valor];
    opcao.selected = valor === PERFIL.OPERADOR_MOTORISTA;
    campoPerfil.appendChild(opcao);
  }

  function abrirDialogo(usuario) {
    usuarioDaSenha = usuario || null;
    formUsuario.reset();
    campoPerfil.value = PERFIL.OPERADOR_MOTORISTA;
    dialogoAviso.hidden = true;

    camposNovo.hidden = !!usuario;
    document.getElementById('dialogo-titulo').textContent =
      usuario ? (usuario.temSenha ? 'Redefinir senha' : 'Definir senha') : 'Novo usuário';
    document.getElementById('dialogo-explicacao').textContent = usuario
      ? 'Nova senha de ' + usuario.nome + ' (' + usuario.email + '). Ela passa a entrar também com e-mail e senha.'
      : 'A conta já nasce com o perfil escolhido e entra com e-mail e senha. Se a pessoa entrar pelo Google com o mesmo e-mail, a conta é a mesma.';
    document.getElementById('rotulo-senha').textContent = usuario ? 'Nova senha' : 'Senha inicial';
    botaoSalvar.textContent = usuario ? 'Salvar senha' : 'Criar usuário';

    dialogo.hidden = false;
    (usuario ? campoSenha : campoNome).focus();
  }

  function fecharDialogo() {
    dialogo.hidden = true;
    formUsuario.reset();
  }

  document.getElementById('botao-novo').addEventListener('click', () => abrirDialogo(null));
  document.getElementById('botao-cancelar').addEventListener('click', fecharDialogo);
  dialogo.addEventListener('keydown', (e) => { if (e.key === 'Escape') fecharDialogo(); });

  formUsuario.addEventListener('submit', async (evento) => {
    evento.preventDefault();
    dialogoAviso.hidden = true;
    botaoSalvar.disabled = true;

    try {
      if (usuarioDaSenha) {
        await Usuarios.definirSenha(usuarioDaSenha.id, campoSenha.value);
        const nome = usuarioDaSenha.nome;
        fecharDialogo();
        await carregar();
        avisar('Senha de ' + nome + ' definida. Ela já pode entrar com e-mail e senha.', 'sucesso');
      } else {
        const nome = campoNome.value.trim();
        const email = campoEmail.value.trim();
        const perfil = Number(campoPerfil.value);
        if (!nome || !email || !campoSenha.value) {
          throw new Error('Preencha nome, e-mail e senha.');
        }
        await Usuarios.criar(nome, email, perfil, campoSenha.value);
        fecharDialogo();
        await carregar();
        avisar(nome + ' (' + NOME_PERFIL[perfil] + ') já pode entrar com ' + email + ' e a senha definida.', 'sucesso');
      }
    } catch (erro) {
      dialogoAviso.textContent = erro.message;
      dialogoAviso.hidden = false;
    } finally {
      botaoSalvar.disabled = false;
    }
  });


  /* --- Início ----------------------------------------------------------- */

  // A API responde 403 para quem não é Administrador. Aqui a tela avisa
  // antes, com uma mensagem que explica em vez de só recusar.
  if (Sessao.ehAdministrador()) {
    document.getElementById('botao-novo').hidden = false;
    carregar();
  } else {
    areaLista.hidden = true;
    avisar('Só administradores podem gerenciar usuários e permissões.', 'info');
  }
}
