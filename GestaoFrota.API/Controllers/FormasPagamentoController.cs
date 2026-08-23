using GestaoFrota.API.DTOs;
using GestaoFrota.API.Security;
using GestaoFrota.Domain.Entities;
using GestaoFrota.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFrota.API.Controllers;

/// <summary>
/// CRUD de formas de pagamento (Sprint 2). Leitura liberada para
/// todos os perfis autenticados; escrita restrita a Administrador e Financeiro.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Perfis.Todos)]
public class FormasPagamentoController : ControllerBase
{
    private readonly AppDbContext _context;

    public FormasPagamentoController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FormaPagamentoResponse>>> Listar([FromQuery] bool? ativo)
    {
        var query = _context.FormasPagamento.AsNoTracking().AsQueryable();

        if (ativo.HasValue)
        {
            query = query.Where(f => f.Ativo == ativo.Value);
        }

        var formas = await query
            .OrderBy(f => f.Nome)
            .Select(f => new FormaPagamentoResponse(f.Id, f.Nome, f.Ativo))
            .ToListAsync();

        return Ok(formas);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FormaPagamentoResponse>> ObterPorId(Guid id)
    {
        var forma = await _context.FormasPagamento.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);

        if (forma is null)
        {
            return NotFound(new { mensagem = "Forma de pagamento não encontrada." });
        }

        return Ok(new FormaPagamentoResponse(forma.Id, forma.Nome, forma.Ativo));
    }

    [HttpPost]
    [Authorize(Roles = Perfis.GestaoFinanceira)]
    public async Task<ActionResult<FormaPagamentoResponse>> Criar([FromBody] FormaPagamentoRequest request)
    {
        var existente = await _context.FormasPagamento.AnyAsync(f => f.Nome == request.Nome);
        if (existente)
        {
            return Conflict(new { mensagem = "Já existe uma forma de pagamento com esse nome." });
        }

        var forma = new FormaPagamento
        {
            Nome = request.Nome,
            Ativo = request.Ativo
        };

        _context.FormasPagamento.Add(forma);
        await _context.SaveChangesAsync();

        var response = new FormaPagamentoResponse(forma.Id, forma.Nome, forma.Ativo);
        return CreatedAtAction(nameof(ObterPorId), new { id = forma.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Perfis.GestaoFinanceira)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] FormaPagamentoRequest request)
    {
        var forma = await _context.FormasPagamento.FindAsync(id);
        if (forma is null)
        {
            return NotFound(new { mensagem = "Forma de pagamento não encontrada." });
        }

        var duplicada = await _context.FormasPagamento.AnyAsync(f => f.Id != id && f.Nome == request.Nome);
        if (duplicada)
        {
            return Conflict(new { mensagem = "Já existe uma forma de pagamento com esse nome." });
        }

        forma.Nome = request.Nome;
        forma.Ativo = request.Ativo;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Exclusão lógica (soft delete), preservando o histórico financeiro.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Desativar(Guid id)
    {
        var forma = await _context.FormasPagamento.FindAsync(id);
        if (forma is null)
        {
            return NotFound(new { mensagem = "Forma de pagamento não encontrada." });
        }

        forma.Ativo = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
