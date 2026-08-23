using GestaoFrota.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoFrota.Infrastructure.Data.Configurations;

public class RotaConfiguration : IEntityTypeConfiguration<Rota>
{
    public void Configure(EntityTypeBuilder<Rota> builder)
    {
        builder.ToTable("Rotas");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Nome).IsRequired().HasMaxLength(150);
        builder.Property(r => r.Origem).IsRequired().HasMaxLength(150);
        builder.Property(r => r.Destino).IsRequired().HasMaxLength(150);
        builder.Property(r => r.DistanciaEstimadaKm).HasColumnType("decimal(10,2)");
    }
}
