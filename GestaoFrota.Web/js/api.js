/* =========================================================================
   API

   Um invólucro fino em cima do fetch. Ele cuida de três coisas chatas que
   toda tela precisaria repetir:

     - anexar o cabeçalho Authorization com o JWT
     - transformar respostas de erro em uma mensagem legível em português
     - devolver null quando a resposta é 204 No Content (PUT e DELETE)

   Erros viram uma exceção ErroApi, para a tela poder usar try/catch.
   ========================================================================= */

class ErroApi extends Error {
  constructor(status, mensagem) {
    super(mensagem);
    this.status = status;
  }
}

const Api = {

  // `login: true` (só no login por senha): um 401 é "e-mail ou senha
  // incorretos", não sessão vencida — não apaga a sessão nem recarrega a página.
  async requisitar(caminho, { login = false, ...opcoes } = {}) {
    const cabecalhos = { 'Content-Type': 'application/json' };
    const token = Sessao.token();

    if (token) {
      cabecalhos['Authorization'] = 'Bearer ' + token;
    }

    let resposta;

    try {
      resposta = await fetch(CONFIG.API + caminho, {
        ...opcoes,
        headers: { ...cabecalhos, ...opcoes.headers }
      });
    } catch {
      throw new ErroApi(0, 'Não foi possível falar com o servidor. Verifique se a API está rodando em ' + CONFIG.API + '.');
    }

    if (resposta.status === 401 && !login) {
      Sessao.encerrar();
      window.location.href = 'index.html';
      throw new ErroApi(401, 'Sua sessão expirou. Entre de novo.');
    }

    if (resposta.status === 204) {
      return null;
    }

    const corpo = await this.lerCorpo(resposta);

    if (!resposta.ok) {
      throw new ErroApi(resposta.status, this.extrairMensagem(resposta.status, corpo));
    }

    return corpo;
  },

  async lerCorpo(resposta) {
    const texto = await resposta.text();
    if (!texto) return null;

    try {
      return JSON.parse(texto);
    } catch {
      return texto;
    }
  },

  // A API responde de três jeitos diferentes conforme o caso:
  //   { mensagem: "..." }                   nos NotFound e Conflict dos controllers
  //   { errors: { Campo: ["..."] } }        na validação automática do [ApiController]
  //   nada                                  no 403
  extrairMensagem(status, corpo) {
    if (corpo && corpo.mensagem) {
      return corpo.mensagem;
    }

    if (corpo && corpo.errors) {
      return Object.values(corpo.errors).flat().join(' ');
    }

    if (status === 403) {
      return 'Seu perfil não tem permissão para essa ação.';
    }

    return 'Não foi possível concluir a operação (erro ' + status + ').';
  },

  get(caminho) {
    return this.requisitar(caminho);
  },

  post(caminho, dados) {
    return this.requisitar(caminho, { method: 'POST', body: JSON.stringify(dados) });
  },

  put(caminho, dados) {
    return this.requisitar(caminho, { method: 'PUT', body: JSON.stringify(dados) });
  },

  delete(caminho) {
    return this.requisitar(caminho, { method: 'DELETE' });
  }
};


/* -------------------------------------------------------------------------
   Atalhos por recurso, com os caminhos que os controllers realmente expõem.
   ------------------------------------------------------------------------- */

const Categorias = {
  listar(ativo) {
    const filtro = ativo === undefined ? '' : '?ativo=' + ativo;
    return Api.get('/api/categorias' + filtro);
  },
  criar(categoria) {
    return Api.post('/api/categorias', categoria);
  },
  atualizar(id, categoria) {
    return Api.put('/api/categorias/' + id, categoria);
  },
  // O DELETE da API é soft delete: marca Ativo = false. Só Administrador.
  desativar(id) {
    return Api.delete('/api/categorias/' + id);
  }
};

const FormasPagamento = {
  listar(ativo) {
    const filtro = ativo === undefined ? '' : '?ativo=' + ativo;
    return Api.get('/api/formaspagamento' + filtro);
  },
  criar(forma) {
    return Api.post('/api/formaspagamento', forma);
  },
  atualizar(id, forma) {
    return Api.put('/api/formaspagamento/' + id, forma);
  },
  desativar(id) {
    return Api.delete('/api/formaspagamento/' + id);
  }
};

const Usuarios = {
  listar() {
    return Api.get('/api/usuarios');
  },
  alterarPerfil(id, perfil) {
    return Api.put('/api/usuarios/' + id + '/perfil', { perfil });
  },
  alterarStatus(id, ativo) {
    return Api.put('/api/usuarios/' + id + '/status', { ativo });
  },
  criar(nome, email, perfil, senha) {
    return Api.post('/api/usuarios', { nome, email, perfil, senha });
  },
  definirSenha(id, senha) {
    return Api.put('/api/usuarios/' + id + '/senha', { senha });
  }
};

// Período no formato que a API entende: { meses: 6 } ou { tudo: true }.
function consultaPeriodo(periodo) {
  const p = new URLSearchParams();
  if (periodo && periodo.tudo) p.set('tudo', 'true');
  else p.set('meses', String((periodo && periodo.meses) || 6));
  return p.toString();
}

const Relatorios = {
  resumo(periodo) {
    return Api.get('/api/relatorios/resumo?' + consultaPeriodo(periodo));
  },
  frota(periodo) {
    return Api.get('/api/relatorios/frota?' + consultaPeriodo(periodo));
  },
  // PDF e CSV vêm como arquivo e precisam do token, então não dá para usar
  // um <a href> simples: baixa com fetch e entrega ao navegador.
  async baixar(periodo, formato) {
    let resposta;
    try {
      resposta = await fetch(CONFIG.API + '/api/relatorios/exportar?' + consultaPeriodo(periodo) + '&formato=' + formato, {
        headers: { 'Authorization': 'Bearer ' + Sessao.token() }
      });
    } catch {
      throw new ErroApi(0, 'Não foi possível falar com o servidor.');
    }
    if (!resposta.ok) {
      throw new ErroApi(resposta.status, Api.extrairMensagem(resposta.status, await Api.lerCorpo(resposta)));
    }
    const nome = (resposta.headers.get('Content-Disposition') || '').match(/filename="?([^";]+)/);
    const url = URL.createObjectURL(await resposta.blob());
    const link = document.createElement('a');
    link.href = url;
    link.download = nome ? nome[1] : 'relatorio-financeiro.' + formato;
    document.body.appendChild(link);
    link.click();
    link.remove();
    setTimeout(() => URL.revokeObjectURL(url), 5000);
  }
};

const Movimentacoes = {
  listar() {
    return Api.get('/api/movimentacoes');
  }
};

const Viagens = {
  listar() {
    return Api.get('/api/viagens');
  }
};

const Auth = {
  entrarComGoogle(idToken) {
    return Api.post('/api/auth/google', { idToken });
  },
  entrarComSenha(email, senha) {
    return Api.requisitar('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, senha }),
      login: true
    });
  },
  meusDados() {
    return Api.get('/api/auth/me');
  }
};
