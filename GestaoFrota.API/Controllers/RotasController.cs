using GestaoFrota.API.DTOs;
using GestaoFrota.API.Security;
using GestaoFrota.Domain.Entities;
using GestaoFrota.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFrota.API.Controllers;

/// <summary>
/// Rotas cadastradas (US07): origem, destino e distância estimada, usadas ao
/// abrir viagens e nas estatísticas por rota (US10). Leitura para todos os
/// perfis; escrita para Administrador e Gestor de Frota; desativar só Administrador.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Perfis.Todos)]
public class RotasController : ControllerBase
{
    private readonly AppDbContext _context;

    public RotasController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RotaResponse>>> Listar([FromQuery] bool? ativa)
    {
        var query = _context.Rotas.AsNoTracking().AsQueryable();

        if (ativa.HasValue) query = query.Where(r => r.Ativa == ativa.Value);

        var rotas = await query.OrderBy(r => r.Nome).ToListAsync();
        return Ok(rotas.Select(RotaResponse.De));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RotaResponse>> ObterPorId(Guid id)
    {
        var rota = await _context.Rotas.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

        return rota is null
            ? NotFound(new { mensagem = "Rota não encontrada." })
            : Ok(RotaResponse.De(rota));
    }

    [HttpPost]
    [Authorize(Roles = Perfis.GestaoFrota)]
    public async Task<ActionResult<RotaResponse>> Criar([FromBody] RotaRequest request)
    {
        var nome = request.Nome.Trim();

        if (await _context.Rotas.AnyAsync(r => r.Nome == nome))
        {
            return Conflict(new { mensagem = "Já existe uma rota com esse nome." });
        }

        var rota = new Rota
        {
            Nome = nome,
            Origem = request.Origem.Trim(),
            Destino = request.Destino.Trim(),
            DistanciaEstimadaKm = Math.Round(request.DistanciaEstimadaKm, 2),
            Ativa = request.Ativa
        };

        _context.Rotas.Add(rota);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(ObterPorId), new { id = rota.Id }, RotaResponse.De(rota));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Perfis.GestaoFrota)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] RotaRequest request)
    {
        var rota = await _context.Rotas.FindAsync(id);
        if (rota is null)
        {
            return NotFound(new { mensagem = "Rota não encontrada." });
        }

        var nome = request.Nome.Trim();

        if (await _context.Rotas.AnyAsync(r => r.Id != id && r.Nome == nome))
        {
            return Conflict(new { mensagem = "Já existe outra rota com esse nome." });
        }

        rota.Nome = nome;
        rota.Origem = request.Origem.Trim();
        rota.Destino = request.Destino.Trim();
        rota.DistanciaEstimadaKm = Math.Round(request.DistanciaEstimadaKm, 2);
        rota.Ativa = request.Ativa;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Exclusão lógica: as viagens antigas continuam apontando para a rota.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Desativar(Guid id)
    {
        var rota = await _context.Rotas.FindAsync(id);
        if (rota is null)
        {
            return NotFound(new { mensagem = "Rota não encontrada." });
        }

        rota.Ativa = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
