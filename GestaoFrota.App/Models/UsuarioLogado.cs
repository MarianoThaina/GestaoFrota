namespace GestaoFrota.App.Models;

public class UsuarioLogado
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FotoUrl { get; set; }
    public PerfilUsuario Perfil { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiraEm { get; set; }
    public Guid UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FotoUrl { get; set; }
    public PerfilUsuario Perfil { get; set; }
}
