using GestaoFrota.API.DTOs;
using GestaoFrota.API.Services;
using GestaoFrota.Domain.Entities;
using GestaoFrota.Domain.Enums;
using GestaoFrota.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFrota.API.Controllers;

/// <summary>
/// Autenticação via Google SSO (Sprint 1). O app mobile realiza o
/// Google Sign-In nativamente e envia o id_token para este endpoint,
/// que valida o token, cria/atualiza o usuário e devolve um JWT da API.
/// US16: quem entra pela primeira vez fica "Aguardando Aprovação" e só
/// recebe token depois que um Administrador libera o acesso.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    public const string MensagemAguardandoAprovacao =
        "Seu cadastro foi recebido e aguarda liberação por um administrador.";

    private readonly AppDbContext _context;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;

    public AuthController(
        AppDbContext context,
        IGoogleAuthService googleAuthService,
        IJwtTokenService jwtTokenService,
        IConfiguration configuration)
    {
        _context = context;
        _googleAuthService = googleAuthService;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
    }

    /// <summary>
    /// Login/cadastro via Google. Usuário novo é gravado como Motorista
    /// "Aguardando Aprovação" e recebe 403 até um Administrador liberar
    /// (PUT /api/usuarios/{id}/aprovar). Exceções que entram direto como
    /// Administrador ativo: o primeiro usuário da base (bootstrap) e os
    /// e-mails listados em Authentication:AdministradoresIniciais.
    /// </summary>
    [HttpPost("google")]
    public async Task<ActionResult<AuthResponse>> LoginGoogle([FromBody] GoogleLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            return BadRequest(new { mensagem = "IdToken é obrigatório." });
        }

        GoogleUserInfo googleUser;
        try
        {
            googleUser = await _googleAuthService.ValidarTokenAsync(request.IdToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { mensagem = ex.Message });
        }

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.GoogleId == googleUser.GoogleId);

        if (usuario is null)
        {
            var administradorInicial = await EhAdministradorInicialAsync(googleUser.Email);

            usuario = new Usuario
            {
                GoogleId = googleUser.GoogleId,
                Email = googleUser.Email,
                Nome = googleUser.Nome,
                FotoUrl = googleUser.FotoUrl,
                UltimoLogin = DateTime.UtcNow,
                Perfil = administradorInicial ? PerfilUsuario.Administrador : PerfilUsuario.OperadorMotorista,
                Situacao = administradorInicial ? SituacaoUsuario.Ativo : SituacaoUsuario.AguardandoAprovacao
            };
            _context.Usuarios.Add(usuario);
        }
        else
        {
            usuario.Nome = googleUser.Nome;
            usuario.FotoUrl = googleUser.FotoUrl;
            usuario.UltimoLogin = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        switch (usuario.Situacao)
        {
            case SituacaoUsuario.Inativo:
                return Unauthorized(new { mensagem = "Usuário desativado. Contate um administrador.", situacao = usuario.Situacao });

            case SituacaoUsuario.AguardandoAprovacao:
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    mensagem = MensagemAguardandoAprovacao,
                    situacao = usuario.Situacao
                });
        }

        var (token, expiraEm) = _jwtTokenService.GerarToken(usuario);

        return Ok(new AuthResponse(
            token, expiraEm, usuario.Id, usuario.Nome, usuario.Email, usuario.FotoUrl, usuario.Perfil));
    }

    /// <summary>
    /// Retorna os dados do usuário autenticado a partir do JWT — útil para
    /// o app mobile validar sessão/perfil sem precisar refazer login.
    /// Usuário que deixou de estar Ativo recebe 403 e o app encerra a sessão.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthResponse>> Me()
    {
        var usuarioId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (usuarioId is null || !Guid.TryParse(usuarioId, out var id))
        {
            return Unauthorized();
        }

        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound();
        }

        if (usuario.Situacao != SituacaoUsuario.Ativo)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                mensagem = usuario.Situacao == SituacaoUsuario.Inativo
                    ? "Usuário desativado. Contate um administrador."
                    : MensagemAguardandoAprovacao,
                situacao = usuario.Situacao
            });
        }

        return Ok(new AuthResponse(
            Token: string.Empty,
            ExpiraEm: default,
            usuario.Id, usuario.Nome, usuario.Email, usuario.FotoUrl, usuario.Perfil));
    }

    /// <summary>
    /// Bootstrap: sem isso ninguém conseguiria aprovar o primeiro Administrador.
    /// O primeiro usuário da base entra ativo como Administrador; o mesmo vale
    /// para os e-mails em Authentication:AdministradoresIniciais (appsettings
    /// ou variável Authentication__AdministradoresIniciais__0).
    /// </summary>
    private async Task<bool> EhAdministradorInicialAsync(string email)
    {
        if (!await _context.Usuarios.AnyAsync())
        {
            return true;
        }

        var iniciais = _configuration.GetSection("Authentication:AdministradoresIniciais").Get<string[]>()
                       ?? Array.Empty<string>();

        return iniciais.Any(e => string.Equals(e?.Trim(), email, StringComparison.OrdinalIgnoreCase));
    }
}
