/* =========================================================================
   VIAGENS

   Consulta das viagens (GET /api/viagens). A API já devolve só as do
   próprio motorista quando quem pergunta é o Operador / Motorista.
   Abrir e encerrar viagem (com fila offline) é recurso do aplicativo.
   ========================================================================= */

if (iniciarTela('viagens.html')) {

  const lista = document.getElementById('lista');
  const aviso = document.getElementById('aviso');

  const SITUACAO = {
    1: ['Em andamento', 'etiqueta-alerta'],
    2: ['Concluída', 'etiqueta-positiva'],
    3: ['Cancelada', 'etiqueta-negativa']
  };

  let viagens = [];
  let filtro = 'todas';

  function desenhar() {
    aviso.hidden = true;

    const visiveis = viagens
      .filter(v => filtro === 'todas' || v.status === Number(filtro))
      .sort((a, b) => new Date(b.dataInicio) - new Date(a.dataInicio));

    if (visiveis.length === 0) {
      lista.innerHTML = '<li class="vazio"><strong>Nada por aqui</strong>Nenhuma viagem com esse filtro.</li>';
      return;
    }

    lista.innerHTML = visiveis.map(v => {
      const [rotulo, classe] = SITUACAO[v.status] || ['—', ''];
      const km = v.quilometragemRodadaKm != null ? numero(v.quilometragemRodadaKm) + ' km' : '—';

      return '<li class="lista-item">' +
        '<div class="lista-dados">' +
          '<div class="lista-titulo">' + texto(v.rotaOrigem) + ' → ' + texto(v.rotaDestino) + '</div>' +
          '<div class="lista-nota">' + texto(v.veiculoDescricao) + ' · ' + texto(v.veiculoPlaca) +
            ' · ' + texto(v.motoristaNome) + '</div>' +
          '<div class="lista-nota">' + dataCurta(v.dataInicio) + (v.dataFim ? ' → ' + dataCurta(v.dataFim) : '') +
            ' · hodômetro ' + numero(v.hodometroInicial) +
            (v.hodometroFinal != null ? ' → ' + numero(v.hodometroFinal) : '') + ' km ' +
            '<span class="etiqueta ' + classe + '">' + rotulo + '</span>' +
            (v.sincronizadoOffline ? ' <span class="etiqueta">aberta sem internet</span>' : '') +
          '</div>' +
        '</div>' +
        '<div class="lista-valor">' + km + '<small>rodados</small></div>' +
      '</li>';
    }).join('');
  }

  async function carregar() {
    lista.innerHTML = '<li class="vazio">Carregando...</li>';
    try {
      viagens = await Viagens.listar();
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
      filtro = botao.dataset.status;
      desenhar();
    });
  });

  carregar();
}
