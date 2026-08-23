using GestaoFrota.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoFrota.Infrastructure.Data.Configurations;

public class FreteConfiguration : IEntityTypeConfiguration<Frete>
{
    public void Configure(EntityTypeBuilder<Frete> builder)
    {
        builder.ToTable("Fretes");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Cliente).IsRequired().HasMaxLength(150);
        builder.Property(f => f.DescricaoCarga).HasMaxLength(300);
        builder.Property(f => f.PesoCargaKg).HasColumnType("decimal(12,2)");
        builder.Property(f => f.ValorFrete).HasColumnType("decimal(14,2)");

        builder.HasIndex(f => f.ViagemId).IsUnique();
    }
}
