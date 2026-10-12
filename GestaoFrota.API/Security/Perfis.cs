namespace GestaoFrota.API.Security;

/// <summary>
/// Nomes de perfil usados nas claims de role do JWT (RBAC).
/// Mantidos como string para uso direto em [Authorize(Roles = "...")].
/// </summary>
public static class Perfis
{
    public const string Administrador = "Administrador";
    public const string GestorDeFrota = "GestorDeFrota";
    public const string Financeiro = "Financeiro";
    public const string OperadorMotorista = "OperadorMotorista";

    /// <summary>Perfis com permissão de gestão financeira.</summary>
    public const string GestaoFinanceira = Administrador + "," + Financeiro;

    /// <summary>
    /// Perfis que podem consultar o módulo financeiro (movimentações, dívidas,
    /// saldo e projeção). A escrita continua restrita a <see cref="GestaoFinanceira"/>.
    /// O OperadorMotorista nunca tem acesso a esses dados.
    /// </summary>
    public const string LeituraFinanceira = Administrador + "," + GestorDeFrota + "," + Financeiro;

    /// <summary>Perfis com permissão de gestão de frota.</summary>
    public const string GestaoFrota = Administrador + "," + GestorDeFrota;

    /// <summary>Todos os perfis com acesso autenticado padrão.</summary>
    public const string Todos = Administrador + "," + GestorDeFrota + "," + Financeiro + "," + OperadorMotorista;
}
