/* =========================================================================
   CATEGORIAS

   Segue o desenho de docs/prototipo/imagens/04-categorias.png e
   05-editar-categoria-soft-delete.png:

     - a lista é dividida em dois blocos, Entradas e Saídas
     - cada item tem um ícone redondo, verde ou vermelho
     - o interruptor à direita liga e desliga a categoria ali mesmo
     - tocar no item abre o formulário de edição
     - o botão de criar é flutuante, no canto inferior

   Sobre a desativação: não existe "Excluir" em lugar nenhum. A API faz
   exclusão lógica para preservar o histórico financeiro. Desligar o
   interruptor é um PUT com ativo = false.
   ========================================================================= */

if (iniciarTela('categorias.html')) {

  const secoes = document.getElementById('secoes');
  const aviso = document.getElementById('aviso');
  const botaoNova = document.getElementById('botao-nova');
  const mostrarInativas = document.getElementById('mostrar-inativas');

  const dialogo = document.getElementById('dialogo');
  const dialogoTitulo = document.getElementById('dialogo-titulo');
  const dialogoAviso = document.getElementById('dialogo-aviso');
  const campoNome = document.getElementById('campo-nome');
  const seletorTipo = document.getElementById('seletor-tipo');
  const campoAtivo = document.getElementById('campo-ativo');
  const notaSoftDelete = document.getElementById('nota-soft-delete');
  const botaoSalvar = document.getElementById('botao-salvar');

  const podeEditar = Sessao.podeGerenciarFinanceiro();

  const SETA_ENTRADA = '<path d="M12 5v14M6 13l6 6 6-6"/>';
  const SETA_SAIDA = '<path d="M12 19V5M6 11l6-6 6 6"/>';

  let categorias = [];
  let tipoSelecionado = 'todos';
  let tipoNoFormulario = TIPO_CATEGORIA.DESPESA;
  let editando = null;


  /* --- Avisos ---------------------------------------------------------- */

  function avisar(mensagem, estilo) {
    aviso.className = 'aviso aviso-' + estilo;
    aviso.textContent = mensagem;
    aviso.hidden = false;
  }


  /* --- Carregar e desenhar --------------------------------------------- */

  async function carregar() {
    secoes.innerHTML = '<p class="vazio">Carregando...</p>';

    try {
      // Sem o parâmetro, a API devolve ativas e inativas.
      categorias = await Categorias.listar(mostrarInativas.checked ? undefined : true);
      desenhar();
    } catch (erro) {
      secoes.innerHTML = '';
      avisar(erro.message, 'erro');
    }
  }

  function desenhar() {
    aviso.hidden = true;
    secoes.innerHTML = '';

    const blocos = [
      { titulo: 'Entradas', tipo: TIPO_CATEGORIA.RECEITA },
      { titulo: 'Saídas', tipo: TIPO_CATEGORIA.DESPESA }
    ];

    let mostrouAlgo = false;

    for (const bloco of blocos) {
      if (tipoSelecionado !== 'todos' && Number(tipoSelecionado) !== bloco.tipo) {
        continue;
      }

      const doBloco = categorias.filter(c => c.tipo === bloco.tipo);
      if (doBloco.length === 0) continue;

      mostrouAlgo = true;

      const titulo = document.createElement('h2');
      titulo.className = 'lista-secao';
      titulo.textContent = bloco.titulo;
      secoes.appendChild(titulo);

      const lista = document.createElement('ul');
      lista.className = 'lista';

      for (const categoria of doBloco) {
        lista.appendChild(criarItem(categoria));
      }

      secoes.appendChild(lista);
    }

    if (!mostrouAlgo) {
      secoes.innerHTML =
        '<p class="vazio"><strong>Nenhuma categoria por aqui</strong>' +
        'Crie a primeira para começar a classificar os lançamentos.</p>';
    }
  }

  function criarItem(categoria) {
    const ehEntrada = categoria.tipo === TIPO_CATEGORIA.RECEITA;

    const item = document.createElement('li');
    item.className = 'lista-item' + (categoria.ativo ? '' : ' lista-item-inativo');

    // Área clicável: ícone + nome + subtítulo.
    const abrir = document.createElement('button');
    abrir.className = 'lista-abrir';
    abrir.disabled = !podeEditar;

    const icone = document.createElement('span');
    icone.className = 'icone-categoria ' + (ehEntrada ? 'icone-entrada' : 'icone-saida');
    icone.innerHTML =
      '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" ' +
      'stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">' +
      (ehEntrada ? SETA_ENTRADA : SETA_SAIDA) + '</svg>';

    const dados = document.createElement('div');
    dados.className = 'lista-dados';

    const titulo = document.createElement('div');
    titulo.className = 'lista-titulo';
    titulo.textContent = categoria.nome;

    const nota = document.createElement('div');
    nota.className = 'lista-nota';
    nota.textContent = categoria.ativo
      ? NOME_TIPO_CATEGORIA[categoria.tipo]
      : NOME_TIPO_CATEGORIA[categoria.tipo] + ' · fora dos novos lançamentos';

    dados.appendChild(titulo);
    dados.appendChild(nota);
    abrir.appendChild(icone);
    abrir.appendChild(dados);
    abrir.addEventListener('click', () => abrirDialogo(categoria));
    item.appendChild(abrir);

    // Interruptor de ativa / inativa.
    const interruptor = document.createElement('label');
    interruptor.className = 'interruptor';

    const caixa = document.createElement('input');
    caixa.type = 'checkbox';
    caixa.checked = categoria.ativo;
    caixa.disabled = !podeEditar;
    caixa.setAttribute('aria-label', 'Categoria ' + categoria.nome + ' ativa');
    caixa.addEventListener('change', () => alternar(categoria, caixa));

    const trilho = document.createElement('span');
    trilho.className = 'interruptor-trilho';

    interruptor.appendChild(caixa);
    interruptor.appendChild(trilho);
    item.appendChild(interruptor);

    return item;
  }


  /* --- Ligar e desligar pela lista -------------------------------------- */

  async function alternar(categoria, caixa) {
    const novoEstado = caixa.checked;

    try {
      await Categorias.atualizar(categoria.id, {
        nome: categoria.nome,
        tipo: categoria.tipo,
        ativo: novoEstado
      });

      categoria.ativo = novoEstado;
      await carregar();
      avisar(novoEstado ? 'Categoria reativada.' : 'Categoria desativada.', 'sucesso');
    } catch (erro) {
      caixa.checked = !novoEstado;
      avisar(erro.message, 'erro');
    }
  }


  /* --- Diálogo ---------------------------------------------------------- */

  function marcarTipo(tipo) {
    tipoNoFormulario = tipo;

    for (const botao of seletorTipo.children) {
      botao.classList.toggle('ativo', Number(botao.dataset.tipo) === tipo);
    }
  }

  function abrirDialogo(categoria) {
    editando = categoria || null;
    dialogoAviso.hidden = true;

    dialogoTitulo.textContent = editando ? 'Editar categoria' : 'Nova categoria';
    campoNome.value = editando ? editando.nome : '';
    campoAtivo.checked = editando ? editando.ativo : true;
    botaoSalvar.textContent = editando ? 'Salvar alterações' : 'Criar categoria';
    notaSoftDelete.hidden = !editando;
    marcarTipo(editando ? editando.tipo : TIPO_CATEGORIA.DESPESA);

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
      dialogoAviso.textContent = 'Informe o título da categoria.';
      dialogoAviso.hidden = false;
      campoNome.focus();
      return;
    }

    const dados = { nome, tipo: tipoNoFormulario, ativo: campoAtivo.checked };
    botaoSalvar.disabled = true;

    try {
      if (editando) {
        await Categorias.atualizar(editando.id, dados);
      } else {
        await Categorias.criar(dados);
      }

      fecharDialogo();
      await carregar();
      avisar('Categoria salva.', 'sucesso');
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

  for (const botao of document.querySelectorAll('.segmento')) {
    botao.addEventListener('click', () => {
      document.querySelector('.segmento.ativo').classList.remove('ativo');
      botao.classList.add('ativo');
      tipoSelecionado = botao.dataset.tipo;
      desenhar();
    });
  }

  for (const botao of seletorTipo.children) {
    botao.addEventListener('click', () => marcarTipo(Number(botao.dataset.tipo)));
  }

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
