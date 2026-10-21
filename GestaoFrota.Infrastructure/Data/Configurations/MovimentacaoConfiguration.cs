using GestaoFrota.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoFrota.Infrastructure.Data.Configurations;

public class MovimentacaoConfiguration : IEntityTypeConfiguration<Movimentacao>
{
    public void Configure(EntityTypeBuilder<Movimentacao> builder)
    {
        builder.ToTable("Movimentacoes", t =>
        {
            t.HasCheckConstraint(
                "CK_Movimentacao_ValorPositivo",
                "[Valor] > 0");

            // Se está Pago, a data de pagamento é obrigatória (US03).
            t.HasCheckConstraint(
                "CK_Movimentacao_PagoExigeDataPagamento",
                "[Status] <> 'Pago' OR [DataPagamento] IS NOT NULL");
        });

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Valor).HasColumnType("decimal(14,2)");
        builder.Property(m => m.Descricao).HasMaxLength(300);
        builder.Property(m => m.ComprovanteUrl).HasMaxLength(500);
        builder.Property(m => m.ComprovanteConteudo).HasColumnType("varbinary(max)");
        builder.Property(m => m.ComprovanteTipo).HasMaxLength(100);
        builder.Property(m => m.QuantidadeLitros).HasColumnType("decimal(10,2)");

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.Tipo)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(m => m.Categoria)
            .WithMany(c => c.Movimentacoes)
            .HasForeignKey(m => m.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.FormaPagamento)
            .WithMany(f => f.Movimentacoes)
            .HasForeignKey(m => m.FormaPagamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Usuario)
            .WithMany(u => u.Movimentacoes)
            .HasForeignKey(m => m.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Viagem)
            .WithMany(v => v.Movimentacoes)
            .HasForeignKey(m => m.ViagemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(m => m.Divida)
            .WithMany(d => d.Movimentacoes)
            .HasForeignKey(m => m.DividaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => m.Data);
        builder.HasIndex(m => new { m.Status, m.DataVencimento });
    }
}
