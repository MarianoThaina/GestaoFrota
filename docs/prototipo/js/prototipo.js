/* =========================================================================
   PROTÓTIPO — Gestão Financeira e Controle de Frota
   Comportamento das telas.

   Este arquivo é curto de propósito. Ele faz cinco coisas:
     1. Troca a tela quando você clica em algo com data-ir
     2. Mostra ou esconde itens do menu conforme o perfil escolhido
     3. Atualiza a explicação que fica ao lado do celular
     4. Faz os botões de filtro e o interruptor responderem ao clique
     5. Monta a galeria com todas as telas

   Nada aqui é obrigatório para o protótipo existir: se você apagar este
   arquivo, o index.html continua abrindo e mostrando a tela de login.
   ========================================================================= */


/* -------------------------------------------------------------------------
   1. TEXTO DA EXPLICAÇÃO DE CADA TELA
   Serve tanto para o painel ao lado do celular quanto para os títulos da
   galeria. Para mudar um texto, é só editar aqui embaixo.
   ------------------------------------------------------------------------- */

var TELAS = {
  'login': {
    us: 'US15',
    titulo: 'Login com Google',
    texto: 'Entrada pelo fluxo nativo do Google, sem formulário de e-mail e senha dentro do aplicativo.',
    itens: [
      'Botão claro e padronizado "Entrar com Google"',
      'Nenhum campo de senha próprio: o fluxo é OAuth2',
      'Mensagem amigável se a conexão falhar',
      'Usuário sem aprovação não entra (US16)'
    ]
  },
  'inicio': {
    us: 'US11',
    titulo: 'Indicadores na tela inicial',
    texto: 'Visão instantânea da saúde do negócio assim que o aplicativo abre.',
    itens: [
      'Saldo atual, a receber, a pagar e veículos em viagem',
      'Filtro por mês, ano ou período personalizado',
      'Valores em real, com verde e vermelho discretos',
      'Previsão de caixa vinda do motor da US05'
    ]
  },
  'financeiro': {
    us: 'US03',
    titulo: 'Movimentações de caixa',
    texto: 'Histórico de entradas e saídas, com status de pagamento visível em cada linha.',
    itens: [
      'Entradas e saídas na mesma lista, separadas por cor',
      'Status pendente ou pago em destaque',
      'Vínculo com a viagem que gerou o custo (US09)',
      'Atalho para dívidas e parcelamentos'
    ]
  },
  'nova-movimentacao': {
    us: 'US03',
    menu: 'financeiro',
    titulo: 'Registro de movimentação',
    texto: 'Formulário de lançamento. Está propositalmente com um erro na tela para mostrar a validação funcionando.',
    itens: [
      'Valor, descrição, categoria e forma de pagamento',
      'Valor zero ou negativo é recusado',
      'Data de pagamento vira obrigatória quando o status é pago',
      'Anexo de comprovante pela câmera ou galeria',
      'Litros abastecidos, que alimentam o km por litro (US09)'
    ]
  },
  'categorias': {
    us: 'US01',
    titulo: 'Categorias de movimentação',
    texto: 'Cadastro das categorias que classificam cada entrada e saída do caixa.',
    menu: 'financeiro',
    itens: [
      'Criar, editar e desativar categorias',
      'Título obrigatório e tipo Entrada ou Saída obrigatório',
      'Categorias padrão já vêm cadastradas',
      'Nada é excluído: a categoria inativa fica na lista, apagada'
    ]
  },
  'editar-categoria': {
    us: 'US01',
    titulo: 'Edição e desativação (soft delete)',
    texto: 'A resposta à pergunta do enunciado sobre como a interface evita quebrar o banco.',
    menu: 'financeiro',
    itens: [
      'Em nenhum lugar existe o botão "Excluir"',
      'A ação destrutiva se chama "Desativar categoria"',
      'A tela informa quantos lançamentos dependem dela',
      'Desativada, ela some dos novos lançamentos e fica nos relatórios'
    ]
  },
  'formas-pagamento': {
    us: 'US02',
    titulo: 'Formas de pagamento',
    texto: 'As quatro opções fixas exigidas pela user story, mais as que a empresa criar.',
    menu: 'financeiro',
    itens: [
      'Pix, Boleto, Transferência TED e Cartão Corporativo fixos',
      'As fixas não podem ser removidas: o sistema depende delas',
      'A empresa pode cadastrar formas próprias',
      'As próprias podem ser desativadas sem afetar o histórico'
    ]
  },
  'nova-forma-pagamento': {
    us: 'US02',
    titulo: 'Cadastro de forma de pagamento',
    texto: 'Formulário para a empresa criar uma forma de pagamento própria.',
    menu: 'financeiro',
    itens: [
      'Nome obrigatório e sem repetir uma forma existente',
      'Nasce disponível, e pode ser desligada depois',
      'Vale para toda a empresa, em todos os lançamentos'
    ]
  },
  'dividas': {
    us: 'US04',
    menu: 'financeiro',
    titulo: 'Dívidas e parcelamentos',
    texto: 'Financiamentos de longo prazo que geram parcelas automáticas no fluxo de caixa.',
    itens: [
      'Quantidade de parcelas e valor de cada uma',
      'Soma total calculada sozinha',
      'Valor de quitação antecipada',
      'As parcelas futuras entram no caixa como pendentes'
    ]
  },
  'frota': {
    us: 'US06',
    titulo: 'Frota de veículos',
    texto: 'Cadastro dos veículos com o status operacional e os números de desempenho de cada um.',
    itens: [
      'Placa nos padrões Mercosul e antigo',
      'Status disponível, em viagem ou em manutenção',
      'Custo por quilômetro e consumo médio (US10)',
      'Manutenção lança a despesa no caixa sozinha (US06-b)'
    ]
  },
  'viagens': {
    us: 'US08',
    titulo: 'Viagens e hodômetro',
    texto: 'A tela que o motorista usa em campo. É nela que o aplicativo abre para o perfil dele.',
    itens: [
      'Abertura e fechamento com apontamento de hodômetro',
      'Quilometragem rodada calculada pelo sistema',
      'Funciona sem internet e sincroniza depois',
      'O motorista vê apenas as viagens dele (US18)'
    ]
  },
  'encerrar-viagem': {
    us: 'US08',
    menu: 'viagens',
    titulo: 'Encerramento de viagem',
    texto: 'A validação crítica do projeto: o hodômetro final não pode ser menor nem igual ao inicial.',
    itens: [
      'O sistema bloqueia o fechamento com valor inválido',
      'Mensagem de erro explicando o que está errado',
      'A quilometragem rodada é calculada e salva',
      'Sem rede, o registro fica guardado no aparelho'
    ]
  },
  'relatorios': {
    us: 'US12',
    menu: 'inicio',
    titulo: 'Relatórios e gráficos',
    texto: 'Consolidação visual do desempenho financeiro e logístico do período.',
    itens: [
      'Entradas contra saídas ao longo dos meses',
      'Distribuição das despesas por categoria',
      'Três rotas mais lucrativas (US13)',
      'Três veículos com maior custo por quilômetro (US13)',
      'Exportação do período em CSV (US14)'
    ]
  },
  'usuarios': {
    us: 'US16',
    menu: 'ajustes',
    titulo: 'Usuários e permissões',
    texto: 'Tela visível apenas para o Administrador, onde os cadastros são aprovados.',
    itens: [
      'Novo cadastro entra como aguardando aprovação',
      'O administrador aprova e define o perfil de acesso',
      'Quatro perfis: Administrador, Gestor de Frota, Financeiro e Motorista',
      'Quem não foi aprovado não usa o aplicativo'
    ]
  },
  'ajustes': {
    us: 'US15',
    titulo: 'Ajustes e conta',
    texto: 'Dados do perfil conectado, preferências e a opção de sair exigida pela user story.',
    itens: [
      'Perfil de acesso do usuário em destaque',
      'Opção de sair da conta',
      'Gestão de usuários apenas para o Administrador',
      'Alternância de tema claro e escuro'
    ]
  }
};

