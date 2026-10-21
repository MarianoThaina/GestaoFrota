using GestaoFrota.Domain.Common;
using GestaoFrota.Domain.Enums;

namespace GestaoFrota.Domain.Entities;

/// <summary>
/// Movimentação financeira (receita ou despesa). É a entidade central
/// do módulo financeiro: alimenta o motor de saldo e as projeções.
/// Pode estar vinculada a uma viagem (frete, abastecimento) e/ou a uma dívida (parcela).
/// Suporta rollback (estorno) via campo Estornada.
/// </summary>
public class Movimentacao : EntidadeBase
{
    public TipoMovimentacao Tipo { get; set; }

    public decimal Valor { get; set; }

    /// <summary>Data do lançamento (quando o registro foi feito).</summary>
    public DateTime Data { get; set; } = DateTime.UtcNow;

    public string? Descricao { get; set; }

    /// <summary>
    /// Pendente ou Pago/Recebido. Só movimentações Pagas entram no saldo
    /// atual; as Pendentes alimentam a projeção de caixa.
    /// </summary>
    public StatusMovimentacao Status { get; set; } = StatusMovimentacao.Pendente;

    /// <summary>Data de vencimento (agrupa a projeção de caixa por mês).</summary>
    public DateTime DataVencimento { get; set; } = DateTime.UtcNow.Date;

    /// <summary>
    /// Data em que foi paga/recebida. Obrigatória quando Status = Pago
    /// (regra validada na API e protegida por check constraint no banco).
    /// </summary>
    public DateTime? DataPagamento { get; set; }

    /// <summary>
    /// URL relativa para baixar o comprovante (ex.: api/movimentacoes/{id}/comprovante).
    /// O conteúdo da imagem fica em ComprovanteConteudo.
    /// </summary>
    public string? ComprovanteUrl { get; set; }

    /// <summary>Bytes da imagem do comprovante (câmera ou galeria do dispositivo).</summary>
    public byte[]? ComprovanteConteudo { get; set; }

    /// <summary>Content-Type da imagem (image/jpeg, image/png...).</summary>
    public string? ComprovanteTipo { get; set; }

    public Guid CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public Guid FormaPagamentoId { get; set; }
    public FormaPagamento? FormaPagamento { get; set; }

    public Guid UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    /// <summary>Vínculo com a viagem (US09): frete recebido, abastecimento, pedágio...</summary>
    public Guid? ViagemId { get; set; }
    public Viagem? Viagem { get; set; }

    /// <summary>
    /// Litros abastecidos (US09). Preenchido só em lançamentos de combustível;
    /// é o denominador do km/l por viagem (US10).
    /// </summary>
    public decimal? QuantidadeLitros { get; set; }

    public Guid? DividaId { get; set; }
    public Divida? Divida { get; set; }

    /// <summary>
    /// Indica se a movimentação foi estornada (rollback), mantendo
    /// o histórico íntegro em vez de excluir o registro.
    /// </summary>
    public bool Estornada { get; set; }

    public bool SincronizadoOffline { get; set; } = true;
}
