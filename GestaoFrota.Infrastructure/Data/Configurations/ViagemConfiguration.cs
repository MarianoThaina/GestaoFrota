using GestaoFrota.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoFrota.Infrastructure.Data.Configurations;

public class ViagemConfiguration : IEntityTypeConfiguration<Viagem>
{
    public void Configure(EntityTypeBuilder<Viagem> builder)
    {
        builder.ToTable("Viagens", t => t.HasCheckConstraint(
            "CK_Viagem_HodometroFinalMaiorQueInicial",
            "[HodometroFinal] IS NULL OR [HodometroFinal] > [HodometroInicial]"));

        builder.HasKey(v => v.Id);

        builder.Property(v => v.HodometroInicial).HasColumnType("decimal(12,2)");
        builder.Property(v => v.HodometroFinal).HasColumnType("decimal(12,2)");

        builder.Property(v => v.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(v => v.Observacoes).HasMaxLength(500);

        builder.Ignore(v => v.DistanciaPercorridaKm);

        builder.HasOne(v => v.Veiculo)
            .WithMany(veic => veic.Viagens)
            .HasForeignKey(v => v.VeiculoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Rota)
            .WithMany(r => r.Viagens)
            .HasForeignKey(v => v.RotaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Motorista)
            .WithMany(u => u.Viagens)
            .HasForeignKey(v => v.MotoristaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Frete)
            .WithOne(f => f.Viagem)
            .HasForeignKey<Frete>(f => f.ViagemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
