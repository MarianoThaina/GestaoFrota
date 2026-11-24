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
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
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
    /// Seed de Administrador: e-mails listados em Seed:AdministradoresEmails
    /// (appsettings ou variável de ambiente Seed__AdministradoresEmails__0)
    /// viram Administrador no login. Evita o UPDATE manual no banco.
    /// </summary>
    private bool EhAdministradorSeed(string email)
    {
        var emails = _configuration.GetSection("Seed:AdministradoresEmails").Get<string[]>();
        return emails is not null && emails.Any(e =>
            !string.IsNullOrWhiteSpace(e) &&
            string.Equals(e.Trim(), email, StringComparison.OrdinalIgnoreCase));
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

        if (EhAdministradorSeed(usuario.Email) && usuario.Perfil != PerfilUsuario.Administrador)
        {
            usuario.Perfil = PerfilUsuario.Administrador;
        }

        await _context.SaveChangesAsync();

        var (token, expiraEm) = _jwtTokenService.GerarToken(usuario);

        return Ok(new AuthResponse(
            token, expiraEm, usuario.Id, usuario.Nome, usuario.Email, usuario.FotoUrl, usuario.Perfil));
    }

    /// <summary>Esquema que devolve o resultado ao app (WebAuthenticationCallbackActivity).</summary>
    private const string RetornoApp = "com.gestaofrota.app://oauth2redirect";

    /// <summary>
    /// Retorno do login Google do app Android. O cliente "Aplicativo da Web"
    /// do Google não aceita redirecionar para o esquema do app, então o Google
    /// devolve o código para cá; a API troca o código pelo id_token (com o
    /// Client Secret, que fica só no servidor) e redireciona o navegador para
    /// com.gestaofrota.app://oauth2redirect#id_token=..., que o
    /// WebAuthenticator do app recebe. Dali o app segue o fluxo de sempre:
    /// POST /api/auth/google com o id_token.
    /// </summary>
    [HttpGet("google/retorno")]
    public async Task<IActionResult> RetornoGoogle([FromQuery] string? code, [FromQuery] string? error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            // Ex.: access_denied quando a pessoa cancela ou não está na lista de testadores.
            return Redirect($"{RetornoApp}#error={Uri.EscapeDataString(error)}");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(new { mensagem = "Parâmetro code ausente. Esta rota é chamada pelo Google, não diretamente." });
        }

        try
        {
            var idToken = await _googleAuthService.TrocarCodigoPorIdTokenAsync(code);
            return Redirect($"{RetornoApp}#id_token={Uri.EscapeDataString(idToken)}");
        }
        catch (UnauthorizedAccessException)
        {
            return Redirect($"{RetornoApp}#error=codigo_recusado");
        }
        catch (InvalidOperationException)
        {
            // Client Secret ausente na hospedagem: devolve ao app em vez de um 500 no navegador.
            return Redirect($"{RetornoApp}#error=api_sem_client_secret");
        }
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
