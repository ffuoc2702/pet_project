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
            throw new InvalidOperationException("Không thể khởi tạo quản trị viên bằng vai trò mặc định đã vô hiệu hóa.");
        if (seed.Enabled && !string.IsNullOrWhiteSpace(legacyEmail)
            && !string.Equals(seed.Email.Trim(), legacyEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Cấu hình Seed:Admin hoặc Auth:BootstrapAdminEmail phải dùng cùng một tài khoản khởi tạo quản trị viên.");

        var email = (seed.Enabled ? seed.Email : legacyEmail!).Trim().ToLowerInvariant();
        var manager = services.GetRequiredService<UserManager<User>>();
        var admin = await manager.FindByEmailAsync(email);
        if (admin is null)
        {
            if (!seed.Enabled)
                throw new InvalidOperationException("Tài khoản quản trị viên phải tồn tại khi sử dụng Auth:BootstrapAdminEmail.");
            var normalizedEmail = manager.NormalizeEmail(email);
            if (await db.Users.IgnoreQueryFilters().AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken))
                throw new InvalidOperationException("Không thể tạo lại tài khoản quản trị viên khởi tạo đã bị xóa.");

            admin = User.CreateVerifiedAccount(email, seed.DisplayName);
            var result = await manager.CreateAsync(admin, seed.Password);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Không thể tạo quản trị viên ban đầu: {string.Join(", ", result.Errors.Select(error => error.Code))}");
        }
        if (!admin.CanReceiveInitialAdministratorRole())
            throw new InvalidOperationException("Tài khoản quản trị viên khởi tạo phải đang hoạt động và đã xác minh email.");

        // Explicit one-time provisioning. Preserve existing roles and never reset a password.
        db.Add(new IdentityUserRole<Guid> { UserId = admin.Id, RoleId = adminRole.Id });
        adminRole.CompleteBootstrap();
        await db.SaveChangesAsync(cancellationToken);
    }
}
