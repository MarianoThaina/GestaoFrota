/**
 * Gerador das variações do protótipo.
 *
 * A fonte é sempre index.html + css/estilo.css + js/prototipo.js.
 * As variações são só folhas de estilo empilhadas por cima:
 *
 *   wireframe        = estilo + wireframe
 *   web              = estilo + web
 *   web-wireframe    = estilo + wireframe + web
 *
 * Cada variação sai em duas formas: com arquivos separados (para editar) e
 * em arquivo único (para apresentar e compartilhar).
 *
 * Rode com:  node gerar-versoes.cjs
 */

const fs = require('fs');
const path = require('path');

const raiz = __dirname;
const ler = (p) => fs.readFileSync(path.join(raiz, p), 'utf8');

const html = ler('index.html');
const cssBase = ler('css/estilo.css');
const cssWireframe = ler('css/wireframe.css');
const cssWeb = ler('css/web.css');
const js = ler('js/prototipo.js');

const LINK_CSS = '<link rel="stylesheet" href="css/estilo.css">';
const TAG_JS = '<script src="js/prototipo.js"></script>';

/* Bloco da marca e do usuário, que só existem no layout de barra lateral. */
const MARCA_LATERAL = `        <div class="marca-lateral">
          <span class="marca-icone">
            <svg class="icone icone-pequeno"><use href="#i-caminhao"></use></svg>
          </span>
          <span>
            <strong>Gestão de Frota</strong>
            <small>TransLog Transportes</small>
          </span>
        </div>

`;

const USUARIO_LATERAL = `
        <div class="usuario-lateral">
          <span class="usuario-inicial">AR</span>
          <div>
            Ana Ribeiro
            <small>Administrador</small>
          </div>
        </div>
`;

const FAIXA_WIREFRAME = `</header>

<div class="faixa-wireframe">
  <strong>Wireframe de média fidelidade.</strong>
  Escala de cinza proposital: nesta fase discutimos estrutura, hierarquia e fluxo.
  A paleta definitiva entra no guia de estilos.
</div>`;

/**
 * Monta uma variação.
 *  opcoes.wireframe -> aplica a folha de escala de cinza
 *  opcoes.web       -> aplica o layout de barra lateral
 */
function montar(opcoes) {
  let s = html;

  // --- folhas de estilo, na ordem em que precisam ser aplicadas ---
  const folhas = [LINK_CSS];
  if (opcoes.wireframe) folhas.push('<link rel="stylesheet" href="css/wireframe.css">');
  if (opcoes.web) folhas.push('<link rel="stylesheet" href="css/web.css">');
  s = s.replace(LINK_CSS, folhas.join('\n'));

  // --- textos do cabeçalho ---
  const rotulo = opcoes.wireframe ? 'Wireframe de média fidelidade' : 'Protótipo de telas';
  const meio = opcoes.web ? 'sistema web' : 'aplicativo mobile';

  s = s.replace(
    '<title>Protótipo — Gestão Financeira e Controle de Frota</title>',
    `<title>${rotulo} — Gestão Financeira e Controle de Frota</title>`,
  );
  s = s.replace(
    'Protótipo de telas &middot; Sprint 1 &middot; aplicativo mobile para empresas do ramo logístico',
    `${rotulo} &middot; Entrega 1 &middot; ${meio} para empresas do ramo logístico`,
  );

  if (opcoes.wireframe) s = s.replace('</header>', FAIXA_WIREFRAME);

  // --- barra lateral: marca no topo, usuário no rodapé ---
  if (opcoes.web) {
    const aberturaMenu = /<nav class="menu-inferior" id="menu-inferior">\s*/;
    if (!aberturaMenu.test(s)) throw new Error('abertura do menu nao encontrada');
    s = s.replace(aberturaMenu, (m) => m + MARCA_LATERAL);

    const fechamentoMenu = '      </nav>';
    if (!s.includes(fechamentoMenu)) throw new Error('fechamento do menu nao encontrado');
    s = s.replace(fechamentoMenu, USUARIO_LATERAL + fechamentoMenu);
  }

  return s;
}

/** Transforma a variação num arquivo único, sem nenhuma dependência externa. */
function embutir(s, opcoes) {
  s = s.replace(LINK_CSS, '<style>\n' + cssBase + '\n</style>');
  if (opcoes.wireframe) {
    s = s.replace('<link rel="stylesheet" href="css/wireframe.css">', '<style>\n' + cssWireframe + '\n</style>');
  }
  if (opcoes.web) {
    s = s.replace('<link rel="stylesheet" href="css/web.css">', '<style>\n' + cssWeb + '\n</style>');
  }
  s = s.replace(TAG_JS, '<script>\n' + js + '\n</script>');
  return s;
}

/** Confere que a saída está inteira antes de gravar. */
function conferir(nome, s, opcoes, unico) {
  const erros = [];

  if (unico) {
    if (/href=["']css\//.test(s)) erros.push('sobrou link para css externo');
    if (/src=["']js\//.test(s)) erros.push('sobrou script externo');
    if (!s.includes('.barra-topo {')) erros.push('css base nao entrou');
    if (opcoes.wireframe && !s.includes('MODO WIREFRAME')) erros.push('css de wireframe nao entrou');
    if (opcoes.web && !s.includes('MODO SISTEMA WEB')) erros.push('css de web nao entrou');
    if (!s.includes('function montarGaleria')) erros.push('js nao entrou');
  }

  // Conta as SEÇÕES de tela. Não vale contar "data-tela=" solto, porque o CSS
  // embutido também usa esse atributo nos seletores e inflaria o número.
  const telas = (s.match(/<section class="tela/g) || []).length;
  if (telas !== 15) erros.push('esperava 15 telas, achei ' + telas);
  if (opcoes.web && !s.includes('marca-lateral')) erros.push('marca da barra lateral nao entrou');
  if (opcoes.web && !s.includes('usuario-lateral')) erros.push('rodape da barra lateral nao entrou');

  if (erros.length) {
    console.error('FALHOU ' + nome + ': ' + erros.join('; '));
    process.exitCode = 1;
    return false;
  }
  return true;
}

function gravar(nome, s) {
  fs.writeFileSync(path.join(raiz, nome), s);
  const kb = (fs.statSync(path.join(raiz, nome)).size / 1024).toFixed(0);
  console.log('  ' + nome.padEnd(38) + kb + ' KB');
}

const VARIACOES = [
  { arquivo: 'wireframe',     opcoes: { wireframe: true,  web: false } },
  { arquivo: 'web',           opcoes: { wireframe: false, web: true  } },
  { arquivo: 'web-wireframe', opcoes: { wireframe: true,  web: true  } },
];

console.log('Gerando variacoes a partir de index.html:\n');

for (const v of VARIACOES) {
  const montada = montar(v.opcoes);
  if (conferir(v.arquivo + '.html', montada, v.opcoes, false)) {
    gravar(v.arquivo + '.html', montada);
  }

  const unica = embutir(montada, v.opcoes);
  const nomeUnico = v.arquivo + '-arquivo-unico.html';
  if (conferir(nomeUnico, unica, v.opcoes, true)) {
    gravar(nomeUnico, unica);
  }
}

// O protótipo original (mobile colorido) em arquivo único
const original = embutir(html, {});
if (conferir('prototipo-arquivo-unico.html', original, {}, true)) {
  gravar('prototipo-arquivo-unico.html', original);
}

console.log('\nPronto.');
