using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MatchingSports.Infrastructure.Persistence;

public sealed class MatchingSportsDbContextFactory : IDesignTimeDbContextFactory<MatchingSportsDbContext>
{
    public MatchingSportsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("MATCHINGSPORTS_DESIGN_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=matchingsports;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<MatchingSportsDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite())
            .Options;

        return new MatchingSportsDbContext(options);
    }
}
