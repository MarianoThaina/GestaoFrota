using GestaoFrota.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoFrota.Infrastructure.Data.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nome)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.GoogleId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Perfil)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        // US16: três estados, guardados como texto (AguardandoAprovacao, Ativo, Inativo).
        builder.Property(u => u.Situacao)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Ignore(u => u.Ativo);

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.GoogleId).IsUnique();
        builder.HasIndex(u => u.Situacao);
    }
}
