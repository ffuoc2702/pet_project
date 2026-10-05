using MatchingSports.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MatchingSports.Infrastructure.Persistence.Seed;

internal static class AdminAccountSeeder
{
    public static async Task SeedInitialAdministratorAsync(IServiceProvider services,
        MatchingSportsDbContext db, Role adminRole, CancellationToken cancellationToken)
    {
        if (adminRole.BootstrapCompleted)
            return;
        if (await db.Set<IdentityUserRole<Guid>>().AnyAsync(item => item.RoleId == adminRole.Id, cancellationToken))
        {
            adminRole.CompleteBootstrap();
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var seed = services.GetRequiredService<IOptions<SeedAdminAccountOptions>>().Value;
        var legacyEmail = services.GetRequiredService<IConfiguration>()["Auth:BootstrapAdminEmail"];
        if (!seed.Enabled && string.IsNullOrWhiteSpace(legacyEmail))
            return;
        if (!adminRole.IsActive)
            throw new InvalidOperationException("Cannot bootstrap an administrator with an inactive seed role.");
        if (seed.Enabled && !string.IsNullOrWhiteSpace(legacyEmail)
            && !string.Equals(seed.Email.Trim(), legacyEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Configure either Seed:Admin or a matching Auth:BootstrapAdminEmail, not two different bootstrap accounts.");

        var email = (seed.Enabled ? seed.Email : legacyEmail!).Trim().ToLowerInvariant();
        var manager = services.GetRequiredService<UserManager<User>>();
        var admin = await manager.FindByEmailAsync(email);
        if (admin is null)
        {
            if (!seed.Enabled)
                throw new InvalidOperationException("Bootstrap administrator must be an existing account when using Auth:BootstrapAdminEmail.");
            var normalizedEmail = manager.NormalizeEmail(email);
            if (await db.Users.IgnoreQueryFilters().AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken))
                throw new InvalidOperationException("Cannot recreate a deleted bootstrap account.");

            admin = User.CreateVerifiedAccount(email, seed.DisplayName);
            var result = await manager.CreateAsync(admin, seed.Password);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Cannot create the initial administrator: {string.Join(", ", result.Errors.Select(error => error.Code))}");
        }
        if (!admin.CanReceiveInitialAdministratorRole())
            throw new InvalidOperationException("Bootstrap administrator must be active with a verified email.");

        // Explicit one-time provisioning. Preserve existing roles and never reset a password.
        db.Add(new IdentityUserRole<Guid> { UserId = admin.Id, RoleId = adminRole.Id });
        adminRole.CompleteBootstrap();
        await db.SaveChangesAsync(cancellationToken);
    }
}
