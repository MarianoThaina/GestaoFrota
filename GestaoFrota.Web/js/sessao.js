/* =========================================================================
   SESSÃO

   Guarda o JWT e os dados do usuário no navegador, e responde as perguntas
   de permissão que as telas fazem.

   As regras de permissão aqui são um espelho das que a API aplica nos
   atributos [Authorize(Roles = ...)]. Elas servem para esconder botões que
   o usuário não pode usar — quem realmente decide é o servidor.
   ========================================================================= */

const CHAVE = 'gestaofrota.sessao';

const Sessao = {

  salvar(resposta) {
    localStorage.setItem(CHAVE, JSON.stringify(resposta));
  },

  ler() {
    const bruto = localStorage.getItem(CHAVE);
    return bruto ? JSON.parse(bruto) : null;
  },

  token() {
    const sessao = this.ler();
    return sessao ? sessao.token : null;
  },

  usuario() {
    const sessao = this.ler();
    if (!sessao) return null;

    return {
      id: sessao.usuarioId,
      nome: sessao.nome,
      email: sessao.email,
      fotoUrl: sessao.fotoUrl,
      perfil: sessao.perfil
    };
  },

  expirada() {
    const sessao = this.ler();
    if (!sessao) return true;
    return new Date(sessao.expiraEm) <= new Date();
  },

  encerrar() {
    localStorage.removeItem(CHAVE);
  },

  sair() {
    this.encerrar();
    window.location.href = 'index.html';
  },

  // Chamado no início de toda tela interna. Se não há sessão válida,
  // manda para o login e avisa quem chamou para não continuar.
  exigirLogin() {
    if (this.expirada()) {
      this.encerrar();
      window.location.href = 'index.html';
      return false;
    }
    return true;
  },

  temPerfil(...perfisAceitos) {
    const usuario = this.usuario();
    return usuario ? perfisAceitos.includes(usuario.perfil) : false;
  },

  // Espelha Perfis.GestaoFinanceira — criar e editar categorias e formas
  // de pagamento.
  podeGerenciarFinanceiro() {
    return this.temPerfil(PERFIL.ADMINISTRADOR, PERFIL.FINANCEIRO);
  },

  // Espelha Perfis.GestaoFrota.
  podeGerenciarFrota() {
    return this.temPerfil(PERFIL.ADMINISTRADOR, PERFIL.GESTOR_DE_FROTA);
  },

  ehAdministrador() {
    return this.temPerfil(PERFIL.ADMINISTRADOR);
  }
};
