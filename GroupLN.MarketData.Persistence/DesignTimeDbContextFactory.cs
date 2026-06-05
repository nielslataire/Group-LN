using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GroupLN.MarketData.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MarketDataDbContext>
{
    public MarketDataDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MarketDataDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=.;Database=CPM_MarketData;Trusted_Connection=True;TrustServerCertificate=True",
            sql => sql.MigrationsAssembly(typeof(MarketDataDbContext).Assembly.FullName));

        return new MarketDataDbContext(optionsBuilder.Options);
    }
}
