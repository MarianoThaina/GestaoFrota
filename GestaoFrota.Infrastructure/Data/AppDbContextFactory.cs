using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GestaoFrota.Infrastructure.Data;

/// <summary>
/// Usada apenas pelo `dotnet ef migrations` para instanciar o DbContext
/// em tempo de design, sem precisar subir a API inteira.
/// Ajuste a connection string abaixo (ou use variável de ambiente
/// ConnectionStrings__DefaultConnection) antes de rodar as migrations.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost;Database=GestaoFrota;Trusted_Connection=True;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
