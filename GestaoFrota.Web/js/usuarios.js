/* =========================================================================
   USUÁRIOS E PERMISSÕES

   Responde ao item 10 do entregável: a tela onde o administrador gerencia
   os perfis de acesso.

   A API expõe três coisas, todas restritas a Administrador:
     GET  /api/usuarios              lista todo mundo
     PUT  /api/usuarios/{id}/perfil  troca o perfil
     PUT  /api/usuarios/{id}/status  ativa ou desativa o acesso

   Não existe criar usuário: a conta nasce sozinha no primeiro login com
   Google, dentro do AuthController. Por isso esta tela não tem botão de
   adicionar — ela administra quem já entrou.

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

    dados.appendChild(titulo);
    dados.appendChild(email);

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


  /* --- Início ----------------------------------------------------------- */

  // A API responde 403 para quem não é Administrador. Aqui a tela avisa
  // antes, com uma mensagem que explica em vez de só recusar.
  if (Sessao.ehAdministrador()) {
    carregar();
  } else {
    areaLista.hidden = true;
    avisar('Só administradores podem gerenciar usuários e permissões.', 'info');
  }
}