/* A ordem em que as telas aparecem na galeria. */
var ORDEM_DAS_TELAS = [
  'login', 'inicio',
  'financeiro', 'categorias', 'editar-categoria', 'formas-pagamento',
  'nova-forma-pagamento', 'nova-movimentacao', 'dividas',
  'frota', 'viagens', 'encerrar-viagem', 'relatorios', 'usuarios', 'ajustes'
];

/* Qual tela abre primeiro para cada perfil.
   A US17 diz que o motorista deve abrir direto em Viagens. */
var TELA_INICIAL_DO_PERFIL = {
  ADMINISTRADOR: 'inicio',
  GESTOR_FROTA: 'inicio',
  FINANCEIRO: 'inicio',
  MOTORISTA: 'viagens'
};

/* Perfil escolhido no seletor do topo. */
var perfilAtual = 'ADMINISTRADOR';


/* -------------------------------------------------------------------------
   2. TROCAR DE TELA
   ------------------------------------------------------------------------- */

function mostrarTela(nome) {
  var telas = document.querySelectorAll('#celular-tela .tela');

  for (var i = 0; i < telas.length; i++) {
    if (telas[i].getAttribute('data-tela') === nome) {
      telas[i].classList.add('ativa');
    } else {
      telas[i].classList.remove('ativa');
    }
  }

  atualizarMenu(nome);
  atualizarLegenda(nome);
}

