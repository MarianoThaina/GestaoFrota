using GestaoFrota.Domain.Enums;

namespace GestaoFrota.API.DTOs;

/// <summary>
/// Enviado pelo app mobile após o login Google (Google Sign-In no cliente),
/// contendo o id_token emitido pelo Google.
/// </summary>
public record GoogleLoginRequest(string IdToken);

/// <summary>
/// Resposta com o token JWT da própria API, usado nas chamadas subsequentes.
/// </summary>
public record AuthResponse(
    string Token,
    DateTime ExpiraEm,
    Guid UsuarioId,
    string Nome,
    string Email,
    string? FotoUrl,
    PerfilUsuario Perfil);
