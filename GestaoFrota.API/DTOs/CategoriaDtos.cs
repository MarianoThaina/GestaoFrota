using System.ComponentModel.DataAnnotations;
using GestaoFrota.Domain.Enums;

namespace GestaoFrota.API.DTOs;

public record CategoriaResponse(Guid Id, string Nome, TipoCategoria Tipo, bool Ativo);

public class CategoriaRequest
{
    [Required(ErrorMessage = "O nome da categoria é obrigatório.")]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O tipo da categoria é obrigatório (Receita ou Despesa).")]
    public TipoCategoria Tipo { get; set; }

    public bool Ativo { get; set; } = true;
}
