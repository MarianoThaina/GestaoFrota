/* =========================================================================
   LAYOUT

   Monta a barra lateral uma vez só, aqui, em vez de repetir o mesmo bloco
   de HTML em cada arquivo de tela.

   O menu muda conforme o perfil do usuário, como exige a US16: o Operador /
   Motorista não vê Financeiro nem Relatórios — esses itens simplesmente não
   são criados para ele.

   Itens marcados com `pronta: false` não têm tela no site (a operação
   completa fica no aplicativo Android). Aparecem apagados, com um aviso.

   Telas no site: Relatórios, Movimentações e Viagens (consulta) e os
   cadastros de Categorias, Formas de pagamento e Usuários.
   ========================================================================= */

const ITENS_MENU = [
  {
    secao: 'Financeiro',
    itens: [
      { rotulo: 'Relatórios', arquivo: 'relatorios.html', icone: 'relatorios', pronta: true, perfis: [PERFIL.ADMINISTRADOR, PERFIL.FINANCEIRO, PERFIL.GESTOR_DE_FROTA] },
      { rotulo: 'Movimentações', arquivo: 'movimentacoes.html', icone: 'movimentacoes', pronta: true, perfis: [PERFIL.ADMINISTRADOR, PERFIL.FINANCEIRO, PERFIL.GESTOR_DE_FROTA] },
      { rotulo: 'Categorias', arquivo: 'categorias.html', icone: 'categorias', pronta: true, perfis: [PERFIL.ADMINISTRADOR, PERFIL.FINANCEIRO, PERFIL.GESTOR_DE_FROTA] },
      { rotulo: 'Formas de pagamento', arquivo: 'formas-pagamento.html', icone: 'pagamento', pronta: true, perfis: [PERFIL.ADMINISTRADOR, PERFIL.FINANCEIRO, PERFIL.GESTOR_DE_FROTA] }
    ]
  },
  {
    secao: 'Operação',
    itens: [
      // O protótipo mostra Frota no menu do motorista (imagem 16), então ele
      // entra aqui também.
      { rotulo: 'Frota', arquivo: 'frota.html', icone: 'frota', pronta: false, perfis: [PERFIL.ADMINISTRADOR, PERFIL.GESTOR_DE_FROTA, PERFIL.OPERADOR_MOTORISTA] },
      { rotulo: 'Viagens', arquivo: 'viagens.html', icone: 'viagens', pronta: true, perfis: [PERFIL.ADMINISTRADOR, PERFIL.GESTOR_DE_FROTA, PERFIL.OPERADOR_MOTORISTA] }
    ]
  },
  {
    secao: 'Administração',
    itens: [
      { rotulo: 'Usuários', arquivo: 'usuarios.html', icone: 'usuarios', pronta: true, perfis: [PERFIL.ADMINISTRADOR] },
      { rotulo: 'Ajustes', arquivo: 'ajustes.html', icone: 'ajustes', pronta: false, aviso: 'Em uma próxima versão', perfis: [PERFIL.ADMINISTRADOR, PERFIL.GESTOR_DE_FROTA, PERFIL.FINANCEIRO, PERFIL.OPERADOR_MOTORISTA] }
    ]
  }
];

// Ícones desenhados em SVG dentro do próprio arquivo, sem link externo.
// Assim o site continua funcionando sem internet, igual ao protótipo.
const ICONES = {
  relatorios: '<path d="M4 20V10M10 20V4M16 20v-7M22 20H2"/>',
  movimentacoes: '<path d="M3 6h18M3 12h12M3 18h6"/>',
  categorias: '<path d="M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>',
  pagamento: '<rect x="2" y="5" width="20" height="14" rx="2"/><path d="M2 10h20"/>',
  frota: '<path d="M3 16V7h11v9M14 10h4l3 3v3h-7"/><circle cx="7" cy="17" r="2"/><circle cx="17" cy="17" r="2"/>',
  viagens: '<circle cx="6" cy="6" r="2.5"/><circle cx="18" cy="18" r="2.5"/><path d="M8.5 6H15a3 3 0 0 1 0 6H9a3 3 0 0 0 0 6h6.5"/>',
  usuarios: '<circle cx="12" cy="8" r="3.5"/><path d="M5 20a7 7 0 0 1 14 0"/>',
  ajustes: '<circle cx="12" cy="12" r="3.5"/><path d="M12 2v3M12 19v3M2 12h3M19 12h3M5 5l2 2M17 17l2 2M19 5l-2 2M7 17l-2 2"/>',
  sair: '<path d="M14 4H6a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h8M17 8l4 4-4 4M21 12H10"/>',
  ajuda: '<circle cx="12" cy="12" r="9"/><path d="M9.5 9.5a2.5 2.5 0 1 1 3.5 2.3c-.6.3-1 .8-1 1.5V14M12 17.5h.01"/>'
};

