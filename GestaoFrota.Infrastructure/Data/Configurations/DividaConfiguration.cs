using GestaoFrota.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoFrota.Infrastructure.Data.Configurations;

public class DividaConfiguration : IEntityTypeConfiguration<Divida>
{
    public void Configure(EntityTypeBuilder<Divida> builder)
    {
        builder.ToTable("Dividas");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Descricao).IsRequired().HasMaxLength(200);
        builder.Property(d => d.ValorTotal).HasColumnType("decimal(14,2)");

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Ignore(d => d.ValorParcela);
        builder.Ignore(d => d.SaldoDevedor);
    }
}
