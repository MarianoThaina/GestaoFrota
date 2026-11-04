using GestaoFrota.API.DTOs;
using GestaoFrota.API.Services;
using GestaoFrota.Domain.Entities;
using GestaoFrota.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFrota.API.Controllers;

/// <summary>
/// Autenticação via Google SSO (Sprint 1). O app mobile realiza o
/// Google Sign-In nativamente e envia o id_token para este endpoint,
/// que valida o token, cria/atualiza o usuário e devolve um JWT da API.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(
        AppDbContext context,
        IGoogleAuthService googleAuthService,
        IJwtTokenService jwtTokenService)
    {
        _context = context;
        _googleAuthService = googleAuthService;
        _jwtTokenService = jwtTokenService;
    }

    /// <summary>
    /// Login/cadastro automático via Google. Novos usuários entram
    /// com o perfil padrão OperadorMotorista; a promoção de perfil
    /// (Administrador, Gestor de Frota, Financeiro) é feita por um
    /// Administrador via PUT /api/usuarios/{id}/perfil.
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
            usuario = new Usuario
            {
                GoogleId = googleUser.GoogleId,
                Email = googleUser.Email,
                Nome = googleUser.Nome,
                FotoUrl = googleUser.FotoUrl,
                UltimoLogin = DateTime.UtcNow
            };
            _context.Usuarios.Add(usuario);
        }
        else
        {
            if (!usuario.Ativo)
            {
                return Unauthorized(new { mensagem = "Usuário desativado. Contate um administrador." });
            }

            usuario.Nome = googleUser.Nome;
            usuario.FotoUrl = googleUser.FotoUrl;
            usuario.UltimoLogin = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        var (token, expiraEm) = _jwtTokenService.GerarToken(usuario);

        return Ok(new AuthResponse(
            token, expiraEm, usuario.Id, usuario.Nome, usuario.Email, usuario.FotoUrl, usuario.Perfil));
    }

    /// <summary>
    /// Retorna os dados do usuário autenticado a partir do JWT — útil para
    /// o app mobile validar sessão/perfil sem precisar refazer login.
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

        return Ok(new AuthResponse(
            Token: string.Empty,
            ExpiraEm: default,
            usuario.Id, usuario.Nome, usuario.Email, usuario.FotoUrl, usuario.Perfil));
    }
}
