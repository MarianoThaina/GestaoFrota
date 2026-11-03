namespace GestaoFrota.Domain.Enums;

/// <summary>
/// Situação do cadastro (US16). Quem entra pela primeira vez fica
/// "Aguardando Aprovação" até um Administrador liberar e definir o perfil.
/// </summary>
public enum SituacaoUsuario
{
    AguardandoAprovacao = 1,
    Ativo = 2,
    Inativo = 3
}
