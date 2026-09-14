/* =========================================================================
   MODO DEMONSTRAÇÃO

   Só entra em ação quando CONFIG.MODO_DEMO é true.

   Troca as funções que falam com a API por versões que guardam tudo na
   memória do navegador. As telas não sabem da diferença: continuam chamando
   Categorias.listar(), Categorias.criar() e assim por diante.

   Serve para dois momentos:
     - trabalhar no visual sem precisar da API, do banco e do Google
     - apresentar o trabalho sem depender de nada estar no ar

   Nada aqui vai para a entrega final. Quando a API estiver pronta, mude
   MODO_DEMO para false em config.js e este arquivo fica dormindo.
   ========================================================================= */

if (CONFIG.MODO_DEMO) {

  /* --- Dados falsos ----------------------------------------------------- */

  // Os mesmos nomes que aparecem em docs/prototipo/imagens/04-categorias.png.
  let categoriasDemo = [
    { id: 'demo-1', nome: 'Frete', tipo: 1, ativo: true },
    { id: 'demo-2', nome: 'Reembolso de cliente', tipo: 1, ativo: true },
    { id: 'demo-3', nome: 'Combustível', tipo: 2, ativo: true },
    { id: 'demo-4', nome: 'Pedágio', tipo: 2, ativo: true },
    { id: 'demo-5', nome: 'Manutenção', tipo: 2, ativo: true },
    { id: 'demo-6', nome: 'Diária de ajudante', tipo: 2, ativo: true },
    { id: 'demo-7', nome: 'Vale-pedágio antigo', tipo: 2, ativo: false }
  ];

  // As pessoas da lista "ou entre como" da tela de login. Os nomes vieram
  // de docs/prototipo/imagens/01-login.png e 14-usuarios-permissoes.png.
  window.PESSOAS_DEMO = [
    { id: 'demo-1', nome: 'Ana Ribeiro', email: 'ana.ribeiro@translog.com.br', perfil: 1 },
    { id: 'demo-3', nome: 'Beatriz Lima', email: 'beatriz.lima@translog.com.br', perfil: 3 },
    { id: 'demo-2', nome: 'Carlos Menezes', email: 'carlos.menezes@translog.com.br', perfil: 2 },
    { id: 'demo-4', nome: 'João Batista', email: 'joao.batista@translog.com.br', perfil: 4 }
  ];

  // Um respiro antes de responder, para as telas mostrarem o "Carregando..."
  // como mostrariam com a API de verdade.
  function responder(valor) {
    return new Promise(resolve => setTimeout(() => resolve(valor), 250));
  }

  function novoId() {
    return 'demo-' + Date.now();
  }


  /* --- Categorias ------------------------------------------------------- */

  Categorias.listar = function (ativo) {
    const lista = ativo === undefined
      ? categoriasDemo
      : categoriasDemo.filter(c => c.ativo === ativo);

    const ordenada = [...lista].sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR'));
    return responder(ordenada);
  };

  Categorias.criar = function (categoria) {
    const duplicada = categoriasDemo.some(
      c => c.nome === categoria.nome && c.tipo === categoria.tipo
    );

    if (duplicada) {
      return Promise.reject(new ErroApi(409, 'Já existe uma categoria com esse nome e tipo.'));
    }

    const nova = { id: novoId(), ...categoria };
    categoriasDemo.push(nova);
    return responder(nova);
  };

  Categorias.atualizar = function (id, categoria) {
    const alvo = categoriasDemo.find(c => c.id === id);
    Object.assign(alvo, categoria);
    return responder(null);
  };

  Categorias.desativar = function (id) {
    const alvo = categoriasDemo.find(c => c.id === id);
    alvo.ativo = false;
    return responder(null);
  };


  /* --- Formas de pagamento ---------------------------------------------- */

  // Os mesmos nomes de docs/prototipo/imagens/06-formas-de-pagamento.png.
  let formasDemo = [
    { id: 'forma-1', nome: 'Pix', ativo: true },
    { id: 'forma-2', nome: 'Boleto', ativo: true },
    { id: 'forma-3', nome: 'Transferência TED', ativo: true },
    { id: 'forma-4', nome: 'Cartão Corporativo', ativo: true },
    { id: 'forma-5', nome: 'Dinheiro em espécie', ativo: true },
    { id: 'forma-6', nome: 'Vale-pedágio antigo', ativo: false }
  ];

  FormasPagamento.listar = function (ativo) {
    const lista = ativo === undefined
      ? formasDemo
      : formasDemo.filter(f => f.ativo === ativo);

    return responder([...lista]);
  };

  FormasPagamento.criar = function (forma) {
    if (formasDemo.some(f => f.nome === forma.nome)) {
      return Promise.reject(new ErroApi(409, 'Já existe uma forma de pagamento com esse nome.'));
    }

    const nova = { id: novoId(), ...forma };
    formasDemo.push(nova);
    return responder(nova);
  };

  FormasPagamento.atualizar = function (id, forma) {
    Object.assign(formasDemo.find(f => f.id === id), forma);
    return responder(null);
  };

  FormasPagamento.desativar = function (id) {
    formasDemo.find(f => f.id === id).ativo = false;
    return responder(null);
  };


  /* --- Usuários --------------------------------------------------------- */

  let usuariosDemo = [
    { id: 'demo-1', nome: 'Ana Ribeiro', email: 'ana.ribeiro@translog.com.br', fotoUrl: null, perfil: 1, ativo: true, ultimoLogin: null },
    { id: 'demo-2', nome: 'Carlos Menezes', email: 'carlos.menezes@translog.com.br', fotoUrl: null, perfil: 2, ativo: true, ultimoLogin: null },
    { id: 'demo-3', nome: 'Beatriz Lima', email: 'beatriz.lima@translog.com.br', fotoUrl: null, perfil: 3, ativo: true, ultimoLogin: null },
    { id: 'demo-4', nome: 'João Batista', email: 'joao.batista@translog.com.br', fotoUrl: null, perfil: 4, ativo: true, ultimoLogin: null },
    { id: 'demo-5', nome: 'Marina Souza', email: 'marina.souza@translog.com.br', fotoUrl: null, perfil: 4, ativo: false, ultimoLogin: null }
  ];

  Usuarios.listar = function () {
    return responder([...usuariosDemo]);
  };

  Usuarios.alterarPerfil = function (id, perfil) {
    usuariosDemo.find(u => u.id === id).perfil = perfil;
    return responder(null);
  };

  Usuarios.alterarStatus = function (id, ativo) {
    usuariosDemo.find(u => u.id === id).ativo = ativo;
    return responder(null);
  };


  /* --- Login simulado ---------------------------------------------------- */

  // Chamado pelos botões de perfil da tela de login.
  window.entrarComoDemo = function (pessoa) {
    Sessao.salvar({
      token: 'token-de-demonstracao',
      expiraEm: new Date(Date.now() + 8 * 60 * 60 * 1000).toISOString(),
      usuarioId: pessoa.id,
      nome: pessoa.nome,
      email: pessoa.email,
      fotoUrl: null,
      perfil: pessoa.perfil
    });

    window.location.href = 'categorias.html';
  };
}
