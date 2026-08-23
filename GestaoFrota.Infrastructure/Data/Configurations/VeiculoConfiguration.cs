using GestaoFrota.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoFrota.Infrastructure.Data.Configurations;

public class VeiculoConfiguration : IEntityTypeConfiguration<Veiculo>
{
    public void Configure(EntityTypeBuilder<Veiculo> builder)
    {
        builder.ToTable("Veiculos");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Placa)
            .IsRequired()
            .HasMaxLength(8);

        builder.Property(v => v.Modelo)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.Marca)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.KmAtual)
            .HasColumnType("decimal(12,2)");

        builder.Property(v => v.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Placa é única na frota
        builder.HasIndex(v => v.Placa).IsUnique();
    }
}