// Central de Ajuda já filtrada para o perfil (mesma página que o app abre).
const AJUDA_POR_PERFIL = {
  [PERFIL.ADMINISTRADOR]: 'admin',
  [PERFIL.GESTOR_DE_FROTA]: 'gestor',
  [PERFIL.FINANCEIRO]: 'financeiro',
  [PERFIL.OPERADOR_MOTORISTA]: 'motorista'
};

function svg(nome) {
  return '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" ' +
    'stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">' +
    ICONES[nome] + '</svg>';
}

function iniciais(nome) {
  return nome
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map(parte => parte[0].toUpperCase())
    .join('');
}

function montarLateral(arquivoAtual) {
  const usuario = Sessao.usuario();
  const partes = [];

  partes.push(
    '<div class="marca">' +
      '<div class="marca-icone">' + svg('frota') + '</div>' +
      '<div class="marca-nome">Gestão de Frota<span>Financeiro e operação</span></div>' +
    '</div>'
  );

  partes.push('<nav class="menu">');

  for (const grupo of ITENS_MENU) {
    const visiveis = grupo.itens.filter(item => item.perfis.includes(usuario.perfil));
    if (visiveis.length === 0) continue;

    partes.push('<div class="menu-secao">' + grupo.secao + '</div>');

    for (const item of visiveis) {
      if (item.pronta) {
        const ativo = item.arquivo === arquivoAtual ? ' ativo' : '';
        partes.push(
          '<a class="menu-item' + ativo + '" href="' + item.arquivo + '">' +
            svg(item.icone) + item.rotulo +
          '</a>'
        );
      } else {
        partes.push(
          // Movimentações, Frota e Viagens já funcionam no aplicativo Android
          // (a API tem os endpoints); no site ficam só os cadastros e a administração.
          '<span class="menu-item menu-item-bloqueado" title="' + (item.aviso || 'Disponível no aplicativo Android') + '">' +
            svg(item.icone) + item.rotulo +
          '</span>'
        );
      }
    }
  }

  partes.push('</nav>');

  const avatar = usuario.fotoUrl
    ? '<img class="avatar" src="' + usuario.fotoUrl + '" alt="">'
    : '<div class="avatar">' + iniciais(usuario.nome) + '</div>';

  partes.push(
    '<div class="menu-rodape">' +
      '<div class="usuario">' + avatar +
        '<div class="usuario-dados">' +
          '<div class="usuario-nome">' + usuario.nome + '</div>' +
          '<div class="usuario-perfil">' + NOME_PERFIL[usuario.perfil] + '</div>' +
        '</div>' +
      '</div>' +
      '<a class="botao botao-texto" href="ajuda.html?perfil=' + (AJUDA_POR_PERFIL[usuario.perfil] || 'todos') + '" target="_blank" rel="noopener">' + svg('ajuda') + 'Ajuda</a>' +
      '<button class="botao botao-texto" id="botao-sair">' + svg('sair') + 'Sair</button>' +
    '</div>'
  );

  const lateral = document.querySelector('.lateral');
  lateral.innerHTML = partes.join('');
  document.getElementById('botao-sair').addEventListener('click', () => Sessao.sair());
}

// Formatação em pt-BR usada pelas telas de consulta.
const FORMATO_MOEDA = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const FORMATO_NUMERO = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 1 });

function moeda(valor) {
  return FORMATO_MOEDA.format(valor || 0);
}

function numero(valor) {
  return FORMATO_NUMERO.format(valor || 0);
}

function dataCurta(iso) {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString('pt-BR', { timeZone: 'UTC' });
}

// Texto vindo da API vai para o HTML: escapa antes de montar a lista.
function texto(valor) {
  const div = document.createElement('div');
  div.textContent = valor == null ? '' : String(valor);
  return div.innerHTML;
}

// Toda tela interna chama isto como primeira linha.
function iniciarTela(arquivoAtual) {
  if (!Sessao.exigirLogin()) return false;
  montarLateral(arquivoAtual);
  return true;
}
