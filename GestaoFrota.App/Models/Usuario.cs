namespace GestaoFrota.App.Models;

/// <summary>Espelha GestaoFrota.Domain.Enums.SituacaoUsuario (US16).</summary>
public enum SituacaoUsuario
{
    AguardandoAprovacao = 1,
    Ativo = 2,
    Inativo = 3
}

public class Usuario
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? FotoUrl { get; set; }

    public PerfilUsuario Perfil { get; set; }

    public SituacaoUsuario Situacao { get; set; }

    public bool Ativo { get; set; }

    public DateTime? UltimoLogin { get; set; }

    public DateTime DataCriacao { get; set; }

    // ---------- Apresentação ----------

    public bool AguardandoAprovacao => Situacao == SituacaoUsuario.AguardandoAprovacao;

    public bool Inativo => Situacao == SituacaoUsuario.Inativo;

    public string SituacaoTexto => Situacao switch
    {
        SituacaoUsuario.AguardandoAprovacao => "Aguardando aprovação",
        SituacaoUsuario.Inativo => "Inativo",
        _ => "Ativo"
    };

    public Color CorSituacao => Situacao switch
    {
        SituacaoUsuario.AguardandoAprovacao => Color.FromArgb("#E8710A"),
        SituacaoUsuario.Inativo => Color.FromArgb("#D93025"),
        _ => Color.FromArgb("#1E8E3E")
    };

    public string BotaoSituacaoTexto => Situacao == SituacaoUsuario.Ativo ? "Desativar usuário" : "Reativar usuário";

    public string CadastroTexto => $"Cadastrado em {DataCriacao:dd/MM/yyyy}";
}
