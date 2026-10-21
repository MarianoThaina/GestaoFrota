using GestaoFrota.Domain.Enums;

namespace GestaoFrota.Domain.Services;

/// <summary>Viagem mínima para as estatísticas (US10/US13).</summary>
public readonly record struct ViagemEstatistica(
    Guid ViagemId,
    Guid RotaId,
    string RotaNome,
    string RotaOrigem,
    string RotaDestino,
    Guid VeiculoId,
    string VeiculoPlaca,
    string VeiculoDescricao,
    StatusViagem Status,
    decimal? QuilometragemRodadaKm);

/// <summary>Lançamento vinculado a uma viagem (US09), já sem os estornados.</summary>
public readonly record struct LancamentoViagem(
    Guid ViagemId,
    TipoMovimentacao Tipo,
    decimal Valor,
    decimal? QuantidadeLitros);

/// <summary>Resultado de uma viagem (US10).</summary>
public record ResultadoViagem(
    Guid ViagemId,
    decimal Receitas,
    decimal Despesas,
    decimal Rentabilidade,
    decimal? KmRodados,
    decimal? CustoPorKm,
    decimal Litros,
    decimal? KmPorLitro);

/// <summary>Rota agregada pelas viagens do período (US10/US13).</summary>
public record EstatisticaRota(
    Guid RotaId,
    string Nome,
    string Origem,
    string Destino,
    int Viagens,
    decimal Receitas,
    decimal Despesas,
    decimal Rentabilidade,
    decimal KmRodados,
    decimal? CustoPorKm,
    decimal RentabilidadeMediaPorViagem);

/// <summary>Veículo agregado pelas viagens do período (US10/US13).</summary>
public record EstatisticaVeiculo(
    Guid VeiculoId,
    string Placa,
    string Descricao,
    int Viagens,
    decimal KmRodados,
    decimal Receitas,
    decimal Despesas,
    decimal? CustoPorKm,
    decimal Litros,
    decimal? KmPorLitro);

