using GestaoFrota.Domain.Common;
using GestaoFrota.Domain.Enums;

namespace GestaoFrota.Domain.Entities;

/// <summary>
/// Usuário do sistema. Entra pelo Google SSO, por e-mail e senha, ou pelos dois.
/// O perfil (PerfilUsuario) define as permissões via RBAC.
/// </summary>
public class Usuario : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Identificador único retornado pelo Google (sub do id_token). Nulo na conta
    /// criada pelo Administrador com senha, até a pessoa entrar pelo Google com o
    /// mesmo e-mail (aí a conta é vinculada e mantém o perfil).
    /// </summary>
    public string? GoogleId { get; set; }

    /// <summary>
    /// Hash da senha (PBKDF2, PasswordHasher do ASP.NET Core). Nulo em conta só
    /// Google. A senha em si nunca é gravada.
    /// </summary>
    public string? SenhaHash { get; set; }

    /// <summary>Erros seguidos de senha; zera no login certo.</summary>
    public int TentativasLoginFalhas { get; set; }

    /// <summary>Login por senha bloqueado até esta data (UTC) depois de erros demais.</summary>
    public DateTime? BloqueadoAte { get; set; }

    public string? FotoUrl { get; set; }

    public PerfilUsuario Perfil { get; set; } = PerfilUsuario.OperadorMotorista;

    public bool Ativo { get; set; } = true;

    public DateTime? UltimoLogin { get; set; }

    // Navegação
    public ICollection<Viagem> Viagens { get; set; } = new List<Viagem>();
    public ICollection<Movimentacao> Movimentacoes { get; set; } = new List<Movimentacao>();
}
