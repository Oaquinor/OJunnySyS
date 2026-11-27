using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OJunnySyS.Infrastructure.Persistence;

public sealed class StoreDbContextFactory : IDesignTimeDbContextFactory<StoreDbContext>
{
    public StoreDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__OjunnySyS")
            ?? "Server=localhost;Database=ojunnysy_design;User ID=design;Password=design";
        var options = new DbContextOptionsBuilder<StoreDbContext>()
            .UseMySQL(connectionString)
            .Options;
        return new StoreDbContext(options);
    }
}
