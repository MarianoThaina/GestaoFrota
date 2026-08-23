using System.ComponentModel.DataAnnotations;

namespace GestaoFrota.API.DTOs;

public record FormaPagamentoResponse(Guid Id, string Nome, bool Ativo);

public class FormaPagamentoRequest
{
    [Required(ErrorMessage = "O nome da forma de pagamento é obrigatório.")]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;
}
