namespace GestaoFrota.Domain.Enums;

/// <summary>
/// Perfis de acesso configuráveis do sistema (RBAC),
/// conforme especificado no documento de requisitos.
/// </summary>
public enum PerfilUsuario
{
    Administrador = 1,
    GestorDeFrota = 2,
    Financeiro = 3,
    OperadorMotorista = 4
}
