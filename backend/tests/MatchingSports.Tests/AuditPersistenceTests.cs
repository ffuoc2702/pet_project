using System.Text.Json;
using AutoMapper;
using MatchingSports.Api.Contracts;
using MatchingSports.Api.Controllers;
using MatchingSports.Application;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Role;
using MatchingSports.Application.Features.SystemSettings;
using MatchingSports.Application.Features.UserProfile;
using MatchingSports.Application.Features.UserSportProfile;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Infrastructure;
using MatchingSports.Infrastructure.Persistence;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace MatchingSports.Tests;

public sealed class AuditPersistenceTests
{
    [RbacDatabaseFact]
    public async Task ApiWritesStampVerifiedActorsReturnPersistedMetadataAndKeepCreatorImmutable()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("MATCHINGSPORTS_RBAC_TEST_CONNECTION")!);
        Assert.StartsWith("matchingsports_rbac_test_", connection.Database, StringComparison.Ordinal);
        connection.Database = $"matchingsports_rbac_test_aud_{Guid.NewGuid():N}";
        var clock = new AuditClock();
        var emailSender = new TestEmailSender();
        Guid currentActorId = Guid.Empty;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:matchingsports"] = connection.ConnectionString,
            ["Auth:Jwt:SigningKey"] = new string('s', 48),
            ["Auth:Otp:Pepper"] = new string('p', 48),
            ["Seed:Admin:Enabled"] = "true",
            ["Seed:Admin:Email"] = "audit-admin@example.test",
            ["Seed:Admin:Password"] = "Testing-Password123!",
            ["Seed:Admin:DisplayName"] = "Audit admin"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApplication(null, typeof(ContractMappingProfile).Assembly);
        services.AddInfrastructure(configuration);
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton<IOtpEmailSender>(emailSender);
        services.AddScoped<ICurrentUser>(_ => new TestCurrentUser(currentActorId));
        await using var provider = services.BuildServiceProvider();
        try
        {
            await provider.InitializeDatabaseAsync(true);
            Guid adminId;
            Guid registeredId;
            Guid customRoleId;
            Guid sportId;
            Guid formatId;
            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                var admin = await db.Users.SingleAsync(user => user.Email == "audit-admin@example.test");
                adminId = admin.Id;
                currentActorId = adminId;
                Assert.Null(admin.CreatedBy);
                Assert.Null(admin.UpdatedBy);
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var actor = scope.ServiceProvider.GetRequiredService<ICurrentUser>();
                var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
                var controller = new RoleController(mediator, actor, mapper)
                {
                    ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
                };
                var createdResponse = await controller.CreateRole(new CreateRoleRequest { Name = "AUDIT_REVIEWER" }, CancellationToken.None);
                var created = Assert.IsType<BaseResponse<RoleDto>>(Assert.IsType<OkObjectResult>(createdResponse.Result).Value).Data;
                customRoleId = created.Id;
                Assert.Equal(adminId, created.CreatedBy);
                Assert.Equal(adminId, created.UpdatedBy);
                Assert.Equal(clock.GetUtcNow(), created.CreatedAt);
                clock.Advance(TimeSpan.FromMinutes(1));
                var updatedResponse = await controller.UpdateRole(created.Id, new UpdateRoleRequest
                {
                    Name = created.Name,
                    Description = "Updated",
                    IsActive = true,
                    IsDefaultForRegistration = false,
                    ConcurrencyStamp = created.ConcurrencyStamp
                }, CancellationToken.None);
                var updated = Assert.IsType<BaseResponse<RoleDto>>(Assert.IsType<OkObjectResult>(updatedResponse.Result).Value).Data;
                Assert.Equal(created.CreatedAt, updated.CreatedAt);
                Assert.Equal(adminId, updated.CreatedBy);
                Assert.Equal(clock.GetUtcNow(), updated.UpdatedAt);
                Assert.Equal(adminId, updated.UpdatedBy);
                Assert.Equal(updated.UpdatedAt, (await db.Roles.AsNoTracking().SingleAsync(role => role.Id == updated.Id)).UpdatedAt);
                var roleJson = JsonSerializer.SerializeToElement(updated);
                Assert.Equal(updated.CreatedAt, roleJson.GetProperty(nameof(BaseDto.CreatedAt)).GetDateTimeOffset());
                Assert.Equal(updated.UpdatedAt, roleJson.GetProperty(nameof(BaseDto.UpdatedAt)).GetDateTimeOffset());
                Assert.Equal(adminId, roleJson.GetProperty(nameof(BaseDto.CreatedBy)).GetGuid());
                Assert.Equal(adminId, roleJson.GetProperty(nameof(BaseDto.UpdatedBy)).GetGuid());
                var roleRoundTrip = Assert.IsType<RoleDto>(JsonSerializer.Deserialize<RoleDto>(roleJson));
                Assert.Equal(updated, roleRoundTrip with { Permissions = updated.Permissions });

                clock.Advance(TimeSpan.FromMinutes(1));
                var updatedPermissions = await mediator.Send(new UpdateRolePermissionsCommand(adminId, updated.Id,
                    [PermissionCodes.ReadOwnProfile], updated.ConcurrencyStamp));
                Assert.Equal(created.CreatedAt, updatedPermissions.CreatedAt);
                Assert.Equal(adminId, updatedPermissions.CreatedBy);
                Assert.Equal(clock.GetUtcNow(), updatedPermissions.UpdatedAt);
                Assert.Equal(adminId, updatedPermissions.UpdatedBy);

                var roleQuery = new GetRolesQuery
                {
                    ActorUserId = adminId,
                    SearchString = updatedPermissions.Name,
                    FromDate = updatedPermissions.UpdatedAt,
                    ToDate = updatedPermissions.UpdatedAt
                };
                var rolePage = await mediator.Send(roleQuery);
                Assert.Equal(updatedPermissions.Id, Assert.Single(rolePage.Items).Id);
                Assert.Equal(1, rolePage.TotalItems);
                var matrix = await mediator.Send(new GetRolePermissionMatrixQuery
                {
                    ActorUserId = adminId,
                    SearchString = roleQuery.SearchString,
                    FromDate = roleQuery.FromDate,
                    ToDate = roleQuery.ToDate
                });
                Assert.Equal(updatedPermissions.Id, Assert.Single(matrix.Roles.Items).Id);
                var outsideDate = await mediator.Send(roleQuery with { FromDate = updatedPermissions.UpdatedAt.AddSeconds(1), ToDate = null });
                Assert.Empty(outsideDate.Items);
                Assert.Equal(0, outsideDate.TotalItems);
                var beforeDate = await mediator.Send(roleQuery with { FromDate = null, ToDate = updatedPermissions.UpdatedAt.AddSeconds(-1) });
                Assert.Empty(beforeDate.Items);
                Assert.Equal(0, beforeDate.TotalItems);

                var sportController = new SportController(mediator, mapper, actor)
                {
                    ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
                };
                var sportResponse = await sportController.CreateSport(new CreateSportRequest
                {
                    Code = "AUDIT_SPORT",
                    Name = "Audit sport",
                    Formats = [new SportFormatRequest { Action = "NEW", Code = "AUDIT_SINGLE", DisplayName = "Single", SideSize = 1 }]
                }, CancellationToken.None);
                var sport = Assert.IsType<BaseResponse<Application.Features.Sport.SportDto>>(Assert.IsType<OkObjectResult>(sportResponse.Result).Value).Data;
                sportId = sport.Id;
                formatId = Assert.Single(sport.Formats).Id;
                Assert.Equal(adminId, sport.CreatedBy);
                Assert.Equal(adminId, Assert.Single(sport.Formats).CreatedBy);

                var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
                using (scope.ServiceProvider.GetRequiredService<IAuditActorContext>().BeginScope(adminId))
                {
                    var registration = await identity.RegisterAsync("audit-member@example.test", "Testing-Password123!", "Member", null, CancellationToken.None);
                    registeredId = registration.UserId;
                    Assert.Equal(adminId, scope.ServiceProvider.GetRequiredService<IAuditActorContext>().ActorUserId);
                }
                var registered = await db.Users.Include(user => user.Profile).SingleAsync(user => user.Id == registeredId);
                Assert.Null(registered.CreatedBy);
                Assert.Null(registered.Profile!.CreatedBy);
                var wrongOtp = emailSender.Otp == "000000" ? "111111" : "000000";
                await Assert.ThrowsAsync<UnauthorizedException>(() => identity.VerifyEmailAsync(registered.Email!, wrongOtp, CancellationToken.None));
                Assert.Null((await db.OtpChallenges.SingleAsync()).UpdatedBy);
                await identity.VerifyEmailAsync(registered.Email!, emailSender.Otp!, CancellationToken.None);
                Assert.Equal(registeredId, registered.UpdatedBy);
                Assert.Equal(registeredId, (await db.OtpChallenges.SingleAsync()).UpdatedBy);
                await Assert.ThrowsAsync<UnauthorizedException>(() => identity.LoginAsync(registered.Email!, "Incorrect-Password123!", null, null, CancellationToken.None));
                Assert.Null(registered.UpdatedBy);
                var login = await identity.LoginAsync(registered.Email!, "Testing-Password123!", null, null, CancellationToken.None);
                var session = await db.AuthSessions.SingleAsync();
                Assert.Equal(registeredId, session.CreatedBy);
                Assert.Equal(registeredId, registered.UpdatedBy);
                clock.Advance(TimeSpan.FromMinutes(1));
                await identity.RefreshAsync(login.RefreshToken, null, null, CancellationToken.None);
                Assert.Equal(registeredId, session.UpdatedBy);
                Assert.Equal(clock.GetUtcNow(), session.UpdatedAt);
                Assert.All(await db.AuthSessions.ToListAsync(), item => Assert.Equal(registeredId, item.CreatedBy));
                Assert.Null(scope.ServiceProvider.GetRequiredService<IAuditActorContext>().ActorUserId);
            }

            currentActorId = registeredId;
            await using (var scope = provider.CreateAsyncScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var profile = await mediator.Send(new UpdateUserProfileCommand("New profile", null, null, null));
                Assert.Equal(registeredId, profile.UpdatedBy);
                Assert.Null(profile.CreatedBy);
                var sportProfile = await mediator.Send(new CreateUserSportProfileCommand(registeredId, sportId, "BEGINNER", [formatId], null));
                Assert.Equal(registeredId, sportProfile.CreatedBy);
                Assert.Equal(registeredId, sportProfile.UpdatedBy);
                clock.Advance(TimeSpan.FromMinutes(1));
                var updatedProfile = await mediator.Send(new UpdateUserSportProfileCommand(registeredId, sportProfile.Id, "BEGINNER", [formatId], "Updated bio"));
                Assert.Equal(sportProfile.CreatedAt, updatedProfile.CreatedAt);
                Assert.Equal(registeredId, updatedProfile.CreatedBy);
                Assert.Equal(registeredId, updatedProfile.UpdatedBy);
                Assert.Equal(clock.GetUtcNow(), updatedProfile.UpdatedAt);
            }

            currentActorId = adminId;
            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var assignment = await mediator.Send(new GetUserRolesQuery(adminId, registeredId));
                await mediator.Send(new ReplaceUserRolesCommand(adminId, registeredId, [customRoleId], assignment.ConcurrencyStamp!));
                var target = await db.Users.SingleAsync(user => user.Id == registeredId);
                Assert.Equal(adminId, target.UpdatedBy);
                Assert.NotEqual(target.Id, target.UpdatedBy);
                Assert.Null(target.CreatedBy);

                var settings = await mediator.Send(new GetSystemSettingsQuery(adminId));
                var updated = await mediator.Send(new UpdateSystemSettingsCommand(adminId, settings.ConcurrencyToken,
                    new SystemSettingsInput(5, 3, 10, 2, 50, 72, 100, 5, 3600, 30, 90)));
                Assert.Equal(adminId, updated.UpdatedBy);
                Assert.Null(updated.CreatedBy);
                var settingsRoundTrip = JsonSerializer.Deserialize<SystemSettingsDto>(JsonSerializer.Serialize(updated));
                Assert.Equal(updated, settingsRoundTrip);

                var notification = new NotificationHistory { UserId = registeredId, Type = NotificationType.MATCH_REMINDER, Title = "Reminder", Body = "Details" };
                var message = new OutboxMessage { AggregateType = "TEAM", AggregateId = customRoleId, EventType = "Test", PayloadJson = "{}" };
                db.AddRange(notification, message);
                await db.SaveChangesAsync();
                Assert.Null(notification.CreatedBy);
                Assert.Null(message.CreatedBy);
                Assert.Equal(notification.CreatedAt.AddDays(90), notification.PurgeAfter);
                Assert.Equal(message.CreatedAt.AddDays(30), message.PurgeAfter);

                var createdAt = notification.CreatedAt;
                var purgeAfter = notification.PurgeAfter;
                clock.Advance(TimeSpan.FromMinutes(1));
                db.Entry(notification).State = EntityState.Modified;
                db.Entry(notification).Property(item => item.CreatedAt).CurrentValue = clock.GetUtcNow().AddYears(-1);
                db.Entry(notification).Property(item => item.CreatedBy).CurrentValue = Guid.NewGuid();
                notification.Title = "Updated by worker";
                db.SaveChanges();
                Assert.Equal(createdAt, notification.CreatedAt);
                Assert.Null(notification.CreatedBy);
                Assert.Null(notification.UpdatedBy);
                Assert.Equal(clock.GetUtcNow(), notification.UpdatedAt);
                Assert.Equal(purgeAfter, notification.PurgeAfter);
            }
        }
        finally
        {
            await using var cleanupScope = provider.CreateAsyncScope();
            await cleanupScope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>().Database.EnsureDeletedAsync();
        }
    }

    private sealed class AuditClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 7, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan delay) => _now += delay;
    }

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId => userId;
    }

    private sealed class TestEmailSender : IOtpEmailSender
    {
        public string? Otp { get; private set; }
        public Task SendAsync(string email, string otp, OtpPurpose purpose, TimeSpan lifetime, CancellationToken cancellationToken)
        {
            Otp = otp;
            return Task.CompletedTask;
        }
    }
}
