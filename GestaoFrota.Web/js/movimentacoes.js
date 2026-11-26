/* =========================================================================
   MOVIMENTAÇÕES

   Consulta das receitas e despesas (GET /api/movimentacoes). Lançar,
   pagar, estornar e anexar comprovante continuam no aplicativo: o site
   mostra o que está registrado, com os mesmos dados.
   ========================================================================= */

if (iniciarTela('movimentacoes.html')) {

  const lista = document.getElementById('lista');
  const aviso = document.getElementById('aviso');
  const mostrarEstornadas = document.getElementById('mostrar-estornadas');

  const RECEITA = 1;
  const PAGO = 2;

  let movimentacoes = [];
  let filtro = 'todas';

  function situacao(m) {
    if (m.estornada) return '<span class="etiqueta">Estornada</span>';
    if (m.status === PAGO) return '<span class="etiqueta etiqueta-positiva">' + (m.tipo === RECEITA ? 'Recebido' : 'Pago') + '</span>';
    const vencida = new Date(m.dataVencimento) < new Date(new Date().toDateString());
    return '<span class="etiqueta ' + (vencida ? 'etiqueta-negativa' : 'etiqueta-alerta') + '">' +
      (vencida ? 'Em atraso' : 'Pendente') + '</span>';
  }

  function desenhar() {
    aviso.hidden = true;

    const visiveis = movimentacoes
      .filter(m => mostrarEstornadas.checked || !m.estornada)
      .filter(m => filtro === 'todas' ||
        (filtro === 'pagas' && m.status === PAGO && !m.estornada) ||
        (filtro === 'pendentes' && m.status !== PAGO && !m.estornada))
      .sort((a, b) => new Date(a.dataVencimento) - new Date(b.dataVencimento));

    if (visiveis.length === 0) {
      lista.innerHTML = '<li class="vazio"><strong>Nada por aqui</strong>Nenhuma movimentação com esse filtro.</li>';
      return;
    }

    lista.innerHTML = visiveis.map(m => {
      const entrada = m.tipo === RECEITA;
      const detalhes = [m.categoriaNome, m.formaPagamentoNome]
        .concat(m.quantidadeLitros ? [numero(m.quantidadeLitros) + ' L'] : [])
        .concat(m.viagemId ? ['vinculada a viagem'] : [])
        .concat(m.temComprovante ? ['com comprovante'] : [])
        .filter(Boolean).map(texto).join(' · ');

      return '<li class="lista-item' + (m.estornada ? ' lista-item-inativo' : '') + '">' +
        '<div class="lista-dados">' +
          '<div class="lista-titulo">' + texto(m.descricao || '(sem descrição)') + '</div>' +
          '<div class="lista-nota">' + detalhes + '</div>' +
          '<div class="lista-nota">Vence em ' + dataCurta(m.dataVencimento) +
            (m.dataPagamento ? ' · pago em ' + dataCurta(m.dataPagamento) : '') + ' ' + situacao(m) + '</div>' +
        '</div>' +
        '<div class="lista-valor ' + (entrada ? 'valor-positivo' : 'valor-negativo') + '">' +
          (entrada ? '+ ' : '− ') + moeda(m.valor) +
        '</div>' +
      '</li>';
    }).join('');
  }

  async function carregar() {
    lista.innerHTML = '<li class="vazio">Carregando...</li>';
    try {
      movimentacoes = await Movimentacoes.listar();
      desenhar();
    } catch (erro) {
      lista.innerHTML = '';
      aviso.className = 'aviso aviso-erro';
      aviso.textContent = erro.message;
      aviso.hidden = false;
    }
  }

  document.querySelectorAll('.segmento').forEach(botao => {
    botao.addEventListener('click', () => {
      document.querySelectorAll('.segmento').forEach(b => b.classList.remove('ativo'));
      botao.classList.add('ativo');
      filtro = botao.dataset.filtro;
      desenhar();
    });
  });

  mostrarEstornadas.addEventListener('change', desenhar);

  carregar();
}
