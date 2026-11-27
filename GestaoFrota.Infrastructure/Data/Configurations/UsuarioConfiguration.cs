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

        // Opcional: conta só com senha não tem GoogleId. O índice único abaixo
        // ignora os nulos (o SQL Server recebe o filtro [GoogleId] IS NOT NULL).
        builder.Property(u => u.GoogleId)
            .HasMaxLength(100);

        builder.Property(u => u.SenhaHash)
            .HasMaxLength(500);

        builder.Property(u => u.Perfil)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.GoogleId).IsUnique();
    }
}
