using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Features.UserProfile;
using MatchingSports.Application.Features.UserSportProfile;
using MatchingSports.Domain.Entities;
using MatchingSports.Infrastructure.Persistence;
using MatchingSports.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.DemoSimulator;

internal sealed class DemoSeeder(DemoSession session, DemoState state, DemoCheckpoint checkpoint)
{
    private static readonly string[] Names = ["An", "Bình", "Chi", "Dũng", "Giang", "Hà", "Khánh", "Linh", "Minh", "Phúc"];

    public async Task SeedUsersAsync(Guid sportId, Guid formatId)
    {
        for (var number = 0; number < state.UserCount; number++)
        {
            session.Token.ThrowIfCancellationRequested();
            await using var scope = session.Provider.CreateAsyncScope();
            using var audit = scope.ServiceProvider.GetRequiredService<IAuditActorContext>().BeginScope(null);
            var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var email = Email(state.RunId, number);
            var user = await manager.FindByEmailAsync(email);
            if (user is null)
            {
                await using var transaction = await db.Database.BeginTransactionAsync(session.Token);
                user = User.CreateVerifiedAccount(email, $"Demo {Names[number % Names.Length]} {number + 1:00}");
                RequireIdentity(await manager.CreateAsync(user, session.Password));
                var role = await db.Roles.SingleAsync(role => role.SeedKey == RoleNames.User, session.Token);
                RequireIdentity(await manager.AddToRoleAsync(user, role.Name!));
                await transaction.CommitAsync(session.Token);
            }
            if (user.DeletedAt is not null || user.Status != MatchingSports.Domain.Enums.UserStatus.ACTIVE || !user.EmailConfirmed
                || !await manager.CheckPasswordAsync(user, session.Password))
                throw new DemoSimulationException("Tài khoản demo đã thay đổi/trùng hoặc mật khẩu không khớp; dùng RunId mới.");
            if (state.Resources.TryGetValue($"user-{number}", out var savedId) && savedId != user.Id)
                throw new DemoSimulationException("User trong checkpoint không khớp database.");
            session.AuthenticatedUsers.Add(user.Id);
            state.Resources[$"user-{number}"] = user.Id;
            var profileStep = $"profile-{number}";
            if (!state.CompletedSteps.Contains(profileStep))
            {
                state.ActiveStep = profileStep;
                await checkpoint.SaveAsync(state, session.Token);
                await session.SendAsync(user.Id, new UpdateUserProfileCommand(user.Profile?.DisplayName ?? $"Demo player {number + 1:00}",
                    null, number % 2 == 0 ? "Bình Thạnh, TP.HCM" : "Quận 1, TP.HCM", "Liên hệ demo sau khi trận được xác nhận"));
                if (!await db.UserSportProfiles.AnyAsync(profile => profile.UserId == user.Id && profile.SportId == sportId, session.Token))
                    await session.SendAsync(user.Id, new CreateUserSportProfileCommand(user.Id, sportId,
                        number % 3 == 0 ? "BEGINNER" : number % 3 == 1 ? "INTERMEDIATE" : "ADVANCED", [formatId], "Hồ sơ giả lập để demo MatchingSports."));
                state.CompletedSteps.Add(profileStep);
                await checkpoint.SaveAsync(state, session.Token);
            }
        }
    }

    public static string Email(string runId, int number) => $"demo.{runId}.{number + 1:000}@example.test";
    private static void RequireIdentity(IdentityResult result)
    {
        if (!result.Succeeded) throw new DemoSimulationException("Identity từ chối seed: " + string.Join(", ", result.Errors.Select(error => error.Code)));
    }
}
