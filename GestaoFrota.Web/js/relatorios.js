/* =========================================================================
   RELATÓRIOS

   Versão web, só de consulta, da tela de Relatórios do aplicativo:
   indicadores do período, entradas e saídas por mês, despesas por
   categoria, resumo da frota e exportação em PDF/CSV.

   Usa os mesmos endpoints do app (GET /api/relatorios/resumo, /frota e
   /exportar), então os números batem com o que aparece no celular.
   ========================================================================= */

if (iniciarTela('relatorios.html')) {

  const aviso = document.getElementById('aviso');
  const periodoRotulo = document.getElementById('periodo-rotulo');
  const indicadores = document.getElementById('indicadores');
  const grafico = document.getElementById('grafico');
  const categorias = document.getElementById('categorias');
  const frota = document.getElementById('frota');

  const MESES_CURTOS = ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez'];

  let periodo = { meses: 6 };

  function avisar(mensagem, estilo) {
    aviso.className = 'aviso aviso-' + estilo;
    aviso.textContent = mensagem;
    aviso.hidden = false;
  }

  function indicador(rotulo, valor, classe, destaque) {
    return '<div class="indicador' + (destaque ? ' indicador-destaque' : '') + '">' +
      '<div class="indicador-rotulo">' + rotulo + '</div>' +
      '<div class="indicador-valor ' + (classe || '') + '">' + valor + '</div>' +
    '</div>';
  }

  function desenharIndicadores(resumo) {
    const i = resumo.indicadores;
    periodoRotulo.textContent = 'Período: ' + resumo.periodoRotulo + ' · por vencimento · ' +
      i.quantidadeLancamentos + ' lançamento(s)';

    indicadores.innerHTML =
      indicador('Resultado do período (entradas − saídas)', moeda(i.resultado),
        i.resultado >= 0 ? 'valor-positivo' : 'valor-negativo', true) +
      indicador('Entradas no período', moeda(i.totalEntradas), 'valor-positivo') +
      indicador('Saídas no período', moeda(i.totalSaidas), 'valor-negativo') +
      indicador('A receber neste mês', moeda(i.aReceberNoMes)) +
      indicador('A pagar neste mês', moeda(i.aPagarNoMes)) +
      indicador('Pendentes em atraso', moeda(i.emAtraso), i.emAtraso > 0 ? 'valor-negativo' : '') +
      indicador('Maior despesa', i.maiorDespesaCategoria
        ? texto(i.maiorDespesaCategoria) + ' · ' + moeda(i.maiorDespesaValor)
        : 'Nenhuma despesa');
  }

  function desenharGrafico(fluxo) {
    if (!fluxo || fluxo.length === 0) {
      grafico.innerHTML = '<p class="vazio">Sem lançamentos no período.</p>';
      return;
    }

    const maior = Math.max(1, ...fluxo.map(m => Math.max(m.entradas, m.saidas)));
    const altura = v => Math.round((v / maior) * 100) + '%';

    grafico.innerHTML =
      '<div class="grafico-colunas">' +
        fluxo.map(m =>
          '<div class="grafico-mes" title="' + texto(m.rotulo) + ': entradas ' + moeda(m.entradas) +
            ', saídas ' + moeda(m.saidas) + '">' +
            '<div class="grafico-par">' +
              '<div class="grafico-barra grafico-barra-entrada" style="height:' + altura(m.entradas) + '"></div>' +
              '<div class="grafico-barra grafico-barra-saida" style="height:' + altura(m.saidas) + '"></div>' +
            '</div>' +
          '</div>').join('') +
      '</div>' +
      '<div class="grafico-rotulos">' + fluxo.map(m => '<span>' + MESES_CURTOS[m.mes - 1] + '</span>').join('') + '</div>' +
      '<div class="legenda">' +
        '<span><i style="background:var(--cor-positiva)"></i>Entradas</span>' +
        '<span><i style="background:var(--cor-negativa)"></i>Saídas</span>' +
      '</div>';
  }

  function desenharCategorias(lista) {
    if (!lista || lista.length === 0) {
      categorias.innerHTML = '<p class="vazio">Nenhuma despesa no período.</p>';
      return;
    }

    categorias.innerHTML = lista.map(c =>
      '<div class="barra-categoria">' +
        '<div class="barra-categoria-topo">' +
          '<span>' + texto(c.categoria) + '</span>' +
          '<span>' + moeda(c.total) + ' · ' + numero(c.percentual) + '%</span>' +
        '</div>' +
        '<div class="barra-categoria-trilho"><span style="width:' + Math.min(100, c.percentual) + '%"></span></div>' +
      '</div>').join('');
  }

  function desenharFrota(f) {
    const linhas = [
      ['Veículos em viagem agora', f.veiculosEmViagem + ' de ' + f.veiculosAtivos],
      ['Viagens no período', f.totalViagens + ' (' + f.viagensConcluidas + ' concluídas)'],
      ['Km rodados', numero(f.kmRodados) + ' km'],
      ['Rentabilidade das viagens', moeda(f.receitasVinculadas - f.despesasVinculadas)]
    ];

    let html = '<ul class="lista">' + linhas.map(([rotulo, valor]) =>
      '<li class="lista-item"><div class="lista-dados"><div class="lista-nota">' + rotulo +
      '</div></div><div class="lista-valor">' + valor + '</div></li>').join('') + '</ul>';

    const veiculo = (f.veiculosMaisEficientes || [])[0];
    if (veiculo) {
      html += '<p class="lista-nota" style="margin-top:12px">Veículo mais eficiente: <strong>' +
        texto(veiculo.descricao) + ' · ' + texto(veiculo.placaFormatada) + '</strong> — ' +
        (veiculo.custoPorKm != null ? moeda(veiculo.custoPorKm) + '/km' : 'sem custo vinculado') +
        (veiculo.kmPorLitro != null ? ' · ' + numero(veiculo.kmPorLitro) + ' km/L' : '') + '</p>';
    }

    frota.innerHTML = html;
  }

  async function carregar() {
    aviso.hidden = true;
    indicadores.innerHTML = '<p class="vazio">Carregando...</p>';
    grafico.innerHTML = '';
    categorias.innerHTML = '';
    frota.innerHTML = '';

    try {
      const [resumo, estatisticas] = await Promise.all([
        Relatorios.resumo(periodo),
        Relatorios.frota(periodo)
      ]);

      desenharIndicadores(resumo);
      desenharGrafico(resumo.fluxoMensal);
      desenharCategorias(resumo.despesasPorCategoria);
      desenharFrota(estatisticas);
    } catch (erro) {
      indicadores.innerHTML = '';
      avisar(erro.message, 'erro');
    }
  }

  document.querySelectorAll('.segmento').forEach(botao => {
    botao.addEventListener('click', () => {
      document.querySelectorAll('.segmento').forEach(b => b.classList.remove('ativo'));
      botao.classList.add('ativo');
      periodo = botao.dataset.tudo ? { tudo: true } : { meses: Number(botao.dataset.meses) };
      carregar();
    });
  });

  async function exportar(formato, botao) {
    const textoOriginal = botao.textContent;
    botao.disabled = true;
    botao.textContent = 'Gerando...';
    try {
      await Relatorios.baixar(periodo, formato);
    } catch (erro) {
      avisar(erro.message, 'erro');
    } finally {
      botao.disabled = false;
      botao.textContent = textoOriginal;
    }
  }

  const botaoPdf = document.getElementById('botao-pdf');
  const botaoCsv = document.getElementById('botao-csv');
  botaoPdf.addEventListener('click', () => exportar('pdf', botaoPdf));
  botaoCsv.addEventListener('click', () => exportar('csv', botaoCsv));

  carregar();
}
