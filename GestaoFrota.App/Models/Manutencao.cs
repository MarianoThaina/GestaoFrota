using GestaoFrota.App.Helpers;

namespace GestaoFrota.App.Models;

/// <summary>Registro de manutenção (US06-b), lido da API. Sem cache local.</summary>
public class Manutencao
{
    public Guid Id { get; set; }

    public Guid VeiculoId { get; set; }

    public string VeiculoPlaca { get; set; } = string.Empty;

    public string VeiculoDescricao { get; set; } = string.Empty;

    public DateTime Data { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public decimal ValorTotal { get; set; }

    public DateTime? DataConclusao { get; set; }

    public bool Concluida { get; set; }

    public Guid? MovimentacaoId { get; set; }

    public StatusMovimentacao? DespesaStatus { get; set; }

    // ---------- Apresentação ----------

    public string ValorTexto => Formatacao.Moeda(ValorTotal);

    public string DataTexto => Concluida && DataConclusao.HasValue
        ? $"{Formatacao.Data(Data)} → concluída em {Formatacao.Data(DataConclusao.Value)}"
        : $"Aberta em {Formatacao.Data(Data)}";

    public string DespesaTexto => DespesaStatus switch
    {
        StatusMovimentacao.Pago => "Despesa paga",
        StatusMovimentacao.Pendente => "Despesa pendente no caixa",
        _ => "Sem despesa vinculada"
    };
}
