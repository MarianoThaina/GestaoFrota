using GestaoFrota.API.DTOs;
using GestaoFrota.API.Security;
using GestaoFrota.API.Services;
using GestaoFrota.Domain.Entities;
using GestaoFrota.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFrota.API.Controllers;

/// <summary>
/// Gestão de usuários e perfis de acesso (RBAC). Somente Administrador
/// pode listar todos os usuários, criar contas com senha, alterar perfis,
/// definir senhas ou desativar contas.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Perfis.Administrador)]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ISenhaService _senhaService;

    public UsuariosController(AppDbContext context, ISenhaService senhaService)
    {
        _context = context;
        _senhaService = senhaService;
    }

    private static UsuarioResponse ParaResposta(Usuario u) => new(
        u.Id, u.Nome, u.Email, u.FotoUrl, u.Perfil, u.Ativo, u.UltimoLogin,
        TemSenha: u.SenhaHash != null, TemGoogle: u.GoogleId != null);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioResponse>>> Listar()
    {
        var usuarios = await _context.Usuarios
            .AsNoTracking()
            .OrderBy(u => u.Nome)
            .Select(u => new UsuarioResponse(
                u.Id, u.Nome, u.Email, u.FotoUrl, u.Perfil, u.Ativo, u.UltimoLogin,
                u.SenhaHash != null, u.GoogleId != null))
            .ToListAsync();

        return Ok(usuarios);
    }

    /// <summary>
    /// Cria uma conta já com perfil e senha inicial (login por e-mail e senha).
    /// Se a pessoa entrar depois pelo Google com o mesmo e-mail, a conta é
    /// vinculada e mantém o perfil.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UsuarioResponse>> Criar([FromBody] CriarUsuarioRequest request)
    {
        var nome = request.Nome?.Trim();
        if (string.IsNullOrEmpty(nome) || nome.Length > 150)
        {
            return BadRequest(new { mensagem = "Informe o nome (até 150 caracteres)." });
        }

        var email = PoliticaSenha.NormalizarEmail(request.Email);
        if (email is null)
        {
            return BadRequest(new { mensagem = "Informe um e-mail válido." });
        }

        if (!Enum.IsDefined(request.Perfil))
        {
            return BadRequest(new { mensagem = "Perfil inválido." });
        }

        var erroSenha = PoliticaSenha.Validar(request.Senha);
        if (erroSenha is not null)
        {
            return BadRequest(new { mensagem = erroSenha });
        }

        if (await _context.Usuarios.AnyAsync(u => u.Email == email))
        {
            return Conflict(new { mensagem = "Já existe uma conta com este e-mail." });
        }

        var usuario = new Usuario
        {
            Nome = nome,
            Email = email,
            Perfil = request.Perfil
        };
        usuario.SenhaHash = _senhaService.GerarHash(usuario, request.Senha);

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(Listar), new { id = usuario.Id }, ParaResposta(usuario));
    }

    /// <summary>
    /// Define ou redefine a senha de qualquer conta (também de uma conta Google,
    /// que passa a entrar dos dois jeitos). Desfaz um bloqueio por tentativas.
    /// </summary>
    [HttpPut("{id:guid}/senha")]
    public async Task<IActionResult> DefinirSenha(Guid id, [FromBody] DefinirSenhaRequest request)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound(new { mensagem = "Usuário não encontrado." });
        }

        var erroSenha = PoliticaSenha.Validar(request.Senha);
        if (erroSenha is not null)
        {
            return BadRequest(new { mensagem = erroSenha });
        }

        usuario.SenhaHash = _senhaService.GerarHash(usuario, request.Senha);
        usuario.TentativasLoginFalhas = 0;
        usuario.BloqueadoAte = null;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:guid}/perfil")]
    public async Task<IActionResult> AlterarPerfil(Guid id, [FromBody] AlterarPerfilRequest request)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound(new { mensagem = "Usuário não encontrado." });
        }

        usuario.Perfil = request.Perfil;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> AlterarStatus(Guid id, [FromBody] AlterarStatusRequest request)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound(new { mensagem = "Usuário não encontrado." });
        }

        usuario.Ativo = request.Ativo;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
