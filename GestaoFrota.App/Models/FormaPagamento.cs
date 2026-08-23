using SQLite;

namespace GestaoFrota.App.Models;

public class FormaPagamento
{
    [PrimaryKey]
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;

    [Indexed]
    public bool PendenteSincronizacao { get; set; }
}
