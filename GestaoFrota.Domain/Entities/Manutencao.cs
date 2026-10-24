using GestaoFrota.Domain.Common;

namespace GestaoFrota.Domain.Entities;

/// <summary>
/// Registro de manutenção de um veículo (US06-b). Ao ser criada, a API coloca
/// o veículo "Em Manutenção" e gera automaticamente uma movimentação de saída
/// na categoria Manutenção (<see cref="MovimentacaoId"/>), tudo na mesma
/// transação. Ao ser concluída, o veículo volta a "Disponível".
/// </summary>
public class Manutencao : EntidadeBase
{
    public Guid VeiculoId { get; set; }
    public Veiculo? Veiculo { get; set; }

    /// <summary>Data em que o serviço foi realizado/iniciado.</summary>
    public DateTime Data { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public decimal ValorTotal { get; set; }

    /// <summary>Nulo enquanto o veículo continua parado na oficina.</summary>
    public DateTime? DataConclusao { get; set; }

    /// <summary>Despesa gerada automaticamente no caixa.</summary>
    public Guid? MovimentacaoId { get; set; }
    public Movimentacao? Movimentacao { get; set; }

    public bool Concluida => DataConclusao.HasValue;
}
