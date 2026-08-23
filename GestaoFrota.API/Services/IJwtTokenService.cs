using GestaoFrota.Domain.Entities;

namespace GestaoFrota.API.Services;

public interface IJwtTokenService
{
    /// <summary>
    /// Gera o JWT da API contendo claims de identidade e o perfil (role)
    /// do usuário, usado para autorização RBAC via [Authorize(Roles = ...)].
    /// </summary>
    (string Token, DateTime ExpiraEm) GerarToken(Usuario usuario);
}
