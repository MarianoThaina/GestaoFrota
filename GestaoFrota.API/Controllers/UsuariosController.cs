using System.Security.Claims;
using GestaoFrota.API.DTOs;
using GestaoFrota.API.Security;
using GestaoFrota.Domain.Enums;
using GestaoFrota.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFrota.API.Controllers;

/// <summary>
/// Gestão de usuários e perfis de acesso (RBAC, US16). Somente Administrador
/// pode listar, aprovar cadastros pendentes, alterar perfis e situação.
/// Um Administrador não consegue se desativar nem se rebaixar sozinho.
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

    /// <summary>Lista com os pendentes de aprovação primeiro; filtro opcional por situação.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioResponse>>> Listar([FromQuery] SituacaoUsuario? situacao)
    {
        var query = _context.Usuarios.AsNoTracking().AsQueryable();

        if (situacao.HasValue) query = query.Where(u => u.Situacao == situacao.Value);

        var usuarios = await query
            .OrderByDescending(u => u.Situacao == SituacaoUsuario.AguardandoAprovacao)
            .ThenBy(u => u.Nome)
            .Select(u => new UsuarioResponse(
                u.Id, u.Nome, u.Email, u.FotoUrl, u.Perfil, u.Situacao,
                u.Situacao == SituacaoUsuario.Ativo, u.UltimoLogin, u.DataCriacao))
            .ToListAsync();

        return Ok(usuarios);
    }

    /// <summary>Quantos cadastros aguardam liberação (para o aviso no app).</summary>
    [HttpGet("pendentes/quantidade")]
    public async Task<ActionResult<int>> QuantidadePendentes() =>
        Ok(await _context.Usuarios.CountAsync(u => u.Situacao == SituacaoUsuario.AguardandoAprovacao));

    [HttpPut("{id:guid}/perfil")]
    public async Task<IActionResult> AlterarPerfil(Guid id, [FromBody] AlterarPerfilRequest request)
    {
        if (!Enum.IsDefined(request.Perfil))
        {
            return BadRequest(new { mensagem = "Perfil inválido." });
        }

        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound(new { mensagem = "Usuário não encontrado." });
        }

        if (EhOProprioUsuario(id) && request.Perfil != PerfilUsuario.Administrador)
        {
            return Conflict(new { mensagem = "Você não pode remover o seu próprio perfil de Administrador." });
        }

        usuario.Perfil = request.Perfil;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Libera um cadastro "Aguardando Aprovação" (US16): vira Ativo e, se
    /// informado, já recebe o perfil definitivo.
    /// </summary>
    [HttpPut("{id:guid}/aprovar")]
    public async Task<IActionResult> Aprovar(Guid id, [FromBody] AprovarUsuarioRequest? request)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound(new { mensagem = "Usuário não encontrado." });
        }

        if (usuario.Situacao == SituacaoUsuario.Ativo)
        {
            return Conflict(new { mensagem = "Este usuário já está ativo." });
        }

        if (request?.Perfil is { } perfil)
        {
            if (!Enum.IsDefined(perfil))
            {
                return BadRequest(new { mensagem = "Perfil inválido." });
            }

            usuario.Perfil = perfil;
        }

        usuario.Situacao = SituacaoUsuario.Ativo;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Ativo, Inativo ou de volta para Aguardando Aprovação.</summary>
    [HttpPut("{id:guid}/situacao")]
    public async Task<IActionResult> AlterarSituacao(Guid id, [FromBody] AlterarSituacaoRequest request)
    {
        if (!Enum.IsDefined(request.Situacao))
        {
            return BadRequest(new { mensagem = "Situação inválida." });
        }

        return await DefinirSituacaoAsync(id, request.Situacao);
    }

    /// <summary>Compatibilidade: true = Ativo, false = Inativo.</summary>
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> AlterarStatus(Guid id, [FromBody] AlterarStatusRequest request) =>
        await DefinirSituacaoAsync(id, request.Ativo ? SituacaoUsuario.Ativo : SituacaoUsuario.Inativo);

    // ---------------- Auxiliares ----------------

    private async Task<IActionResult> DefinirSituacaoAsync(Guid id, SituacaoUsuario situacao)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound(new { mensagem = "Usuário não encontrado." });
        }

        if (EhOProprioUsuario(id) && situacao != SituacaoUsuario.Ativo)
        {
            return Conflict(new { mensagem = "Você não pode desativar o seu próprio acesso." });
        }

        usuario.Situacao = situacao;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool EhOProprioUsuario(Guid id)
    {
        var valor = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(valor, out var meuId) && meuId == id;
    }
}
