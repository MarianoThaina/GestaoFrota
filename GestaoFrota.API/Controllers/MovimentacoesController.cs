using System.Linq.Expressions;
using System.Security.Claims;
using GestaoFrota.API.DTOs;
using GestaoFrota.API.Security;
using GestaoFrota.API.Services;
using GestaoFrota.Domain.Entities;
using GestaoFrota.Domain.Enums;
using GestaoFrota.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFrota.API.Controllers;

/// <summary>
/// Movimentações financeiras (US03): receitas e despesas com status
/// Pendente/Pago, data de vencimento, data de pagamento e comprovante em imagem.
/// Leitura: Administrador, GestorDeFrota e Financeiro. Escrita: Administrador e
/// Financeiro. O OperadorMotorista não acessa nenhum endpoint deste controller.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Perfis.LeituraFinanceira)]
public class MovimentacoesController : ControllerBase
{
    private const long TamanhoMaximoComprovante = 5 * 1024 * 1024;

    private readonly AppDbContext _context;

    public MovimentacoesController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Projeção direto para o DTO: evita carregar os bytes do comprovante
    /// (varbinary(max)) em listagens.
    /// </summary>
    private static readonly Expression<Func<Movimentacao, MovimentacaoResponse>> Projecao = m =>
        new MovimentacaoResponse(
            m.Id, m.Tipo, m.Valor, m.Descricao, m.Status, m.Data, m.DataVencimento, m.DataPagamento,
            m.CategoriaId, m.Categoria!.Nome, m.FormaPagamentoId, m.FormaPagamento!.Nome,
            m.DividaId, m.ComprovanteUrl != null, m.ComprovanteUrl, m.Estornada, m.UsuarioId);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MovimentacaoResponse>>> Listar(
        [FromQuery] TipoMovimentacao? tipo,
        [FromQuery] StatusMovimentacao? status,
        [FromQuery] DateTime? de,
        [FromQuery] DateTime? ate,
        [FromQuery] Guid? dividaId,
        [FromQuery] int limite = 200)
    {
        limite = Math.Clamp(limite, 1, 1000);

        var query = _context.Movimentacoes.AsNoTracking().AsQueryable();

        if (tipo.HasValue) query = query.Where(m => m.Tipo == tipo.Value);
        if (status.HasValue) query = query.Where(m => m.Status == status.Value);
        if (de.HasValue) query = query.Where(m => m.DataVencimento >= de.Value.Date);
        if (ate.HasValue) query = query.Where(m => m.DataVencimento <= ate.Value.Date);
        if (dividaId.HasValue) query = query.Where(m => m.DividaId == dividaId.Value);

        var lista = await query
            .OrderByDescending(m => m.DataVencimento)
            .ThenByDescending(m => m.DataCriacao)
            .Take(limite)
            .Select(Projecao)
            .ToListAsync();

        return Ok(lista);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MovimentacaoResponse>> ObterPorId(Guid id)
    {
        var mov = await _context.Movimentacoes.AsNoTracking()
            .Where(m => m.Id == id)
            .Select(Projecao)
            .FirstOrDefaultAsync();

        return mov is null
            ? NotFound(new { mensagem = "Movimentação não encontrada." })
            : Ok(mov);
    }

    [HttpPost]
    [Authorize(Roles = Perfis.GestaoFinanceira)]
    public async Task<ActionResult<MovimentacaoResponse>> Criar([FromBody] MovimentacaoRequest request)
    {
        if (!TentarObterUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var erro = await ValidarReferenciasAsync(request.Tipo, request.CategoriaId!.Value, request.FormaPagamentoId!.Value);
        if (erro is not null)
        {
            return BadRequest(new { mensagem = erro });
        }

        var movimentacao = new Movimentacao
        {
            Tipo = request.Tipo,
            Valor = Math.Round(request.Valor, 2),
            Descricao = request.Descricao.Trim(),
            Status = request.Status,
            Data = DateTime.UtcNow,
            DataVencimento = request.DataVencimento!.Value.Date,
            DataPagamento = request.Status == StatusMovimentacao.Pago ? request.DataPagamento!.Value.Date : null,
            CategoriaId = request.CategoriaId.Value,
            FormaPagamentoId = request.FormaPagamentoId.Value,
            UsuarioId = usuarioId,
            SincronizadoOffline = false
        };

        _context.Movimentacoes.Add(movimentacao);
        await _context.SaveChangesAsync();

        var response = await _context.Movimentacoes.AsNoTracking()
            .Where(m => m.Id == movimentacao.Id)
            .Select(Projecao)
            .FirstAsync();

        return CreatedAtAction(nameof(ObterPorId), new { id = movimentacao.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Perfis.GestaoFinanceira)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] MovimentacaoRequest request)
    {
        var movimentacao = await _context.Movimentacoes.FindAsync(id);
        if (movimentacao is null)
        {
            return NotFound(new { mensagem = "Movimentação não encontrada." });
        }

        if (movimentacao.Estornada)
        {
            return Conflict(new { mensagem = "Movimentações estornadas não podem ser editadas." });
        }

        if (movimentacao.DividaId.HasValue && request.Tipo != movimentacao.Tipo)
        {
            return BadRequest(new { mensagem = "O tipo de uma parcela de dívida não pode ser alterado." });
        }

        var erro = await ValidarReferenciasAsync(request.Tipo, request.CategoriaId!.Value, request.FormaPagamentoId!.Value);
        if (erro is not null)
        {
            return BadRequest(new { mensagem = erro });
        }

        movimentacao.Tipo = request.Tipo;
        movimentacao.Valor = Math.Round(request.Valor, 2);
        movimentacao.Descricao = request.Descricao.Trim();
        movimentacao.Status = request.Status;
        movimentacao.DataVencimento = request.DataVencimento!.Value.Date;
        movimentacao.DataPagamento = request.Status == StatusMovimentacao.Pago ? request.DataPagamento!.Value.Date : null;
        movimentacao.CategoriaId = request.CategoriaId.Value;
        movimentacao.FormaPagamentoId = request.FormaPagamentoId.Value;

        await _context.SaveChangesAsync();
        await RecalcularDividaAsync(movimentacao.DividaId);

        return NoContent();
    }

    /// <summary>Marca como Pago/Recebido. Se a data não for enviada, usa hoje.</summary>
    [HttpPatch("{id:guid}/pagar")]
    [Authorize(Roles = Perfis.GestaoFinanceira)]
    public async Task<IActionResult> Pagar(Guid id, [FromBody] PagarMovimentacaoRequest? request)
    {
        var movimentacao = await _context.Movimentacoes.FindAsync(id);
        if (movimentacao is null)
        {
            return NotFound(new { mensagem = "Movimentação não encontrada." });
        }

        if (movimentacao.Estornada)
        {
            return Conflict(new { mensagem = "Movimentações estornadas não podem ser pagas." });
        }

        if (movimentacao.Status == StatusMovimentacao.Pago)
        {
            return Conflict(new { mensagem = "Esta movimentação já está paga/recebida." });
        }

        var dataPagamento = (request?.DataPagamento ?? DateTime.UtcNow).Date;
        if (dataPagamento > DateTime.UtcNow.Date.AddDays(1))
        {
            return BadRequest(new { mensagem = "A data do pagamento não pode estar no futuro." });
        }

        movimentacao.Status = StatusMovimentacao.Pago;
        movimentacao.DataPagamento = dataPagamento;

        await _context.SaveChangesAsync();
        await RecalcularDividaAsync(movimentacao.DividaId);

        return NoContent();
    }

    /// <summary>
    /// Estorno (rollback lógico): o registro permanece no histórico, mas deixa
    /// de contar no saldo atual e na projeção.
    /// </summary>
    [HttpPatch("{id:guid}/estornar")]
    [Authorize(Roles = Perfis.GestaoFinanceira)]
    public async Task<IActionResult> Estornar(Guid id)
    {
        var movimentacao = await _context.Movimentacoes.FindAsync(id);
        if (movimentacao is null)
        {
            return NotFound(new { mensagem = "Movimentação não encontrada." });
        }

        if (movimentacao.Estornada)
        {
            return Conflict(new { mensagem = "Esta movimentação já foi estornada." });
        }

        movimentacao.Estornada = true;

        await _context.SaveChangesAsync();
        await RecalcularDividaAsync(movimentacao.DividaId);

        return NoContent();
    }

    // ---------------- Comprovante (imagem) ----------------

    /// <summary>
    /// Anexa/substitui o comprovante (multipart/form-data, campo "arquivo").
    /// Aceita JPEG, PNG, WEBP e HEIC de até 5 MB; o tipo é detectado pelo
    /// conteúdo do arquivo, não pelo que o cliente declara.
    /// </summary>
    [HttpPost("{id:guid}/comprovante")]
    [Authorize(Roles = Perfis.GestaoFinanceira)]
    [RequestSizeLimit(TamanhoMaximoComprovante + 1024 * 1024)]
    public async Task<IActionResult> EnviarComprovante(Guid id, IFormFile arquivo)
    {
        if (arquivo is null || arquivo.Length == 0)
        {
            return BadRequest(new { mensagem = "Nenhum arquivo foi enviado." });
        }

        if (arquivo.Length > TamanhoMaximoComprovante)
        {
            return BadRequest(new { mensagem = "O comprovante deve ter no máximo 5 MB." });
        }

        var movimentacao = await _context.Movimentacoes.FindAsync(id);
        if (movimentacao is null)
        {
            return NotFound(new { mensagem = "Movimentação não encontrada." });
        }

        using var memoria = new MemoryStream();
        await arquivo.CopyToAsync(memoria);
        var bytes = memoria.ToArray();

        var tipo = DetectorImagem.DetectarContentType(bytes);
        if (tipo is null)
        {
            return BadRequest(new { mensagem = "O comprovante precisa ser uma imagem (JPEG, PNG, WEBP ou HEIC)." });
        }

        movimentacao.ComprovanteConteudo = bytes;
        movimentacao.ComprovanteTipo = tipo;
        movimentacao.ComprovanteUrl = $"api/movimentacoes/{id}/comprovante";

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:guid}/comprovante")]
    public async Task<IActionResult> ObterComprovante(Guid id)
    {
        var comprovante = await _context.Movimentacoes.AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new { m.ComprovanteConteudo, m.ComprovanteTipo })
            .FirstOrDefaultAsync();

        if (comprovante?.ComprovanteConteudo is null)
        {
            return NotFound(new { mensagem = "Esta movimentação não possui comprovante." });
        }

        return File(comprovante.ComprovanteConteudo, comprovante.ComprovanteTipo ?? "application/octet-stream");
    }

    [HttpDelete("{id:guid}/comprovante")]
    [Authorize(Roles = Perfis.GestaoFinanceira)]
    public async Task<IActionResult> RemoverComprovante(Guid id)
    {
        var movimentacao = await _context.Movimentacoes.FindAsync(id);
        if (movimentacao is null)
        {
            return NotFound(new { mensagem = "Movimentação não encontrada." });
        }

        movimentacao.ComprovanteConteudo = null;
        movimentacao.ComprovanteTipo = null;
        movimentacao.ComprovanteUrl = null;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ---------------- Auxiliares ----------------

    private bool TentarObterUsuarioId(out Guid usuarioId)
    {
        var valor = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(valor, out usuarioId);
    }

    /// <summary>
    /// Categoria e forma de pagamento precisam existir, estar ativas e a
    /// categoria deve ser do mesmo tipo da movimentação (Receita/Despesa).
    /// </summary>
    private async Task<string?> ValidarReferenciasAsync(TipoMovimentacao tipo, Guid categoriaId, Guid formaPagamentoId)
    {
        var categoria = await _context.Categorias.AsNoTracking().FirstOrDefaultAsync(c => c.Id == categoriaId);
        if (categoria is null || !categoria.Ativo)
        {
            return "Categoria inválida ou desativada.";
        }

        if ((int)categoria.Tipo != (int)tipo)
        {
            return $"A categoria \"{categoria.Nome}\" não é do tipo {tipo}.";
        }

        var forma = await _context.FormasPagamento.AsNoTracking().FirstOrDefaultAsync(f => f.Id == formaPagamentoId);
        if (forma is null || !forma.Ativo)
        {
            return "Forma de pagamento inválida ou desativada.";
        }

        return null;
    }

    private async Task RecalcularDividaAsync(Guid? dividaId)
    {
        if (!dividaId.HasValue)
        {
            return;
        }

        await DividaRegras.RecalcularAsync(_context, dividaId.Value);
        await _context.SaveChangesAsync();
    }
}
