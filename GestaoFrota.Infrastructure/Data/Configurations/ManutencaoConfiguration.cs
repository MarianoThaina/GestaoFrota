using GestaoFrota.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoFrota.Infrastructure.Data.Configurations;

public class ManutencaoConfiguration : IEntityTypeConfiguration<Manutencao>
{
    public void Configure(EntityTypeBuilder<Manutencao> builder)
    {
        builder.ToTable("Manutencoes", t => t.HasCheckConstraint(
            "CK_Manutencao_ValorPositivo",
            "[ValorTotal] > 0"));

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Descricao)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(m => m.ValorTotal).HasColumnType("decimal(14,2)");

        builder.Ignore(m => m.Concluida);

        // Veículo com histórico de manutenção não pode ser apagado fisicamente.
        builder.HasOne(m => m.Veiculo)
            .WithMany(v => v.Manutencoes)
            .HasForeignKey(m => m.VeiculoId)
            .OnDelete(DeleteBehavior.Restrict);

        // A despesa gerada pode ser estornada/removida sem perder o registro da oficina.
        builder.HasOne(m => m.Movimentacao)
            .WithMany()
            .HasForeignKey(m => m.MovimentacaoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => m.VeiculoId);
    }
}
