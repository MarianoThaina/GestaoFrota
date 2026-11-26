/* =========================================================================
   CENTRAL DE AJUDA

   Página pública (sem login). Dois filtros que se somam:
     - busca por texto: esconde assuntos que não têm a palavra e abre as
       perguntas de "Problemas comuns" que batem;
     - perfil ("Sou:"): mostra só os assuntos marcados com data-perfis desse
       perfil. Assunto sem data-perfis vale para todos.

   A busca ignora acentos e maiúsculas ("manutencao" acha "Manutenção").
   ========================================================================= */

(function () {
  const busca = document.getElementById('busca');
  const nada = document.getElementById('nada');
  const topicos = Array.from(document.querySelectorAll('.topico'));
  const botoesPerfil = Array.from(document.querySelectorAll('.ajuda-perfil'));

  let perfil = 'todos';

  function normalizar(texto) {
    return (texto || '').normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
  }

  // Texto de cada assunto calculado uma vez (inclui os textos alternativos das imagens).
  const indice = topicos.map(t => ({
    elemento: t,
    texto: normalizar(t.textContent + ' ' + Array.from(t.querySelectorAll('img')).map(i => i.alt).join(' ')),
    perfis: (t.dataset.perfis || '').split(/\s+/).filter(Boolean)
  }));

  function aplicar() {
    const termos = normalizar(busca.value).split(/\s+/).filter(Boolean);
    let algum = false;

    for (const item of indice) {
      const doPerfil = perfil === 'todos' || item.perfis.length === 0 || item.perfis.includes(perfil);
      const daBusca = termos.every(t => item.texto.includes(t));
      const visivel = doPerfil && daBusca;
      item.elemento.hidden = !visivel;
      if (visivel) algum = true;

      // Em "Problemas comuns", abre só as perguntas que batem com a busca.
      item.elemento.querySelectorAll('details').forEach(d => {
        const bate = termos.length > 0 && termos.every(t => normalizar(d.textContent).includes(t));
        d.open = bate;
        d.hidden = termos.length > 0 && !bate;
      });
    }

    nada.hidden = algum;
  }

  busca.addEventListener('input', aplicar);

  botoesPerfil.forEach(botao => {
    botao.addEventListener('click', () => {
      botoesPerfil.forEach(b => b.classList.remove('ativo'));
      botao.classList.add('ativo');
      perfil = botao.dataset.perfil;
      aplicar();
    });
  });

  // Link direto para um perfil: ajuda.html?perfil=motorista
  const pedido = new URLSearchParams(location.search).get('perfil');
  const botaoPedido = botoesPerfil.find(b => b.dataset.perfil === pedido);
  if (botaoPedido) botaoPedido.click();
})();
