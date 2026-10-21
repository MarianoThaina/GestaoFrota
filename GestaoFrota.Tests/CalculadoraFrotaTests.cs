using GestaoFrota.Domain.Enums;
using GestaoFrota.Domain.Services;

namespace GestaoFrota.Tests;

/// <summary>US10 (estatísticas de rota e veículo) e US13 (ranking de eficiência).</summary>
public class CalculadoraFrotaTests
{
    private static readonly Guid RotaSpCwb = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid RotaSpRio = Guid.Parse("00000000-0000-0000-0000-00000000000b");
    private static readonly Guid Volvo = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid Scania = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private static ViagemEstatistica Viagem(
        Guid id,
        Guid rota,
        Guid veiculo,
        decimal? km,
        StatusViagem status = StatusViagem.Concluida)
    {
        var (rotaNome, origem, destino) = rota == RotaSpCwb
            ? ("SP - Curitiba", "São Paulo/SP", "Curitiba/PR")
            : ("SP - Rio", "São Paulo/SP", "Rio de Janeiro/RJ");

        var (placa, descricao) = veiculo == Volvo ? ("RTA9F12", "Volvo FH 460") : ("QXB4521", "Scania R 450");

        return new ViagemEstatistica(id, rota, rotaNome, origem, destino, veiculo, placa, descricao, status, km);
    }

    private static LancamentoViagem Receita(Guid viagem, decimal valor) =>
        new(viagem, TipoMovimentacao.Receita, valor, null);

    private static LancamentoViagem Despesa(Guid viagem, decimal valor, decimal? litros = null) =>
        new(viagem, TipoMovimentacao.Despesa, valor, litros);

    // ---------- Por viagem ----------

    [Fact]
    public void PorViagem_CalculaRentabilidadeCustoPorKmEKmPorLitro()
    {
        // Caso do roteiro de teste do PR: 415 km, R$ 4.200 de frete, R$ 2.230,50 em 372,5 L.
        var v1 = Guid.NewGuid();
        var viagens = new[] { Viagem(v1, RotaSpCwb, Volvo, 415m) };
        var lancamentos = new[] { Receita(v1, 4200m), Despesa(v1, 2230.50m, 372.5m) };

        var r = CalculadoraFrota.PorViagem(viagens, lancamentos).Single();

        Assert.Equal(4200m, r.Receitas);
        Assert.Equal(2230.50m, r.Despesas);
        Assert.Equal(1969.50m, r.Rentabilidade);
        Assert.Equal(415m, r.KmRodados);
        Assert.Equal(5.37m, r.CustoPorKm);
        Assert.Equal(372.5m, r.Litros);
        Assert.Equal(1.11m, r.KmPorLitro);
    }

    [Fact]
    public void PorViagem_ViagemCanceladaFicaDeFora()
    {
        var cancelada = Guid.NewGuid();
        var viagens = new[] { Viagem(cancelada, RotaSpCwb, Volvo, 415m, StatusViagem.Cancelada) };
        var lancamentos = new[] { Receita(cancelada, 4200m) };

        Assert.Empty(CalculadoraFrota.PorViagem(viagens, lancamentos));
    }

    [Fact]
    public void PorViagem_EmAndamentoEntraNoFinanceiroMasSemQuilometragem()
    {
        var aberta = Guid.NewGuid();
        var viagens = new[] { Viagem(aberta, RotaSpCwb, Volvo, null, StatusViagem.EmAndamento) };
        var lancamentos = new[] { Despesa(aberta, 500m, 100m) };

        var r = CalculadoraFrota.PorViagem(viagens, lancamentos).Single();

        Assert.Equal(-500m, r.Rentabilidade);
        Assert.Null(r.KmRodados);
        Assert.Null(r.CustoPorKm);
        Assert.Null(r.KmPorLitro);
    }

    [Fact]
    public void PorViagem_SemLancamentosVinculados_ZeraOFinanceiroESemCustoPorKm()
    {
        var v1 = Guid.NewGuid();

        var r = CalculadoraFrota.PorViagem(new[] { Viagem(v1, RotaSpCwb, Volvo, 300m) }, Array.Empty<LancamentoViagem>()).Single();

        Assert.Equal(0m, r.Receitas);
        Assert.Equal(0m, r.Rentabilidade);
        Assert.Equal(0m, r.CustoPorKm); // 0 ÷ 300 = 0, custo zero é um custo válido
        Assert.Null(r.KmPorLitro);      // sem litros não há consumo
    }

    [Fact]
    public void PorViagem_LitrosDeReceitaNaoContam()
    {
        var v1 = Guid.NewGuid();
        var lancamentos = new[] { new LancamentoViagem(v1, TipoMovimentacao.Receita, 100m, 50m) };

        var r = CalculadoraFrota.PorViagem(new[] { Viagem(v1, RotaSpCwb, Volvo, 100m) }, lancamentos).Single();

        Assert.Equal(0m, r.Litros);
    }

    // ---------- Por rota ----------

