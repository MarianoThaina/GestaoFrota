using SQLite;

namespace GestaoFrota.App.Models;

public enum TipoCategoria
{
    Receita = 1,
    Despesa = 2
}

/// <summary>
/// [PrimaryKey]/[Indexed] permitem que a sqlite-net use esta mesma classe
/// como tabela local de cache (arquitetura Offline-First).
/// </summary>
public class Categoria
{
    [PrimaryKey]
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public TipoCategoria Tipo { get; set; }

    public bool Ativo { get; set; } = true;

    /// <summary>
    /// True quando o registro foi criado/editado offline e ainda
    /// não foi confirmado pela API.
    /// </summary>
    [Indexed]
    public bool PendenteSincronizacao { get; set; }
}
