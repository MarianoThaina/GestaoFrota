using GestaoFrota.Domain.Enums;

namespace GestaoFrota.API.DTOs;

public record UsuarioResponse(
    Guid Id, string Nome, string Email, string? FotoUrl, PerfilUsuario Perfil, bool Ativo, DateTime? UltimoLogin);

public record AlterarPerfilRequest(PerfilUsuario Perfil);

public record AlterarStatusRequest(bool Ativo);
