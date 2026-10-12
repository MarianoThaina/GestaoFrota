using System.ComponentModel.DataAnnotations;
using GestaoFrota.Domain.Entities;
using GestaoFrota.Domain.Enums;

namespace GestaoFrota.API.DTOs;

public record DividaResponse(
    Guid Id,
    string Descricao,
    decimal ValorTotal,
    decimal ValorParcela,
    int NumeroParcelas,
    int ParcelasPagas,
    DateTime DataInicio,
    DateTime DataPrimeiroVencimento,
    decimal? ValorQuitacaoAntecipada,
    StatusDivida Status,
    decimal SaldoDevedor)
{
    public static DividaResponse De(Divida d) => new(
        d.Id, d.Descricao, d.ValorTotal, d.ValorParcela, d.NumeroParcelas, d.ParcelasPagas,
        d.DataInicio, d.DataPrimeiroVencimento, d.ValorQuitacaoAntecipada, d.Status, d.SaldoDevedor);
}

public record DividaDetalheResponse(DividaResponse Divida, IReadOnlyList<MovimentacaoResponse> Parcelas);

public class DividaRequest
{
    [Required(ErrorMessage = "A descrição da dívida é obrigatória.")]
    [MaxLength(200, ErrorMessage = "A descrição aceita no máximo 200 caracteres.")]
    public string Descricao { get; set; } = string.Empty;

    [Range(0.01, 999999999999.99,
        ErrorMessage = "O valor da parcela deve ser maior que zero.")]
    public decimal ValorParcela { get; set; }

    [Range(1, 480, ErrorMessage = "A quantidade de parcelas deve ficar entre 1 e 480.")]
    public int NumeroParcelas { get; set; }

    [Required(ErrorMessage = "O vencimento da 1ª parcela é obrigatório.")]
    public DateTime? DataPrimeiroVencimento { get; set; }

    [Range(0d, 999999999999.99,
        ErrorMessage = "O valor de quitação antecipada não pode ser negativo.")]
    public decimal? ValorQuitacaoAntecipada { get; set; }

    [Required(ErrorMessage = "A categoria das parcelas é obrigatória.")]
    public Guid? CategoriaId { get; set; }

    [Required(ErrorMessage = "A forma de pagamento das parcelas é obrigatória.")]
    public Guid? FormaPagamentoId { get; set; }
}
