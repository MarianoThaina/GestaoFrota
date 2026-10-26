using System.Globalization;
using System.Text;
using GestaoFrota.API.DTOs;
using GestaoFrota.API.Security;
using GestaoFrota.API.Services;
using GestaoFrota.Domain.Enums;
using GestaoFrota.Domain.Services;
using GestaoFrota.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFrota.API.Controllers;

/// <summary>
/// Relatórios financeiros (US11 painel de indicadores, US12 gráficos de fluxo
/// de caixa e US14 exportação) e estatísticas de frota (US10/US13).
/// Período (mesmos parâmetros nos três endpoints): <c>meses=N</c> (últimos N,
/// incluindo o atual, padrão 6), <c>tudo=true</c> (desde o primeiro registro),
/// <c>ano=2026</c> (ano inteiro), <c>ano=2026&amp;mes=10</c> (um mês) ou
/// <c>de=2026-01-15&amp;ate=2026-03-20</c> (livre). Agrupado pela data de
/// vencimento. Estornadas nunca entram. Leitura: Administrador, Gestor de Frota
/// e Financeiro; o Motorista recebe 403.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Perfis.LeituraFinanceira)]
public class RelatoriosController : ControllerBase
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    private readonly AppDbContext _context;

    public RelatoriosController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Fluxo mensal, despesas por categoria e indicadores em uma única resposta.</summary>
    [HttpGet("resumo")]
    public async Task<ActionResult<RelatorioResumoResponse>> ObterResumo(
        [FromQuery] int? meses, [FromQuery] DateTime? de, [FromQuery] DateTime? ate,
        [FromQuery] int? ano, [FromQuery] int? mes, [FromQuery] bool tudo = false)
    {
        var hoje = DateTime.UtcNow;
        var periodo = await ResolverPeriodoFinanceiroAsync(hoje, meses, de, ate, ano, mes, tudo);
        var inicioComMesAnterior = periodo.Inicio.AddMonths(-1);
        var mesAtual = new DateTime(hoje.Year, hoje.Month, 1);
        var fimMesAtual = mesAtual.AddMonths(1);

        // Carrega o período, o mês anterior a ele (variação de saídas), o mês
        // corrente (a receber/a pagar) e os pendentes vencidos de qualquer data.
        var brutos = await _context.Movimentacoes.AsNoTracking()
            .Where(m => !m.Estornada
                        && ((m.DataVencimento >= inicioComMesAnterior && m.DataVencimento < periodo.Fim)
                            || (m.DataVencimento >= mesAtual.AddMonths(-1) && m.DataVencimento < fimMesAtual)
                            || (m.Status == StatusMovimentacao.Pendente && m.DataVencimento < hoje)))
            .Select(m => new { m.Tipo, m.Status, m.Valor, m.DataVencimento, Categoria = m.Categoria!.Nome })
            .ToListAsync();

        var lancamentos = brutos
            .Select(m => new LancamentoRelatorio(m.Tipo, m.Status, m.Valor, m.DataVencimento, m.Categoria))
            .ToList();

        var fluxo = CalculadoraRelatorios.FluxoPorMes(lancamentos, periodo.Inicio, periodo.Fim)
            .Select(f => new FluxoMensalResponse(
                f.Ano, f.Mes, $"{f.Mes:00}/{f.Ano}",
                f.Entradas, f.Saidas, f.SaldoDoMes, f.EntradasRealizadas, f.SaidasRealizadas))
            .ToList();

        var categorias = CalculadoraRelatorios.DespesasPorCategoria(lancamentos, periodo.Inicio, periodo.Fim)
            .Select(c => new DespesaCategoriaResponse(c.Categoria, c.Total, c.Percentual))
            .ToList();

        var i = CalculadoraRelatorios.Indicadores(lancamentos, hoje, periodo.Inicio, periodo.Fim);
        var indicadores = new IndicadoresResponse(
            i.TotalEntradas, i.TotalSaidas, i.Resultado, i.AReceberNoMes, i.APagarNoMes, i.EmAtraso,
            i.MaiorDespesaCategoria, i.MaiorDespesaValor, i.VariacaoSaidasPercentual, i.QuantidadeLancamentos);

        return Ok(new RelatorioResumoResponse(
            periodo.QuantidadeMeses, periodo.Inicio, periodo.UltimoDia, periodo.Rotulo, indicadores, fluxo, categorias, hoje));
    }

    /// <summary>
    /// Estatísticas de rota e veículo (US10) e ranking de eficiência (US13):
    /// viagens iniciadas no período, com as movimentações vinculadas a elas
    /// (US09). Rentabilidade = entradas − saídas vinculadas; custo/km = saídas
    /// ÷ km rodados; km/l = km rodados ÷ litros. Canceladas ficam de fora.
    /// </summary>
    [HttpGet("frota")]
    public async Task<ActionResult<EstatisticasFrotaResponse>> ObterFrota(
        [FromQuery] int? meses, [FromQuery] DateTime? de, [FromQuery] DateTime? ate,
        [FromQuery] int? ano, [FromQuery] int? mes, [FromQuery] bool tudo = false)
    {
        var hoje = DateTime.UtcNow;

        DateTime? primeiraViagem = tudo
            ? await _context.Viagens.AsNoTracking().MinAsync(v => (DateTime?)v.DataInicio)
            : null;
        var periodo = PeriodoRelatorio.Resolver(hoje, meses, de, ate, ano, mes, tudo, primeiraViagem);

        var viagens = await _context.Viagens.AsNoTracking()
            .Where(v => v.Status != StatusViagem.Cancelada && v.DataInicio >= periodo.Inicio && v.DataInicio < periodo.Fim)
            .Select(v => new ViagemEstatistica(
                v.Id,
                v.RotaId, v.Rota!.Nome, v.Rota.Origem, v.Rota.Destino,
                v.VeiculoId, v.Veiculo!.Placa, v.Veiculo.Marca + " " + v.Veiculo.Modelo,
                v.Status, v.QuilometragemRodadaKm))
            .ToListAsync();

        var ids = viagens.Select(v => v.ViagemId).ToList();

        var lancamentos = await _context.Movimentacoes.AsNoTracking()
            .Where(m => !m.Estornada && m.ViagemId != null && ids.Contains(m.ViagemId.Value))
            .Select(m => new LancamentoViagem(m.ViagemId!.Value, m.Tipo, m.Valor, m.QuantidadeLitros))
            .ToListAsync();

        var rotas = CalculadoraFrota.PorRota(viagens, lancamentos);
        var veiculos = CalculadoraFrota.PorVeiculo(viagens, lancamentos);

        var veiculosEmViagem = await _context.Veiculos.CountAsync(v => v.Ativo && v.Status == StatusVeiculo.EmViagem);
        var veiculosAtivos = await _context.Veiculos.CountAsync(v => v.Ativo);

        static EstatisticaRotaResponse MapearRota(EstatisticaRota r) => new(
            r.RotaId, r.Nome, r.Origem, r.Destino, r.Viagens, r.Receitas, r.Despesas, r.Rentabilidade,
            r.KmRodados, r.CustoPorKm, r.RentabilidadeMediaPorViagem);

        static EstatisticaVeiculoResponse MapearVeiculo(EstatisticaVeiculo v) => new(
            v.VeiculoId, v.Placa, ValidadorPlaca.Formatar(v.Placa), v.Descricao, v.Viagens, v.KmRodados,
            v.Receitas, v.Despesas, v.CustoPorKm, v.Litros, v.KmPorLitro);

        return Ok(new EstatisticasFrotaResponse(
            periodo.QuantidadeMeses, periodo.Inicio, periodo.UltimoDia,
            veiculosEmViagem, veiculosAtivos,
            viagens.Count,
            viagens.Count(v => v.Status == StatusViagem.Concluida),
            viagens.Sum(v => v.QuilometragemRodadaKm ?? 0m),
            lancamentos.Where(l => l.Tipo == TipoMovimentacao.Receita).Sum(l => l.Valor),
            lancamentos.Where(l => l.Tipo == TipoMovimentacao.Despesa).Sum(l => l.Valor),
            rotas.Select(MapearRota).ToList(),
            veiculos.Select(MapearVeiculo).ToList(),
            CalculadoraFrota.RotasMaisLucrativas(rotas).Select(MapearRota).ToList(),
            CalculadoraFrota.VeiculosMaisEficientes(veiculos).Select(MapearVeiculo).ToList(),
            hoje));
    }

    /// <summary>
    /// Exporta as movimentações do período em CSV (US14). Separador ";" e BOM
    /// UTF-8 para o Excel em português abrir sem configuração.
    /// </summary>
    [HttpGet("exportar")]
    public async Task<IActionResult> Exportar(
        [FromQuery] int? meses, [FromQuery] DateTime? de, [FromQuery] DateTime? ate,
        [FromQuery] int? ano, [FromQuery] int? mes, [FromQuery] bool tudo = false)
    {
        var hoje = DateTime.UtcNow;
        var periodo = await ResolverPeriodoFinanceiroAsync(hoje, meses, de, ate, ano, mes, tudo);

        var linhas = await _context.Movimentacoes.AsNoTracking()
            .Where(m => !m.Estornada && m.DataVencimento >= periodo.Inicio && m.DataVencimento < periodo.Fim)
            .OrderBy(m => m.DataVencimento)
            .ThenBy(m => m.DataCriacao)
            .Select(m => new
            {
                m.DataVencimento,
                m.Tipo,
                m.Descricao,
                Categoria = m.Categoria!.Nome,
                FormaPagamento = m.FormaPagamento!.Nome,
                m.Status,
                m.DataPagamento,
                m.Valor
            })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.Append("Periodo;").Append(periodo.Rotulo).Append(';')
          .Append(periodo.Inicio.ToString("dd/MM/yyyy", PtBr)).Append(';')
          .Append(periodo.UltimoDia.ToString("dd/MM/yyyy", PtBr)).AppendLine();
        sb.AppendLine("Vencimento;Tipo;Descricao;Categoria;Forma de pagamento;Situacao;Pagamento;Valor");

        foreach (var l in linhas)
        {
            sb.Append(l.DataVencimento.ToString("dd/MM/yyyy", PtBr)).Append(';')
              .Append(l.Tipo == TipoMovimentacao.Receita ? "Receita" : "Despesa").Append(';')
              .Append(Csv(l.Descricao)).Append(';')
              .Append(Csv(l.Categoria)).Append(';')
              .Append(Csv(l.FormaPagamento)).Append(';')
              .Append(l.Status == StatusMovimentacao.Pago ? "Pago/Recebido" : "Pendente").Append(';')
              .Append(l.DataPagamento.HasValue ? l.DataPagamento.Value.ToString("dd/MM/yyyy", PtBr) : string.Empty).Append(';')
              .Append(l.Valor.ToString("F2", PtBr))
              .AppendLine();
        }

        var totalEntradas = linhas.Where(l => l.Tipo == TipoMovimentacao.Receita).Sum(l => l.Valor);
        var totalSaidas = linhas.Where(l => l.Tipo == TipoMovimentacao.Despesa).Sum(l => l.Valor);
        sb.AppendLine();
        sb.Append("Total de entradas;;;;;;;").Append(totalEntradas.ToString("F2", PtBr)).AppendLine();
        sb.Append("Total de saidas;;;;;;;").Append(totalSaidas.ToString("F2", PtBr)).AppendLine();
        sb.Append("Resultado;;;;;;;").Append((totalEntradas - totalSaidas).ToString("F2", PtBr)).AppendLine();

        // BOM UTF-8: sem ele o Excel (Windows, pt-BR) troca os acentos por símbolos.
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var nomeArquivo = $"relatorio-financeiro-{periodo.Inicio:yyyyMMdd}-{periodo.UltimoDia:yyyyMMdd}.csv";

        return File(bytes, "text/csv; charset=utf-8", nomeArquivo);
    }

    // ---------------- Auxiliares ----------------

    /// <summary>"Tudo" começa no vencimento mais antigo não estornado.</summary>
    private async Task<PeriodoRelatorio> ResolverPeriodoFinanceiroAsync(
        DateTime hoje, int? meses, DateTime? de, DateTime? ate, int? ano, int? mes, bool tudo)
    {
        DateTime? primeiro = tudo
            ? await _context.Movimentacoes.AsNoTracking().Where(m => !m.Estornada).MinAsync(m => (DateTime?)m.DataVencimento)
            : null;

        return PeriodoRelatorio.Resolver(hoje, meses, de, ate, ano, mes, tudo, primeiro);
    }

    /// <summary>Protege o campo contra ";" e aspas, que quebrariam a coluna.</summary>
    private static string Csv(string? valor)
    {
        if (string.IsNullOrEmpty(valor))
        {
            return string.Empty;
        }

        var precisaAspas = valor.Contains(';') || valor.Contains('"') || valor.Contains('\n');
        var texto = valor.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ");
        return precisaAspas ? $"\"{texto}\"" : texto;
    }
}
