using MatchingSports.Domain.Entities;
using MatchingSports.Infrastructure.Persistence.Seed;
using MatchingSports.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // Serialize seed runners; EF's migration lock does not cover custom seed operations.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(741963852)", cancellationToken);
        await SeedRolesAsync(scope.ServiceProvider, db, cancellationToken);

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
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task SeedRolesAsync(
        IServiceProvider services,
        MatchingSportsDbContext db,
        CancellationToken cancellationToken)
    {
        var roles = await db.Roles.ToListAsync(cancellationToken);
        var initializeLegacyMemberships = false;
        foreach (var roleName in new[] { RoleNames.Admin, RoleNames.User })
        {
            var role = roles.SingleOrDefault(item => item.SeedKey == roleName)
                ?? roles.SingleOrDefault(item => item.SeedKey == null && item.NormalizedName == roleName);
            if (role is null)
            {
                role = Role.Create(roleName, isDefaultForRegistration: roleName == RoleNames.User);
                db.Roles.Add(role);
                roles.Add(role);
            }
            if (roleName == RoleNames.User && !role.RegistrationDefaultInitialized)
                initializeLegacyMemberships = true;
            role.InitializeSeedIdentity(roleName);
            role.InitializeRegistrationDefault(roleName == RoleNames.User);
        }

        await db.SaveChangesAsync(cancellationToken);
        await SeedRolePermissionsAsync(db, cancellationToken);

        var userRole = roles.Single(role => role.SeedKey == RoleNames.User);
        var unassignedUserIds = initializeLegacyMemberships ? await db.Users
            .Where(user => !db.Set<IdentityUserRole<Guid>>().Any(item => item.UserId == user.Id))
            .Select(user => user.Id).ToArrayAsync(cancellationToken) : [];
        foreach (var userId in unassignedUserIds)
            db.Add(new IdentityUserRole<Guid> { UserId = userId, RoleId = userRole.Id });
        await db.SaveChangesAsync(cancellationToken);

        var adminRole = roles.Single(role => role.SeedKey == RoleNames.Admin);
        await AdminAccountSeeder.SeedInitialAdministratorAsync(services, db, adminRole, cancellationToken);
    }

    private static async Task SeedRolePermissionsAsync(MatchingSportsDbContext db, CancellationToken cancellationToken)
    {
        var definitions = InitialPermissionCatalog.All;
        var definitionsByCode = definitions.ToDictionary(definition => definition.Code, StringComparer.Ordinal);
        var permissions = await db.Permissions.ToDictionaryAsync(item => item.Code, cancellationToken);
        var addedCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var definition in definitions)
        {
            if (permissions.TryGetValue(definition.Code, out var existing))
            {
                existing.UpdateDisplayName(definition.Name);
                continue;
            }
            var permission = Permission.Create(definition.Code, definition.Name);
            db.Permissions.Add(permission);
            permissions.Add(definition.Code, permission);
            addedCodes.Add(definition.Code);
        }

        var roles = await db.Roles.Include(role => role.Permissions).ThenInclude(link => link.Permission).ToListAsync(cancellationToken);
        foreach (var role in roles.Where(role => role.SeedKey is not null))
        {
            var initialCodes = InitialPermissionCatalog.GetDefaultPermissionCodes(role.SeedKey!).ToArray();
            role.InitializeDefaultPermissions(initialCodes.Select(code => permissions[code]));
            // Grant newly introduced seed permissions once; never restore a revoked grant.
            foreach (var code in initialCodes.Where(code => addedCodes.Contains(code) &&
                definitionsByCode[code].LegacyReadCode is null))
                role.GrantPermission(permissions[code]);
        }

        // Split the old combined grant once, including custom roles. Revoked old grants stay revoked.
        foreach (var definition in definitions.Where(definition => addedCodes.Contains(definition.Code) && definition.LegacyReadCode is not null))
        {
            foreach (var role in roles.Where(role => role.Permissions.Any(link => link.Permission.Code == definition.LegacyReadCode)))
                role.GrantPermission(permissions[definition.Code]);
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