/* Marca o item certo do menu e esconde o menu na tela de login.
   Telas filhas (Categorias, por exemplo) declaram um "menu" nos dados acima
   para manter o item pai aceso enquanto você navega dentro dele. */
function atualizarMenu(nome) {
  var menu = document.getElementById('menu-inferior');
  menu.style.display = (nome === 'login') ? 'none' : 'flex';

  var aceso = (TELAS[nome] && TELAS[nome].menu) || nome;

  var botoes = menu.querySelectorAll('button');
  for (var i = 0; i < botoes.length; i++) {
    var destino = botoes[i].getAttribute('data-ir');
    if (destino === aceso) {
      botoes[i].classList.add('ativo');
    } else {
      botoes[i].classList.remove('ativo');
    }
  }
}

/* Preenche o painel de explicação ao lado do celular. */
function atualizarLegenda(nome) {
  var dados = TELAS[nome];
  if (!dados) return;

  document.getElementById('legenda-us').textContent = dados.us;
  document.getElementById('legenda-titulo').textContent = dados.titulo;
  document.getElementById('legenda-texto').textContent = dados.texto;

  var lista = document.getElementById('legenda-itens');
  lista.innerHTML = '';

  for (var i = 0; i < dados.itens.length; i++) {
    var item = document.createElement('li');
    item.textContent = dados.itens[i];
    lista.appendChild(item);
  }
}


/* -------------------------------------------------------------------------
   3. APLICAR O PERFIL ESCOLHIDO
   Esta função é a demonstração viva da US17: os itens de menu que o perfil
   não pode acessar não ficam desabilitados, eles somem da interface.
   ------------------------------------------------------------------------- */

function aplicarPerfil(perfil, menu, raiz) {
  var botoes = menu.querySelectorAll('button');

  for (var i = 0; i < botoes.length; i++) {
    var permitidos = botoes[i].getAttribute('data-perfis') || '';
    if (permitidos.indexOf(perfil) >= 0) {
      botoes[i].style.display = 'flex';
    } else {
      botoes[i].style.display = 'none';
    }
  }

  /* O item "Usuários e permissões" só existe para o Administrador (US16). */
  var somenteAdmin = raiz.querySelectorAll('[data-somente-admin]');
  for (var j = 0; j < somenteAdmin.length; j++) {
    somenteAdmin[j].style.display = (perfil === 'ADMINISTRADOR') ? 'flex' : 'none';
  }
}


/* -------------------------------------------------------------------------
   4. LIGAR OS CLIQUES
   ------------------------------------------------------------------------- */

/* Qualquer elemento com data-ir leva para a tela indicada. */
document.addEventListener('click', function (evento) {
  var alvo = evento.target.closest('[data-ir]');
  if (!alvo) return;

  /* Se o botão também define um perfil (os atalhos da tela de login),
     trocamos o perfil junto. */
  var perfilDoBotao = alvo.getAttribute('data-perfil');
  if (perfilDoBotao) {
    perfilAtual = perfilDoBotao;
    document.getElementById('seletor-perfil').value = perfilDoBotao;
    aplicarPerfil(perfilAtual, document.getElementById('menu-inferior'), document);
  }

  mostrarTela(alvo.getAttribute('data-ir'));
});

/* Os botões de filtro em segmentos: clicar marca só o que foi clicado. */
document.addEventListener('click', function (evento) {
  var botao = evento.target.closest('.segmentos button');
  if (!botao) return;

  var irmaos = botao.parentElement.querySelectorAll('button');
  for (var i = 0; i < irmaos.length; i++) {
    irmaos[i].classList.remove('ativo');
  }
  botao.classList.add('ativo');
});

/* O interruptor de tema escuro liga e desliga. */
document.addEventListener('click', function (evento) {
  var chave = evento.target.closest('.interruptor');
  if (!chave) return;
  chave.classList.toggle('ligado');
});

