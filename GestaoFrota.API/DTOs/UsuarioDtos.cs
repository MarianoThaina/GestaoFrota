using GestaoFrota.Domain.Enums;

namespace GestaoFrota.API.DTOs;

public record UsuarioResponse(
    Guid Id, string Nome, string Email, string? FotoUrl, PerfilUsuario Perfil, bool Ativo, DateTime? UltimoLogin,
    bool TemSenha, bool TemGoogle);

/// <summary>Conta criada pelo Administrador já com perfil e senha inicial.</summary>
public record CriarUsuarioRequest(string Nome, string Email, PerfilUsuario Perfil, string Senha);

/// <summary>Administrador define (ou redefine) a senha de uma conta.</summary>
public record DefinirSenhaRequest(string Senha);

public record AlterarPerfilRequest(PerfilUsuario Perfil);

public record AlterarStatusRequest(bool Ativo);
