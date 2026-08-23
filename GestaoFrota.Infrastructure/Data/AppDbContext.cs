using System.Reflection;
using GestaoFrota.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestaoFrota.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();
    public DbSet<Rota> Rotas => Set<Rota>();
    public DbSet<Viagem> Viagens => Set<Viagem>();
    public DbSet<Frete> Fretes => Set<Frete>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<FormaPagamento> FormasPagamento => Set<FormaPagamento>();
    public DbSet<Divida> Dividas => Set<Divida>();
    public DbSet<Movimentacao> Movimentacoes => Set<Movimentacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica todas as classes IEntityTypeConfiguration<T> deste assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        AtualizarDataAtualizacao();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AtualizarDataAtualizacao();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void AtualizarDataAtualizacao()
    {
        var entradas = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entrada in entradas)
        {
            if (entrada.Entity.GetType().GetProperty("DataAtualizacao") != null)
            {
                entrada.Property("DataAtualizacao").CurrentValue = DateTime.UtcNow;
            }
        }
    }
}
