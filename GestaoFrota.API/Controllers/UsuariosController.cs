using GestaoFrota.API.DTOs;
using GestaoFrota.API.Security;
using GestaoFrota.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFrota.API.Controllers;

/// <summary>
/// Gestão de usuários e perfis de acesso (RBAC). Somente Administrador
/// pode listar todos os usuários, alterar perfis ou desativar contas.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Perfis.Administrador)]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsuariosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioResponse>>> Listar()
    {
        var usuarios = await _context.Usuarios
            .AsNoTracking()
            .OrderBy(u => u.Nome)
            .Select(u => new UsuarioResponse(
                u.Id, u.Nome, u.Email, u.FotoUrl, u.Perfil, u.Ativo, u.UltimoLogin))
            .ToListAsync();

        return Ok(usuarios);
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
