namespace GestaoFrota.App.Helpers;

/// <summary>
/// Rotas de navegação "empurradas" (push) sobre o menu principal.
/// Registradas em AppShell via Routing.RegisterRoute.
/// </summary>
public static class Rotas
{
    public const string MovimentacaoForm = "movimentacao-form";
    public const string Comprovante = "comprovante";
    public const string DividaForm = "divida-form";
    public const string DividaDetalhe = "divida-detalhe";

    // Módulo de frota (US06, US06-b, US08)
    public const string VeiculoForm = "veiculo-form";
    public const string ManutencaoForm = "manutencao-form";
    public const string ViagemForm = "viagem-form";
    public const string EncerrarViagem = "encerrar-viagem";
}
