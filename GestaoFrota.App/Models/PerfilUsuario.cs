namespace GestaoFrota.App.Models;

/// <summary>
/// Espelha GestaoFrota.Domain.Enums.PerfilUsuario do backend.
/// Usado no app para decidir quais telas/menus exibir (RBAC).
/// </summary>
public enum PerfilUsuario
{
    Administrador = 1,
    GestorDeFrota = 2,
    Financeiro = 3,
    OperadorMotorista = 4
}
