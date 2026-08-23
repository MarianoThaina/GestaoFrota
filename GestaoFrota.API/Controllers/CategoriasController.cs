using GestaoFrota.API.DTOs;
using GestaoFrota.API.Security;
using GestaoFrota.Domain.Entities;
using GestaoFrota.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFrota.API.Controllers;

/// <summary>
/// CRUD de categorias financeiras (Sprint 2). Leitura liberada para
/// todos os perfis autenticados; escrita restrita a Administrador e Financeiro.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Perfis.Todos)]
public class CategoriasController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriasController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoriaResponse>>> Listar([FromQuery] bool? ativo)
    {
        var query = _context.Categorias.AsNoTracking().AsQueryable();

        if (ativo.HasValue)
        {
            query = query.Where(c => c.Ativo == ativo.Value);
        }

        var categorias = await query
            .OrderBy(c => c.Nome)
            .Select(c => new CategoriaResponse(c.Id, c.Nome, c.Tipo, c.Ativo))
            .ToListAsync();

        return Ok(categorias);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoriaResponse>> ObterPorId(Guid id)
    {
        var categoria = await _context.Categorias.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

        if (categoria is null)
        {
            return NotFound(new { mensagem = "Categoria não encontrada." });
        }

        return Ok(new CategoriaResponse(categoria.Id, categoria.Nome, categoria.Tipo, categoria.Ativo));
    }

    [HttpPost]
    [Authorize(Roles = Perfis.GestaoFinanceira)]
    public async Task<ActionResult<CategoriaResponse>> Criar([FromBody] CategoriaRequest request)
    {
        var existente = await _context.Categorias
            .AnyAsync(c => c.Nome == request.Nome && c.Tipo == request.Tipo);

        if (existente)
        {
            return Conflict(new { mensagem = "Já existe uma categoria com esse nome e tipo." });
        }

        var categoria = new Categoria
        {
            Nome = request.Nome,
            Tipo = request.Tipo,
            Ativo = request.Ativo
        };

        _context.Categorias.Add(categoria);
        await _context.SaveChangesAsync();

        var response = new CategoriaResponse(categoria.Id, categoria.Nome, categoria.Tipo, categoria.Ativo);
        return CreatedAtAction(nameof(ObterPorId), new { id = categoria.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Perfis.GestaoFinanceira)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] CategoriaRequest request)
    {
        var categoria = await _context.Categorias.FindAsync(id);
        if (categoria is null)
        {
            return NotFound(new { mensagem = "Categoria não encontrada." });
        }

        var duplicada = await _context.Categorias
            .AnyAsync(c => c.Id != id && c.Nome == request.Nome && c.Tipo == request.Tipo);

        if (duplicada)
        {
            return Conflict(new { mensagem = "Já existe uma categoria com esse nome e tipo." });
        }

        categoria.Nome = request.Nome;
        categoria.Tipo = request.Tipo;
        categoria.Ativo = request.Ativo;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Exclusão lógica (soft delete) — categorias com movimentações vinculadas
    /// não podem ser removidas fisicamente para preservar o histórico financeiro.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Desativar(Guid id)
    {
        var categoria = await _context.Categorias.FindAsync(id);
        if (categoria is null)
        {
            return NotFound(new { mensagem = "Categoria não encontrada." });
        }

        categoria.Ativo = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
