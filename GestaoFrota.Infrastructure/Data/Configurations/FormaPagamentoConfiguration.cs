using GestaoFrota.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoFrota.Infrastructure.Data.Configurations;

public class FormaPagamentoConfiguration : IEntityTypeConfiguration<FormaPagamento>
{
    public void Configure(EntityTypeBuilder<FormaPagamento> builder)
    {
        builder.ToTable("FormasPagamento");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Nome).IsRequired().HasMaxLength(100);

        builder.HasIndex(f => f.Nome).IsUnique();

        var dataSeed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            new FormaPagamento { Id = Guid.Parse("00000000-0000-0000-0002-000000000001"), Nome = "Pix", Ativo = true, DataCriacao = dataSeed },
            new FormaPagamento { Id = Guid.Parse("00000000-0000-0000-0002-000000000002"), Nome = "Boleto", Ativo = true, DataCriacao = dataSeed },
            new FormaPagamento { Id = Guid.Parse("00000000-0000-0000-0002-000000000003"), Nome = "Transferência TED", Ativo = true, DataCriacao = dataSeed },
            new FormaPagamento { Id = Guid.Parse("00000000-0000-0000-0002-000000000004"), Nome = "Cartão Corporativo", Ativo = true, DataCriacao = dataSeed }
        );
    }
}
