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

  async requisitar(caminho, opcoes = {}) {
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

    if (resposta.status === 401) {
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
  }
};

const Auth = {
  entrarComGoogle(idToken) {
    return Api.post('/api/auth/google', { idToken });
  },
  meusDados() {
    return Api.get('/api/auth/me');
  }
};