    [Fact]
    public void PorRota_AgregaAsViagensDaRotaEOrdenaPorRentabilidade()
    {
        var a1 = Guid.NewGuid();
        var a2 = Guid.NewGuid();
        var b1 = Guid.NewGuid();

        var viagens = new[]
        {
            Viagem(a1, RotaSpCwb, Volvo, 400m),
            Viagem(a2, RotaSpCwb, Scania, 420m),
            Viagem(b1, RotaSpRio, Volvo, 450m)
        };

        var lancamentos = new[]
        {
            Receita(a1, 4000m), Despesa(a1, 1500m),
            Receita(a2, 4200m), Despesa(a2, 1700m),
            Receita(b1, 9000m), Despesa(b1, 2000m)
        };

        var rotas = CalculadoraFrota.PorRota(viagens, lancamentos);

        Assert.Equal(2, rotas.Count);

        var rio = rotas[0];
        Assert.Equal(RotaSpRio, rio.RotaId);
        Assert.Equal(7000m, rio.Rentabilidade);

        var curitiba = rotas[1];
        Assert.Equal(2, curitiba.Viagens);
        Assert.Equal(8200m, curitiba.Receitas);
        Assert.Equal(3200m, curitiba.Despesas);
        Assert.Equal(5000m, curitiba.Rentabilidade);
        Assert.Equal(820m, curitiba.KmRodados);
        Assert.Equal(3.90m, curitiba.CustoPorKm);
        Assert.Equal(2500m, curitiba.RentabilidadeMediaPorViagem);
    }

    // ---------- Por veículo ----------

    [Fact]
    public void PorVeiculo_OrdenaPeloMenorCustoPorKmEDeixaSemKmPorUltimo()
    {
        var volvo1 = Guid.NewGuid();
        var scania1 = Guid.NewGuid();
        var scaniaAberta = Guid.NewGuid();

        var viagens = new[]
        {
            Viagem(volvo1, RotaSpCwb, Volvo, 400m),
            Viagem(scania1, RotaSpCwb, Scania, 400m),
            Viagem(scaniaAberta, RotaSpRio, Scania, null, StatusViagem.EmAndamento)
        };

        var lancamentos = new[]
        {
            Despesa(volvo1, 1600m, 160m),   // 4,00/km · 2,5 km/L
            Despesa(scania1, 1200m, 150m)   // 3,00/km · 2,67 km/L
        };

        var veiculos = CalculadoraFrota.PorVeiculo(viagens, lancamentos);

        Assert.Equal(Scania, veiculos[0].VeiculoId);
        Assert.Equal(3.00m, veiculos[0].CustoPorKm);
        Assert.Equal(2.67m, veiculos[0].KmPorLitro);
        Assert.Equal(2, veiculos[0].Viagens);          // a aberta conta como viagem
        Assert.Equal(400m, veiculos[0].KmRodados);     // mas não soma km

        Assert.Equal(Volvo, veiculos[1].VeiculoId);
        Assert.Equal(4.00m, veiculos[1].CustoPorKm);
        Assert.Equal(2.5m, veiculos[1].KmPorLitro);
    }

    // ---------- Ranking (US13) ----------

    [Fact]
    public void RotasMaisLucrativas_DevolveNoMaximoTresEmOrdemDecrescente()
    {
        var rotas = Enumerable.Range(1, 5)
            .Select(i => new EstatisticaRota(Guid.NewGuid(), $"Rota {i}", "A", "B", 1, i * 1000m, 0m, i * 1000m, 100m, 0m, i * 1000m))
            .ToList();

        var top = CalculadoraFrota.RotasMaisLucrativas(rotas);

        Assert.Equal(3, top.Count);
        Assert.Equal(new[] { 5000m, 4000m, 3000m }, top.Select(r => r.Rentabilidade));
    }

    [Fact]
    public void RotasMaisLucrativas_IgnoraRotaSemViagem()
    {
        var rotas = new[]
        {
            new EstatisticaRota(Guid.NewGuid(), "Sem viagem", "A", "B", 0, 0m, 0m, 0m, 0m, null, 0m),
            new EstatisticaRota(Guid.NewGuid(), "Com viagem", "A", "B", 1, 100m, 0m, 100m, 10m, 0m, 100m)
        };

        var top = CalculadoraFrota.RotasMaisLucrativas(rotas);

        Assert.Single(top);
        Assert.Equal("Com viagem", top[0].Nome);
    }

    [Fact]
    public void VeiculosMaisEficientes_OrdenaPorMenorCustoEIgnoraQuemNaoTemKm()
    {
        var veiculos = new[]
        {
            new EstatisticaVeiculo(Guid.NewGuid(), "AAA1111", "Caro", 1, 100m, 0m, 500m, 5.00m, 0m, null),
            new EstatisticaVeiculo(Guid.NewGuid(), "BBB2222", "Barato", 1, 100m, 0m, 200m, 2.00m, 0m, null),
            new EstatisticaVeiculo(Guid.NewGuid(), "CCC3333", "Parado", 1, 0m, 0m, 300m, null, 0m, null),
            new EstatisticaVeiculo(Guid.NewGuid(), "DDD4444", "Médio", 1, 100m, 0m, 350m, 3.50m, 0m, null),
            new EstatisticaVeiculo(Guid.NewGuid(), "EEE5555", "Caríssimo", 1, 100m, 0m, 900m, 9.00m, 0m, null)
        };

        var top = CalculadoraFrota.VeiculosMaisEficientes(veiculos);

        Assert.Equal(new[] { "Barato", "Médio", "Caro" }, top.Select(v => v.Descricao));
        Assert.DoesNotContain(top, v => v.Descricao == "Parado");
    }
}
