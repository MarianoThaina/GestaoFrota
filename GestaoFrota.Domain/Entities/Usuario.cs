using GestaoFrota.Domain.Common;
using GestaoFrota.Domain.Enums;

namespace GestaoFrota.Domain.Entities;

/// <summary>
/// Usuário do sistema, autenticado via Google SSO.
/// O perfil (PerfilUsuario) define as permissões via RBAC; a situação
/// (SituacaoUsuario) controla se ele pode entrar (US16).
/// </summary>
public class Usuario : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Identificador único retornado pelo Google (sub do id_token).
    /// </summary>
    public string GoogleId { get; set; } = string.Empty;

    public string? FotoUrl { get; set; }

    public PerfilUsuario Perfil { get; set; } = PerfilUsuario.OperadorMotorista;

    /// <summary>
    /// Aguardando Aprovação (padrão no primeiro login), Ativo ou Inativo.
    /// Só "Ativo" recebe token.
    /// </summary>
    public SituacaoUsuario Situacao { get; set; } = SituacaoUsuario.AguardandoAprovacao;

    /// <summary>Atalho de leitura; não é coluna (ver UsuarioConfiguration).</summary>
    public bool Ativo => Situacao == SituacaoUsuario.Ativo;

    public DateTime? UltimoLogin { get; set; }

    // Navegação
    public ICollection<Viagem> Viagens { get; set; } = new List<Viagem>();
    public ICollection<Movimentacao> Movimentacoes { get; set; } = new List<Movimentacao>();
}
