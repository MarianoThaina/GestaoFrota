using GestaoFrota.API.DTOs;
using GestaoFrota.API.Security;
using GestaoFrota.Domain.Entities;
using GestaoFrota.Domain.Enums;
using GestaoFrota.Domain.Services;
using GestaoFrota.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFrota.API.Controllers;

/// <summary>
/// Cadastro de frota (US06). Leitura liberada para todos os perfis (o
/// Motorista precisa escolher o veículo ao abrir uma viagem); escrita
/// restrita a Administrador e Gestor de Frota; desativar só Administrador.
/// A placa é validada (antiga ou Mercosul), normalizada e única.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Perfis.Todos)]
public class VeiculosController : ControllerBase
{
    private readonly AppDbContext _context;

    public VeiculosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VeiculoResponse>>> Listar(
        [FromQuery] StatusVeiculo? status,
        [FromQuery] bool? ativo)
    {
        var query = _context.Veiculos.AsNoTracking().AsQueryable();

        if (status.HasValue) query = query.Where(v => v.Status == status.Value);
        if (ativo.HasValue) query = query.Where(v => v.Ativo == ativo.Value);

        var veiculos = await query
            .OrderBy(v => v.Marca)
            .ThenBy(v => v.Modelo)
            .ThenBy(v => v.Placa)
            .ToListAsync();

        return Ok(veiculos.Select(VeiculoResponse.De));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VeiculoResponse>> ObterPorId(Guid id)
    {
        var veiculo = await _context.Veiculos.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);

        return veiculo is null
            ? NotFound(new { mensagem = "Veículo não encontrado." })
            : Ok(VeiculoResponse.De(veiculo));
    }

    [HttpPost]
    [Authorize(Roles = Perfis.GestaoFrota)]
    public async Task<ActionResult<VeiculoResponse>> Criar([FromBody] VeiculoRequest request)
    {
        var placa = ValidadorPlaca.Normalizar(request.Placa);

        if (await _context.Veiculos.AnyAsync(v => v.Placa == placa))
        {
            return Conflict(new { mensagem = $"Já existe um veículo com a placa {ValidadorPlaca.Formatar(placa)}." });
        }

        var veiculo = new Veiculo
        {
            Placa = placa,
            Marca = request.Marca.Trim(),
            Modelo = request.Modelo.Trim(),
            Ano = request.Ano,
            CapacidadeCargaToneladas = Math.Round(request.CapacidadeCargaToneladas, 2),
            KmAtual = Math.Round(request.KmAtual, 2),
            Status = StatusVeiculo.Disponivel,
            Ativo = request.Ativo
        };

        _context.Veiculos.Add(veiculo);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(ObterPorId), new { id = veiculo.Id }, VeiculoResponse.De(veiculo));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Perfis.GestaoFrota)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] VeiculoRequest request)
    {
        var veiculo = await _context.Veiculos.FindAsync(id);
        if (veiculo is null)
        {
            return NotFound(new { mensagem = "Veículo não encontrado." });
        }

        var placa = ValidadorPlaca.Normalizar(request.Placa);

        if (await _context.Veiculos.AnyAsync(v => v.Id != id && v.Placa == placa))
        {
            return Conflict(new { mensagem = $"Já existe outro veículo com a placa {ValidadorPlaca.Formatar(placa)}." });
        }

        // O hodômetro não pode voltar para trás de uma viagem já encerrada.
        var maiorHodometro = await _context.Viagens
            .Where(v => v.VeiculoId == id && v.HodometroFinal != null)
            .MaxAsync(v => (decimal?)v.HodometroFinal) ?? 0m;

        if (request.KmAtual < maiorHodometro)
        {
            return BadRequest(new
            {
                mensagem = $"O hodômetro atual não pode ser menor que o último registrado em viagem ({maiorHodometro:N0} km)."
            });
        }

        veiculo.Placa = placa;
        veiculo.Marca = request.Marca.Trim();
        veiculo.Modelo = request.Modelo.Trim();
        veiculo.Ano = request.Ano;
        veiculo.CapacidadeCargaToneladas = Math.Round(request.CapacidadeCargaToneladas, 2);
        veiculo.KmAtual = Math.Round(request.KmAtual, 2);
        veiculo.Ativo = request.Ativo;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Exclusão lógica: veículos com viagens e manutenções no histórico não
    /// podem ser removidos fisicamente (FK Restrict). Um veículo em viagem
    /// não pode ser desativado.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Perfis.Administrador)]
    public async Task<IActionResult> Desativar(Guid id)
    {
        var veiculo = await _context.Veiculos.FindAsync(id);
        if (veiculo is null)
        {
            return NotFound(new { mensagem = "Veículo não encontrado." });
        }

        if (veiculo.Status == StatusVeiculo.EmViagem)
        {
            return Conflict(new { mensagem = "Encerre a viagem em andamento antes de desativar o veículo." });
        }

        veiculo.Ativo = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
