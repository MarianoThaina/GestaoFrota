using GestaoFrota.Domain.Entities;
using GestaoFrota.Domain.Enums;
using GestaoFrota.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestaoFrota.API.Services;

public static class DividaRegras
{
    /// <summary>
    /// Recalcula ParcelasPagas e Status da dívida a partir das parcelas
    /// (movimentações Pagas e não estornadas). Não chama SaveChanges.
    /// </summary>
    public static async Task RecalcularAsync(AppDbContext context, Guid dividaId)
    {
        var divida = await context.Dividas.FindAsync(dividaId);
        if (divida is null)
        {
            return;
        }

        var pagas = await context.Movimentacoes.CountAsync(m =>
            m.DividaId == dividaId && m.Status == StatusMovimentacao.Pago && !m.Estornada);

        divida.ParcelasPagas = pagas;
        divida.Status = pagas >= divida.NumeroParcelas
            ? StatusDivida.Quitada
            : pagas > 0 ? StatusDivida.QuitadaParcialmente : StatusDivida.EmAberto;
    }
}