/* O seletor de perfil no topo da página. */
document.getElementById('seletor-perfil').addEventListener('change', function () {
  perfilAtual = this.value;
  aplicarPerfil(perfilAtual, document.getElementById('menu-inferior'), document);
  mostrarTela(TELA_INICIAL_DO_PERFIL[perfilAtual]);
  montarGaleria();
});

/* Os dois botões de modo de visualização. */
document.getElementById('botao-celular').addEventListener('click', function () {
  document.body.classList.remove('modo-galeria');
  this.classList.add('ativo');
  document.getElementById('botao-galeria').classList.remove('ativo');
});

document.getElementById('botao-galeria').addEventListener('click', function () {
  document.body.classList.add('modo-galeria');
  this.classList.add('ativo');
  document.getElementById('botao-celular').classList.remove('ativo');
  montarGaleria();
});


/* -------------------------------------------------------------------------
   5. MONTAR A GALERIA
   Copia o celular inteiro uma vez para cada tela, deixando só aquela tela
   dentro da cópia. É como conseguimos ver todas de uma vez sem repetir o
   HTML onze vezes.
   ------------------------------------------------------------------------- */

function montarGaleria() {
  var galeria = document.getElementById('galeria');
  galeria.innerHTML = '';

  for (var i = 0; i < ORDEM_DAS_TELAS.length; i++) {
    var nome = ORDEM_DAS_TELAS[i];
    var dados = TELAS[nome];

    /* Copia o celular inteiro */
    var copia = document.getElementById('celular-tela').cloneNode(true);
    copia.removeAttribute('id');

    /* Apaga da cópia todas as telas menos a que interessa */
    var telas = copia.querySelectorAll('.tela');
    for (var j = 0; j < telas.length; j++) {
      if (telas[j].getAttribute('data-tela') === nome) {
        telas[j].classList.add('ativa');
      } else {
        telas[j].remove();
      }
    }

    /* Ajusta o menu da cópia */
    var menu = copia.querySelector('.menu-inferior');
    menu.removeAttribute('id');
    menu.style.display = (nome === 'login') ? 'none' : 'flex';
    aplicarPerfil(perfilAtual, menu, copia);

    var aceso = dados.menu || nome;
    var botoes = menu.querySelectorAll('button');
    for (var k = 0; k < botoes.length; k++) {
      if (botoes[k].getAttribute('data-ir') === aceso) {
        botoes[k].classList.add('ativo');
      } else {
        botoes[k].classList.remove('ativo');
      }
    }

    /* Monta o bloco: título, descrição e o celular reduzido */
    var bloco = document.createElement('div');
    bloco.className = 'galeria-item';

    var titulo = document.createElement('h3');
    titulo.textContent = (i + 1) + '. ' + dados.titulo;

    var descricao = document.createElement('p');
    descricao.textContent = dados.us + ' — ' + dados.texto;

    var celular = document.createElement('div');
    celular.className = 'celular';
    celular.appendChild(copia);

    bloco.appendChild(titulo);
    bloco.appendChild(descricao);
    bloco.appendChild(celular);
    galeria.appendChild(bloco);
  }
}


/* -------------------------------------------------------------------------
   6. ESTADO INICIAL
   ------------------------------------------------------------------------- */

aplicarPerfil(perfilAtual, document.getElementById('menu-inferior'), document);
mostrarTela('login');


/* -------------------------------------------------------------------------
   7. MODO EXPORTAÇÃO
   Serve para gerar as imagens das telas para os slides.

   Abra com ?exportar=nome-da-tela para ver só aquela tela, ocupando a
   janela inteira, sem o cabeçalho e sem o painel de explicação.
   Exemplo:  wireframe.html?exportar=viagens&perfil=MOTORISTA
   ------------------------------------------------------------------------- */

(function preparaExportacao() {
  var parametros = new URLSearchParams(window.location.search);
  var telaPedida = parametros.get('exportar');
  if (!telaPedida) return;

  var perfilPedido = parametros.get('perfil');
  if (perfilPedido && TELA_INICIAL_DO_PERFIL[perfilPedido]) {
    perfilAtual = perfilPedido;
    document.getElementById('seletor-perfil').value = perfilPedido;
    aplicarPerfil(perfilAtual, document.getElementById('menu-inferior'), document);
  }

  document.body.classList.add('modo-exportar');
  mostrarTela(telaPedida);
})();
