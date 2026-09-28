using MatchingSports.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(
        this IServiceProvider services,
        bool initialize,
        CancellationToken cancellationToken = default)
    {
        if (!initialize)
            return;

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        var sportSeeds = new[]
        {
            new SportSeed(Guid.Parse("00000000-0000-0000-0000-000000000001"), "FOOTBALL", "Bóng đá"),
            new SportSeed(Guid.Parse("00000000-0000-0000-0000-000000000002"), "BADMINTON", "Cầu lông"),
            new SportSeed(Guid.Parse("00000000-0000-0000-0000-000000000003"), "PICKLEBALL", "Pickleball")
        };

        var existingSports = await db.Sports
            .IgnoreQueryFilters()
            .ToDictionaryAsync(sport => sport.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var seed in sportSeeds)
        {
            if (existingSports.ContainsKey(seed.Code))
                continue;

            var sport = NewSport(seed.Id, seed.Code, seed.Name);
            db.Sports.Add(sport);
            existingSports.Add(sport.Code, sport);
        }

        await db.SaveChangesAsync(cancellationToken);

        var football = existingSports["FOOTBALL"];
        var badminton = existingSports["BADMINTON"];
        var pickleball = existingSports["PICKLEBALL"];
        var formatSeeds = new[]
        {
            new FormatSeed(Guid.Parse("00000000-0000-0000-0001-000000000001"), football.Id, "FOOTBALL_5V5", "Bóng đá 5v5", 5),
            new FormatSeed(Guid.Parse("00000000-0000-0000-0001-000000000002"), football.Id, "FOOTBALL_7V7", "Bóng đá 7v7", 7),
            new FormatSeed(Guid.Parse("00000000-0000-0000-0001-000000000003"), football.Id, "FOOTBALL_11V11", "Bóng đá 11v11", 11),
            new FormatSeed(Guid.Parse("00000000-0000-0000-0001-000000000004"), badminton.Id, "BADMINTON_SINGLES", "Cầu lông đơn", 1),
            new FormatSeed(Guid.Parse("00000000-0000-0000-0001-000000000005"), badminton.Id, "BADMINTON_DOUBLES", "Cầu lông đôi", 2),
            new FormatSeed(Guid.Parse("00000000-0000-0000-0001-000000000006"), pickleball.Id, "PICKLEBALL_SINGLES", "Pickleball đơn", 1),
            new FormatSeed(Guid.Parse("00000000-0000-0000-0001-000000000007"), pickleball.Id, "PICKLEBALL_DOUBLES", "Pickleball đôi", 2)
        };

        var existingFormatCodes = await db.SportFormats
            .AsNoTracking()
            .Select(format => format.Code)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var seed in formatSeeds.Where(seed => !existingFormatCodes.Contains(seed.Code)))
        {
            db.SportFormats.Add(NewFormat(
                seed.Id,
                seed.SportId,
                seed.Code,
                seed.Name,
                seed.SideSize));
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Sport NewSport(Guid id, string code, string name) => new()
    {
        Id = id,
        Code = code,
        Name = name,
        IsActive = true
    };

    private static SportFormat NewFormat(Guid id, Guid sportId, string code, string name, short sideSize) => new()
    {
        Id = id,
        SportId = sportId,
        Code = code,
        DisplayName = name,
        SideSize = sideSize,
        IsActive = true
    };

    private sealed record SportSeed(Guid Id, string Code, string Name);

    private sealed record FormatSeed(
        Guid Id,
        Guid SportId,
        string Code,
        string Name,
        short SideSize);
}
