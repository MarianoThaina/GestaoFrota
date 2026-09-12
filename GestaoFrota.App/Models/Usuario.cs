namespace GestaoFrota.App.Models;

public class Usuario
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? FotoUrl { get; set; }

    public PerfilUsuario Perfil { get; set; }

    public bool Ativo { get; set; }

    public DateTime? UltimoLogin { get; set; }
}
