/* =========================================================================
   FORMAS DE PAGAMENTO

   Irmã da tela de Categorias, e de propósito: as duas são cadastros de
   apoio da movimentação financeira. A categoria diz o que foi o lançamento,
   a forma de pagamento diz como foi pago.

   Segue docs/prototipo/imagens/06-formas-de-pagamento.png, sem os dois
   recursos que o modelo de dados ainda não suporta: o contador de uso
   ("Usada em 38 lançamentos") e a separação entre formas padrão do sistema
   e formas cadastradas pela empresa. A entidade FormaPagamento tem apenas
   Nome e Ativo.
   ========================================================================= */

if (iniciarTela('formas-pagamento.html')) {

  const lista = document.getElementById('lista');
  const aviso = document.getElementById('aviso');
  const botaoNova = document.getElementById('botao-nova');
  const mostrarInativas = document.getElementById('mostrar-inativas');

  const dialogo = document.getElementById('dialogo');
  const dialogoTitulo = document.getElementById('dialogo-titulo');
  const dialogoAviso = document.getElementById('dialogo-aviso');
  const campoNome = document.getElementById('campo-nome');
  const campoAtivo = document.getElementById('campo-ativo');
  const notaSoftDelete = document.getElementById('nota-soft-delete');
  const botaoSalvar = document.getElementById('botao-salvar');

  const podeEditar = Sessao.podeGerenciarFinanceiro();

  let formas = [];
  let editando = null;


  /* --- Avisos ---------------------------------------------------------- */

  function avisar(mensagem, estilo) {
    aviso.className = 'aviso aviso-' + estilo;
    aviso.textContent = mensagem;
    aviso.hidden = false;
  }


  /* --- Carregar e desenhar --------------------------------------------- */

  async function carregar() {
    lista.innerHTML = '<li class="vazio">Carregando...</li>';

    try {
      formas = await FormasPagamento.listar(mostrarInativas.checked ? undefined : true);
      desenhar();
    } catch (erro) {
      lista.innerHTML = '';
      avisar(erro.message, 'erro');
    }
  }

  function desenhar() {
    aviso.hidden = true;
    lista.innerHTML = '';

    if (formas.length === 0) {
      lista.innerHTML =
        '<li class="vazio"><strong>Nenhuma forma de pagamento cadastrada</strong>' +
        'Cadastre Pix, Boleto ou Cartão para poder registrar lançamentos.</li>';
      return;
    }

    for (const forma of formas) {
      lista.appendChild(criarItem(forma));
    }
  }

  function criarItem(forma) {
    const item = document.createElement('li');
    item.className = 'lista-item' + (forma.ativo ? '' : ' lista-item-inativo');

    const abrir = document.createElement('button');
    abrir.className = 'lista-abrir';
    abrir.disabled = !podeEditar;

    const icone = document.createElement('span');
    icone.className = 'icone-categoria icone-pagamento';
    icone.innerHTML =
      '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" ' +
      'stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">' +
      '<rect x="2" y="5" width="20" height="14" rx="2"/><path d="M2 10h20"/></svg>';

    const dados = document.createElement('div');
    dados.className = 'lista-dados';

    const titulo = document.createElement('div');
    titulo.className = 'lista-titulo';
    titulo.textContent = forma.nome;

    const nota = document.createElement('div');
    nota.className = 'lista-nota';
    nota.textContent = forma.ativo
      ? 'Disponível para novos lançamentos'
      : 'Fora dos novos lançamentos';

    dados.appendChild(titulo);
    dados.appendChild(nota);
    abrir.appendChild(icone);
    abrir.appendChild(dados);
    abrir.addEventListener('click', () => abrirDialogo(forma));
    item.appendChild(abrir);

    const interruptor = document.createElement('label');
    interruptor.className = 'interruptor';

    const caixa = document.createElement('input');
    caixa.type = 'checkbox';
    caixa.checked = forma.ativo;
    caixa.disabled = !podeEditar;
    caixa.setAttribute('aria-label', 'Forma ' + forma.nome + ' ativa');
    caixa.addEventListener('change', () => alternar(forma, caixa));

    const trilho = document.createElement('span');
    trilho.className = 'interruptor-trilho';

    interruptor.appendChild(caixa);
    interruptor.appendChild(trilho);
    item.appendChild(interruptor);

    return item;
  }


  /* --- Ligar e desligar pela lista -------------------------------------- */

  async function alternar(forma, caixa) {
    const novoEstado = caixa.checked;

    try {
      await FormasPagamento.atualizar(forma.id, { nome: forma.nome, ativo: novoEstado });

      forma.ativo = novoEstado;
      await carregar();
      avisar(novoEstado ? 'Forma reativada.' : 'Forma desativada.', 'sucesso');
    } catch (erro) {
      caixa.checked = !novoEstado;
      avisar(erro.message, 'erro');
    }
  }


  /* --- Diálogo ---------------------------------------------------------- */

  function abrirDialogo(forma) {
    editando = forma || null;
    dialogoAviso.hidden = true;

    dialogoTitulo.textContent = editando ? 'Editar forma de pagamento' : 'Nova forma de pagamento';
    campoNome.value = editando ? editando.nome : '';
    campoAtivo.checked = editando ? editando.ativo : true;
    botaoSalvar.textContent = editando ? 'Salvar alterações' : 'Criar forma';
    notaSoftDelete.hidden = !editando;

    dialogo.hidden = false;
    campoNome.focus();
  }

  function fecharDialogo() {
    dialogo.hidden = true;
    editando = null;
  }

  async function salvar() {
    const nome = campoNome.value.trim();

    if (nome === '') {
      dialogoAviso.textContent = 'Informe o título da forma de pagamento.';
      dialogoAviso.hidden = false;
      campoNome.focus();
      return;
    }

    const dados = { nome, ativo: campoAtivo.checked };
    botaoSalvar.disabled = true;

    try {
      if (editando) {
        await FormasPagamento.atualizar(editando.id, dados);
      } else {
        await FormasPagamento.criar(dados);
      }

      fecharDialogo();
      await carregar();
      avisar('Forma de pagamento salva.', 'sucesso');
    } catch (erro) {
      dialogoAviso.textContent = erro.message;
      dialogoAviso.hidden = false;
    } finally {
      botaoSalvar.disabled = false;
    }
  }


  /* --- Ligações de evento ----------------------------------------------- */

  botaoNova.hidden = !podeEditar;
  botaoNova.addEventListener('click', () => abrirDialogo(null));

  mostrarInativas.addEventListener('change', carregar);

  document.getElementById('botao-cancelar').addEventListener('click', fecharDialogo);
  botaoSalvar.addEventListener('click', salvar);

  campoNome.addEventListener('keydown', evento => {
    if (evento.key === 'Enter') salvar();
  });

  document.addEventListener('keydown', evento => {
    if (evento.key === 'Escape' && !dialogo.hidden) fecharDialogo();
  });

  carregar();
}
