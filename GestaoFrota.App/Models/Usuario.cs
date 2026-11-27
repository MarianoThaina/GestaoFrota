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

    /// <summary>A conta tem senha (entra por e-mail e senha).</summary>
    public bool TemSenha { get; set; }

    /// <summary>A conta já entrou pelo Google pelo menos uma vez.</summary>
    public bool TemGoogle { get; set; }

    /// <summary>"Último login: 27/11/2026 15:37", ou aviso de que a conta nunca entrou.</summary>
    public string UltimoLoginTexto => UltimoLogin is { } quando
        ? $"Último login: {quando.ToLocalTime():dd/MM/yyyy HH:mm}"
        : "Ainda não entrou";

    /// <summary>Texto da tela de Usuários: por onde a pessoa entra.</summary>
    public string FormasDeEntrar => (TemSenha, TemGoogle) switch
    {
        (true, true) => "Entra com: e-mail e senha ou Google",
        (true, false) => "Entra com: e-mail e senha",
        (false, true) => "Entra com: Google",
        _ => "Ainda sem forma de entrar"
    };
}