/// <summary>
/// Estatísticas de rota e veículo (US10) e ranking de eficiência (US13).
/// Funções puras, sem banco, como a <see cref="CalculadoraRelatorios"/>.
/// Fórmulas do documento de requisitos:
/// - rentabilidade = soma das entradas vinculadas à viagem − soma das saídas vinculadas;
/// - custo por km = saídas vinculadas ÷ quilometragem rodada;
/// - consumo médio (km/l) = quilometragem rodada ÷ litros de combustível vinculados.
/// Viagens canceladas ficam de fora. Viagens em andamento entram no financeiro,
/// mas sem quilometragem (ainda não encerraram), então não entram em custo/km nem km/l.
/// </summary>
public static class CalculadoraFrota
{
    public static IReadOnlyList<ResultadoViagem> PorViagem(
        IEnumerable<ViagemEstatistica> viagens,
        IEnumerable<LancamentoViagem> lancamentos)
    {
        var porViagem = lancamentos
            .GroupBy(l => l.ViagemId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var resultado = new List<ResultadoViagem>();

        foreach (var viagem in viagens.Where(v => v.Status != StatusViagem.Cancelada))
        {
            porViagem.TryGetValue(viagem.ViagemId, out var itens);
            itens ??= new List<LancamentoViagem>();

            var receitas = itens.Where(l => l.Tipo == TipoMovimentacao.Receita).Sum(l => l.Valor);
            var despesas = itens.Where(l => l.Tipo == TipoMovimentacao.Despesa).Sum(l => l.Valor);
            var litros = itens.Where(l => l.Tipo == TipoMovimentacao.Despesa).Sum(l => l.QuantidadeLitros ?? 0m);
            var km = viagem.Status == StatusViagem.Concluida ? viagem.QuilometragemRodadaKm : null;

            resultado.Add(new ResultadoViagem(
                viagem.ViagemId,
                receitas,
                despesas,
                receitas - despesas,
                km,
                Divisao(despesas, km),
                litros,
                Divisao(km, litros)));
        }

        return resultado;
    }

    public static IReadOnlyList<EstatisticaRota> PorRota(
        IEnumerable<ViagemEstatistica> viagens,
        IEnumerable<LancamentoViagem> lancamentos)
    {
        var lista = viagens.Where(v => v.Status != StatusViagem.Cancelada).ToList();
        var resultados = PorViagem(lista, lancamentos).ToDictionary(r => r.ViagemId);

        return lista
            .GroupBy(v => v.RotaId)
            .Select(g =>
            {
                var primeira = g.First();
                var itens = g.Select(v => resultados[v.ViagemId]).ToList();
                var receitas = itens.Sum(i => i.Receitas);
                var despesas = itens.Sum(i => i.Despesas);
                var km = itens.Sum(i => i.KmRodados ?? 0m);
                var rentabilidade = receitas - despesas;

                return new EstatisticaRota(
                    g.Key, primeira.RotaNome, primeira.RotaOrigem, primeira.RotaDestino,
                    itens.Count, receitas, despesas, rentabilidade, km,
                    Divisao(despesas, km),
                    itens.Count == 0 ? 0m : Math.Round(rentabilidade / itens.Count, 2));
            })
            .OrderByDescending(r => r.Rentabilidade)
            .ThenBy(r => r.Nome)
            .ToList();
    }

    public static IReadOnlyList<EstatisticaVeiculo> PorVeiculo(
        IEnumerable<ViagemEstatistica> viagens,
        IEnumerable<LancamentoViagem> lancamentos)
    {
        var lista = viagens.Where(v => v.Status != StatusViagem.Cancelada).ToList();
        var resultados = PorViagem(lista, lancamentos).ToDictionary(r => r.ViagemId);

        return lista
            .GroupBy(v => v.VeiculoId)
            .Select(g =>
            {
                var primeira = g.First();
                var itens = g.Select(v => resultados[v.ViagemId]).ToList();
                var km = itens.Sum(i => i.KmRodados ?? 0m);
                var despesas = itens.Sum(i => i.Despesas);
                var litros = itens.Sum(i => i.Litros);

                return new EstatisticaVeiculo(
                    g.Key, primeira.VeiculoPlaca, primeira.VeiculoDescricao,
                    itens.Count, km, itens.Sum(i => i.Receitas), despesas,
                    Divisao(despesas, km), litros, Divisao(km, litros));
            })
            .OrderBy(v => v.CustoPorKm ?? decimal.MaxValue)
            .ThenBy(v => v.Placa)
            .ToList();
    }

    /// <summary>US13: as rotas mais lucrativas (rentabilidade total), só as que tiveram viagem.</summary>
    public static IReadOnlyList<EstatisticaRota> RotasMaisLucrativas(IEnumerable<EstatisticaRota> rotas, int quantidade = 3) =>
        rotas.Where(r => r.Viagens > 0)
            .OrderByDescending(r => r.Rentabilidade)
            .ThenBy(r => r.Nome)
            .Take(Math.Max(1, quantidade))
            .ToList();

    /// <summary>
    /// US13: os veículos mais eficientes = menor custo por km. Veículo sem
    /// quilometragem rodada no período não tem custo/km e fica de fora.
    /// </summary>
    public static IReadOnlyList<EstatisticaVeiculo> VeiculosMaisEficientes(IEnumerable<EstatisticaVeiculo> veiculos, int quantidade = 3) =>
        veiculos.Where(v => v.CustoPorKm.HasValue)
            .OrderBy(v => v.CustoPorKm)
            .ThenBy(v => v.Placa)
            .Take(Math.Max(1, quantidade))
            .ToList();

    /// <summary>Divisão protegida: nula quando o divisor é nulo ou zero. Duas casas.</summary>
    private static decimal? Divisao(decimal? dividendo, decimal? divisor)
    {
        if (!dividendo.HasValue || !divisor.HasValue || divisor.Value <= 0m)
        {
            return null;
        }

        return Math.Round(dividendo.Value / divisor.Value, 2);
    }
}
