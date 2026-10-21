using GestaoFrota.Domain.Enums;

namespace GestaoFrota.API.DTOs;

public record UsuarioResponse(
    Guid Id,
    string Nome,
    string Email,
    string? FotoUrl,
    PerfilUsuario Perfil,
    SituacaoUsuario Situacao,
    bool Ativo,
    DateTime? UltimoLogin,
    DateTime DataCriacao);

public record AlterarPerfilRequest(PerfilUsuario Perfil);

/// <summary>Compatibilidade com o app antigo: true = Ativo, false = Inativo.</summary>
public record AlterarStatusRequest(bool Ativo);

public record AlterarSituacaoRequest(SituacaoUsuario Situacao);

/// <summary>Libera um cadastro pendente; o perfil é opcional (mantém o atual se omitido).</summary>
public record AprovarUsuarioRequest(PerfilUsuario? Perfil);
