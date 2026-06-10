using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GroupLN.MarketData.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MarketDataDbContext>
{
    public MarketDataDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MarketDataDbContext>();

        var connectionString =
            "Server=tcp:sql6031.site4now.net,1433;Database=db_ab5fbb_cpmmarketdata;User ID=db_ab5fbb_cpmmarketdata_admin;Password=840683P@ssword;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True;Connection Timeout=30;";

        optionsBuilder.UseSqlServer(
            connectionString,
            sql =>
            {
                sql.MigrationsAssembly(typeof(MarketDataDbContext).Assembly.FullName);
                sql.UseNetTopologySuite();
            });

        return new MarketDataDbContext(optionsBuilder.Options);
    }
}
