using GestaoFrota.Domain.Entities;
using GestaoFrota.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoFrota.Infrastructure.Data.Configurations;

public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("Categorias");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nome).IsRequired().HasMaxLength(100);

        builder.Property(c => c.Tipo)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(c => new { c.Nome, c.Tipo }).IsUnique();

        // Categorias padrão (US01): datas fixas exigidas pelo HasData —
        // não pode usar DateTime.UtcNow aqui, senão a migration muda a
        // cada "dotnet ef migrations add".
        var dataSeed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            new Categoria { Id = Guid.Parse("00000000-0000-0000-0001-000000000001"), Nome = "Frete", Tipo = TipoCategoria.Receita, Ativo = true, DataCriacao = dataSeed },
            new Categoria { Id = Guid.Parse("00000000-0000-0000-0001-000000000002"), Nome = "Combustível", Tipo = TipoCategoria.Despesa, Ativo = true, DataCriacao = dataSeed },
            new Categoria { Id = Guid.Parse("00000000-0000-0000-0001-000000000003"), Nome = "Pedágio", Tipo = TipoCategoria.Despesa, Ativo = true, DataCriacao = dataSeed },
            new Categoria { Id = Guid.Parse("00000000-0000-0000-0001-000000000004"), Nome = "Manutenção", Tipo = TipoCategoria.Despesa, Ativo = true, DataCriacao = dataSeed },
            new Categoria { Id = Guid.Parse("00000000-0000-0000-0001-000000000005"), Nome = "Salários", Tipo = TipoCategoria.Despesa, Ativo = true, DataCriacao = dataSeed },
            new Categoria { Id = Guid.Parse("00000000-0000-0000-0001-000000000006"), Nome = "Financiamento", Tipo = TipoCategoria.Despesa, Ativo = true, DataCriacao = dataSeed }
        );
    }
}
