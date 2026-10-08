using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using MatchingSports.Api.Contracts;
using MatchingSports.Api.Controllers;
using MatchingSports.Api.ErrorHandling;
using MatchingSports.Api.Identity;
using MatchingSports.Application;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Notification;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Infrastructure;
using MatchingSports.Infrastructure.Persistence;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MatchingSports.Tests;

public sealed class NotificationPersistenceTests
{
    [RbacDatabaseFact]
    public async Task NotificationApiWorkerRetryDeduplicationReadOwnershipAndCancellationWorkTogether()
    {
        var connection = TestConnection();
        var clock = new NotificationClock();
        var sender = new FakeEmailSender();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:matchingsports"] = connection.ConnectionString,
            ["Auth:Jwt:SigningKey"] = new string('s', 48),
            ["Auth:Otp:Pepper"] = new string('p', 48),
            ["Seed:Admin:Enabled"] = "true",
            ["Seed:Admin:Email"] = "notification-admin@example.test",
            ["Seed:Admin:Password"] = "Testing-Password123!",
            ["Seed:Admin:DisplayName"] = "Notification admin",
            ["Notifications:WorkersEnabled"] = "false"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApplication(null, typeof(ContractMappingProfile).Assembly);
        services.AddInfrastructure(configuration);
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton<INotificationEmailSender>(sender);
        await using var provider = services.BuildServiceProvider();
        try
        {
            await provider.InitializeDatabaseAsync(true);
            Guid adminId, memberId, firstTeamId, secondTeamId;
            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                adminId = (await db.Users.SingleAsync(x => x.Email == "notification-admin@example.test")).Id;
                var member = User.CreateVerifiedAccount("notification-member@example.test", "Member");
                var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                Assert.True((await manager.CreateAsync(member, "Testing-Password123!")).Succeeded);
                Assert.True((await manager.AddToRoleAsync(member, RoleNames.User)).Succeeded);
                memberId = member.Id;
                var sportId = (await db.Sports.FirstAsync()).Id;
                var first = new Team { Id = Guid.NewGuid(), SportId = sportId, CaptainUserId = adminId, Name = "First" };
                var second = new Team { Id = Guid.NewGuid(), SportId = sportId, CaptainUserId = adminId, Name = "Second" };
                firstTeamId = first.Id; secondTeamId = second.Id;
                db.AddRange(first, second,
                    new TeamMember { TeamId = first.Id, UserId = memberId, JoinedAt = clock.GetUtcNow() },
                    new TeamMember { TeamId = second.Id, UserId = memberId, JoinedAt = clock.GetUtcNow() });
                await db.SaveChangesAsync();
            }

            var input = Input(memberId, clock) with
            { AudienceType = NotificationAudienceType.SELECTED_TEAMS, UserIds = [], TeamIds = [firstTeamId, secondTeamId] };
            await using (var forbidden = provider.CreateAsyncScope())
                await Assert.ThrowsAsync<ForbiddenException>(() => forbidden.ServiceProvider.GetRequiredService<IMediator>()
                    .Send(new CreateNotificationConfigCommand(memberId, input)));

            NotificationConfigDto created;
            await using (var scope = provider.CreateAsyncScope())
            {
                var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
                var controller = new NotificationConfigsController(scope.ServiceProvider.GetRequiredService<IMediator>(), new Actor(adminId), mapper)
                { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
                var request = new NotificationConfigRequest
                {
                    IsSendNow = true,
                    ScheduleType = "ONCE",
                    Critical = "WARN",
                    Title = input.Title,
                    Body = "<p onclick='bad()'>Body <b>mobile</b></p><script>bad()</script>",
                    MaxSendTimes = 1,
                    AudienceType = input.AudienceType.ToString(),
                    TeamIds = input.TeamIds,
                    InAppEnabled = true,
                    EmailEnabled = true
                };
                var previewResponse = await controller.PreviewNotificationConfig(request, new NotificationPreviewPaginationRequest { PageNumber = 1, PageSize = 1 }, CancellationToken.None);
                var preview = Assert.IsType<BaseResponse<NotificationPreviewDto>>(Assert.IsType<OkObjectResult>(previewResponse.Result).Value).Data;
                Assert.Equal(2, preview.Recipients.TotalItems);
                Assert.Single(preview.Recipients.Items);
                Assert.Single(preview.NextOccurrences);
                Assert.Contains("Body mobile", preview.BodyText);
                Assert.DoesNotContain("onclick", preview.Body);
                var result = await controller.CreateNotificationConfig(request, CancellationToken.None);
                created = Assert.IsType<BaseResponse<NotificationConfigDto>>(Assert.IsType<OkObjectResult>(result.Result).Value).Data;
                Assert.Equal(adminId, created.CreatedBy);
                Assert.Equal(NotificationConfigStatus.DRAFT, created.Status);
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                Assert.Empty(await db.NotificationHistories.ToListAsync());
                var activation = await controller.ActivateNotificationConfig(created.Id, new NotificationVersionRequest { ConcurrencyToken = created.ConcurrencyToken }, CancellationToken.None);
                var active = Assert.IsType<BaseResponse<NotificationConfigDto>>(Assert.IsType<OkObjectResult>(activation.Result).Value).Data;
                Assert.NotEqual(created.ConcurrencyToken, active.ConcurrencyToken);
            }

            var generations = await Task.WhenAll(GenerateAsync(provider), GenerateAsync(provider));
            Assert.Contains(true, generations);
            Guid memberHistoryId, adminHistoryId;
            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                var histories = await db.NotificationHistories.Where(x => x.NotificationConfigId == created.Id).ToListAsync();
                Assert.Equal(2, histories.Count);
                memberHistoryId = histories.Single(x => x.UserId == memberId).Id;
                adminHistoryId = histories.Single(x => x.UserId == adminId).Id;
                Assert.All(histories, x =>
                {
                    Assert.Equal(1, x.SendNumber);
                    Assert.Null(x.CreatedBy);
                    Assert.Equal(x.CreatedAt.AddDays(90), x.PurgeAfter);
                    Assert.Null(x.TeamId);
                    Assert.Equal(NotificationDeliveryStatus.PENDING, x.EmailStatus);
                });
                Assert.Equal(1, (await db.NotificationConfigs.SingleAsync(x => x.Id == created.Id)).SendCount);
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var historyPage = await mediator.Send(new GetNotificationHistoryQuery { ActorUserId = adminId, NotificationConfigId = created.Id, PageSize = 1 });
                Assert.Equal(2, historyPage.TotalItems);
                Assert.Single(historyPage.Items);
                var inbox = await mediator.Send(new GetNotificationsQuery { ActorUserId = memberId });
                Assert.Equal(memberHistoryId, Assert.Single(inbox.Items).Id);
                Assert.Equal(1, (await mediator.Send(new GetUnreadNotificationCountQuery(memberId))).Count);
                await Assert.ThrowsAsync<NotFoundException>(() => mediator.Send(new GetOwnNotificationQuery(memberId, adminHistoryId)));
                await Assert.ThrowsAsync<ForbiddenException>(() => mediator.Send(new GetNotificationHistoryQuery { ActorUserId = memberId }));
                await Assert.ThrowsAsync<ConflictException>(() => mediator.Send(new UpdateNotificationConfigCommand(adminId, created.Id, created.ConcurrencyToken, input)));
            }
            var reads = await Task.WhenAll(ReadAsync(provider, memberId, memberHistoryId), ReadAsync(provider, memberId, memberHistoryId));
            Assert.Equal(reads[0].ReadAt, reads[1].ReadAt);
            sender.FailuresRemaining = 1;
            Assert.True(await DispatchAsync(provider));
            Assert.True(await DispatchAsync(provider));
            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                var pending = await db.OutboxMessages.SingleAsync(x => x.ProcessedAt == null);
                Assert.Equal(1, pending.RetryCount);
                Assert.Null(pending.FailedAt);
                Assert.Equal(1, (await db.NotificationConfigs.SingleAsync(x => x.Id == created.Id)).SendCount);
                Assert.Equal(1, await db.NotificationHistories.CountAsync(x => x.EmailStatus == NotificationDeliveryStatus.SENT));
                Assert.False(await scope.ServiceProvider.GetRequiredService<NotificationJobs>().DispatchNextEmailAsync(CancellationToken.None));
            }
            clock.Advance(TimeSpan.FromSeconds(5));
            Assert.True(await DispatchAsync(provider));
            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                Assert.Equal(2, await db.NotificationHistories.CountAsync(x => x.EmailStatus == NotificationDeliveryStatus.SENT));
                Assert.Equal(2, await db.OutboxMessages.CountAsync(x => x.ProcessedAt != null));
            }
            Assert.Equal(3, sender.Keys.Count);
            Assert.Equal(2, sender.Keys.Distinct().Count());

            var emailOnly = await CreateActiveAsync(provider, adminId, Input(memberId, clock) with { InAppEnabled = false });
            Assert.True(await GenerateAsync(provider));
            await using (var scope = provider.CreateAsyncScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var config = await mediator.Send(new GetNotificationConfigQuery(adminId, emailOnly.Id));
                await mediator.Send(new CancelNotificationConfigCommand(adminId, config.Id, config.ConcurrencyToken));
                Assert.Equal(0, (await mediator.Send(new GetUnreadNotificationCountQuery(memberId))).Count);
            }
            var sendsBeforeCancel = sender.Keys.Count;
            Assert.True(await DispatchAsync(provider));
            Assert.Equal(sendsBeforeCancel, sender.Keys.Count);
            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                var cancelled = await db.NotificationHistories.SingleAsync(x => x.NotificationConfigId == emailOnly.Id);
                Assert.False(cancelled.InAppEnabled);
                Assert.Equal(NotificationDeliveryStatus.SKIPPED, cancelled.EmailStatus);
            }

            var cronInput = Input(memberId, clock) with
            { ScheduleType = NotificationScheduleType.SCHEDULED, CronString = "* * * * *", MaxSendTimes = 2, EmailEnabled = false };
            var cron = await CreateActiveAsync(provider, adminId, cronInput);
            Assert.True(await GenerateAsync(provider));
            await using (var scope = provider.CreateAsyncScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var current = await mediator.Send(new GetNotificationConfigQuery(adminId, cron.Id));
                await mediator.Send(new UpdateNotificationConfigCommand(adminId, cron.Id, current.ConcurrencyToken, cronInput with { Title = "New title" }));
            }
            clock.Advance(TimeSpan.FromMinutes(1));
            Assert.True(await GenerateAsync(provider));
            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                var histories = await db.NotificationHistories.Where(x => x.NotificationConfigId == cron.Id).OrderBy(x => x.SendNumber).ToListAsync();
                Assert.Equal(["Title", "New title"], histories.Select(x => x.Title).ToArray());
                Assert.Equal([1, 2], histories.Select(x => x.SendNumber).ToArray());
                Assert.Equal(NotificationSchedulingStatus.COMPLETED, (await db.NotificationConfigs.SingleAsync(x => x.Id == cron.Id)).SchedulingStatus);
            }

            var recovery = await CreateActiveAsync(provider, adminId, Input(memberId, clock) with { EmailEnabled = false });
            await using (var scope = provider.CreateAsyncScope())
            {
                var work = scope.ServiceProvider.GetRequiredService<INotificationWorkRepository>();
                await using var transaction = await work.BeginAsync(CancellationToken.None);
                var config = await work.LockConfigAsync(recovery.Id, CancellationToken.None);
                config!.Claim(clock.GetUtcNow(), TimeSpan.FromMinutes(5));
                await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
                await transaction.CommitAsync(CancellationToken.None);
            }
            Assert.False(await GenerateAsync(provider));
            clock.Advance(TimeSpan.FromMinutes(6));
            Assert.True(await GenerateAsync(provider));

            var expires = await CreateActiveAsync(provider, adminId, Input(memberId, clock) with
            { IsSendNow = false, AvailableAt = clock.GetUtcNow().AddMinutes(1), ExpiredAfter = clock.GetUtcNow().AddMinutes(2), EmailEnabled = false });
            clock.Advance(TimeSpan.FromMinutes(3));
            Assert.True(await GenerateAsync(provider));
            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                Assert.False(await db.NotificationHistories.AnyAsync(x => x.NotificationConfigId == expires.Id));
                Assert.Equal(NotificationSchedulingStatus.COMPLETED, (await db.NotificationConfigs.SingleAsync(x => x.Id == expires.Id)).SchedulingStatus);
                var settings = await db.SystemSettings.SingleAsync();
                settings.UpdateValues(settings.GetValues() with { OutboxMaxRetryCount = 0 });
                await db.SaveChangesAsync();
            }

            sender.FailuresRemaining = 5;
            var failed = await CreateActiveAsync(provider, adminId, Input(memberId, clock));
            Assert.True(await GenerateAsync(provider));
            Assert.True(await DispatchAsync(provider));
            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                Assert.Equal(NotificationDeliveryStatus.FAILED, (await db.NotificationHistories.SingleAsync(x => x.NotificationConfigId == failed.Id)).EmailStatus);
                Assert.NotNull((await db.OutboxMessages.SingleAsync(x => x.AggregateId == failed.Id)).FailedAt);
            }
            await using (var scope = provider.CreateAsyncScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var pushDraft = await mediator.Send(new CreateNotificationConfigCommand(adminId, Input(memberId, clock) with { PushEnabled = true }));
                await Assert.ThrowsAsync<RequestValidationException>(() => mediator.Send(new ActivateNotificationConfigCommand(adminId, pushDraft.Id, pushDraft.ConcurrencyToken)));
            }
            var broadcast = await CreateActiveAsync(provider, adminId, Input(memberId, clock) with
            { AudienceType = NotificationAudienceType.ALL_USERS, UserIds = [], EmailEnabled = false });
            Assert.True(await GenerateAsync(provider));
            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                Assert.Equal(2, await db.NotificationHistories.CountAsync(x => x.NotificationConfigId == broadcast.Id));
            }

            var corrupt = await CreateActiveAsync(provider, adminId, cronInput);
            await using (var scope = provider.CreateAsyncScope())
                await scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>().Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE notification_configs SET cron_string = 'invalid' WHERE id = {corrupt.Id}");
            Assert.True(await GenerateAsync(provider));
            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                var aborted = await db.NotificationConfigs.SingleAsync(x => x.Id == corrupt.Id);
                Assert.Equal(NotificationSchedulingStatus.ABORTED, aborted.SchedulingStatus);
                Assert.Equal("INVALID_SCHEDULE", aborted.AbortReason);
                Assert.False(await db.NotificationHistories.AnyAsync(x => x.NotificationConfigId == corrupt.Id));
            }

            sender.FailuresRemaining = 0;
            var deliveryDeadline = await CreateActiveAsync(provider, adminId, Input(memberId, clock) with
            { ExpiredAfter = clock.GetUtcNow().AddMinutes(1) });
            Assert.True(await GenerateAsync(provider));
            clock.Advance(TimeSpan.FromMinutes(2));
            var callsBeforeExpiry = sender.Keys.Count;
            Assert.True(await DispatchAsync(provider));
            Assert.Equal(callsBeforeExpiry, sender.Keys.Count);
            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                var history = await db.NotificationHistories.SingleAsync(x => x.NotificationConfigId == deliveryDeadline.Id);
                Assert.Equal(NotificationDeliveryStatus.SKIPPED, history.EmailStatus);
                // Expiry ends delivery; already published in-app history remains until retention ends.
                Assert.Equal(history.Id, (await scope.ServiceProvider.GetRequiredService<IMediator>()
                    .Send(new GetOwnNotificationQuery(memberId, history.Id))).Id);
            }
            var pendingCleanup = await CreateActiveAsync(provider, adminId, Input(memberId, clock));
            Assert.True(await GenerateAsync(provider));
            clock.Advance(TimeSpan.FromDays(91));
            await using (var scope = provider.CreateAsyncScope())
            {
                var jobs = scope.ServiceProvider.GetRequiredService<NotificationJobs>();
                Assert.True(await jobs.PurgeExpiredAsync(500, CancellationToken.None) > 0);
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                Assert.Equal(pendingCleanup.Id, Assert.Single(await db.NotificationHistories.ToListAsync()).NotificationConfigId);
                Assert.Equal(2, await db.OutboxMessages.CountAsync()); // A pending delivery keeps its history; failed messages remain.
            }
            Assert.True(await DispatchAsync(provider));
            await using (var scope = provider.CreateAsyncScope())
            {
                Assert.True(await scope.ServiceProvider.GetRequiredService<NotificationJobs>().PurgeExpiredAsync(500, CancellationToken.None) > 0);
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                Assert.Empty(await db.NotificationHistories.ToListAsync());
                Assert.Single(await db.OutboxMessages.ToListAsync());
            }
            await VerifyHttpContractsAsync(configuration, adminId, memberId);
        }
        finally
        {
            await using var scope = provider.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>().Database.EnsureDeletedAsync();
        }
    }

    [RbacDatabaseFact]
    public async Task MigrationKeepsLegacyIdsAuditReadTimesRetentionAndEscapesOldText()
    {
        var connection = TestConnection();
        var options = new DbContextOptionsBuilder<MatchingSportsDbContext>().UseNpgsql(connection.ConnectionString, x => x.UseNetTopologySuite()).Options;
        await using var db = new MatchingSportsDbContext(options);
        var migrator = db.GetService<IMigrator>();
        const string previous = "20261007084327_AddEntityAuditMetadata";
        try
        {
            await migrator.MigrateAsync(previous);
            var user = User.CreateVerifiedAccount("legacy-notification@example.test", "Legacy");
            user.NormalizedEmail = user.Email!.ToUpperInvariant(); user.NormalizedUserName = user.UserName!.ToUpperInvariant();
            await MigrationTestData.InsertUserAsync(db, user);
            var id = Guid.NewGuid();
            var createdAt = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
            var readAt = createdAt.AddHours(1);
            var purgeAfter = createdAt.AddDays(90);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO notifications (id,user_id,type,title,body,payload_json,created_at,updated_at,created_by,updated_by,read_at,purge_after)
                VALUES ({id},{user.Id},'MATCH_REMINDER','Legacy','<script>bad()</script>', jsonb_build_object(),
                        {createdAt},{readAt},{user.Id},{user.Id},{readAt},{purgeAfter})
                """);
            await migrator.MigrateAsync();
            var history = await db.NotificationHistories.SingleAsync();
            Assert.Equal(id, history.Id);
            Assert.Equal(createdAt, history.CreatedAt);
            Assert.Equal(readAt, history.ReadAt);
            Assert.Equal(readAt, history.UpdatedAt);
            Assert.Equal(user.Id, history.CreatedBy);
            Assert.Equal(purgeAfter, history.PurgeAfter);
            Assert.Equal("<script>bad()</script>", history.BodyText);
            Assert.Equal("&lt;script&gt;bad()&lt;/script&gt;", history.Body);
            Assert.Null(history.NotificationConfigId);
            Assert.True(history.InAppEnabled);
            await migrator.MigrateAsync(previous);
            await migrator.MigrateAsync();
            db.ChangeTracker.Clear();
            Assert.Equal(id, (await db.NotificationHistories.SingleAsync()).Id);
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }

    private static NpgsqlConnectionStringBuilder TestConnection()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("MATCHINGSPORTS_RBAC_TEST_CONNECTION")!);
        Assert.StartsWith("matchingsports_rbac_test_", connection.Database, StringComparison.Ordinal);
        connection.Database = $"matchingsports_rbac_test_not_{Guid.NewGuid():N}";
        return connection;
    }

    private static async Task VerifyHttpContractsAsync(IConfiguration configuration, Guid adminId, Guid memberId)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddConfiguration(configuration);
        builder.Logging.ClearProviders();
        builder.Services.AddApplication(null, typeof(ContractMappingProfile).Assembly);
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddCurrentUser();
        builder.Services.AddControllers().AddApplicationPart(typeof(NotificationsController).Assembly)
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        await using var app = builder.Build();
        app.Urls.Add("http://127.0.0.1:0");
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            json.Converters.Add(new JsonStringEnumConverter());
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/notifications")).StatusCode);
            await using var scope = app.Services.CreateAsyncScope();
            var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
            var adminTokens = await identity.LoginAsync("notification-admin@example.test", "Testing-Password123!", null, null, CancellationToken.None);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);
            var request = new
            {
                isSendNow = true,
                scheduleType = "ONCE",
                critical = "WARN",
                title = "HTTP notification",
                body = "<p>Mobile body</p>",
                maxSendTimes = 1,
                audienceType = "SELECTED_USERS",
                userIds = new[] { adminId },
                inAppEnabled = true,
                metadata = new { SchemaVersion = 1, ActionType = "VIEW_ENTITY", EntityType = "USER", EntityId = adminId }
            };
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/notification-configs", new { title = "Invalid", scheduleType = "INVALID" })).StatusCode);
            var createdResponse = await client.PostAsJsonAsync("/api/v1/notification-configs", request);
            Assert.Equal(HttpStatusCode.OK, createdResponse.StatusCode);
            var created = (await createdResponse.Content.ReadFromJsonAsync<BaseResponse<NotificationConfigDto>>(json))!.Data;
            Assert.Equal(JsonValueKind.Object, created.Metadata.ValueKind);
            Assert.Equal(adminId, created.CreatedBy);
            var preview = await client.PostAsJsonAsync("/api/v1/notification-configs/preview?pageNumber=1&pageSize=1", request);
            Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
            Assert.Equal(1, (await preview.Content.ReadFromJsonAsync<BaseResponse<NotificationPreviewDto>>(json))!.Data.Recipients.TotalItems);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/notification-configs?pageNumber=0")).StatusCode);
            var list = await client.GetFromJsonAsync<PaginatedResponse<NotificationConfigDto>>("/api/v1/notification-configs?pageSize=1&searchString=HTTP%20notification", json);
            Assert.Equal(created.Id, Assert.Single(list!.Data).Id);
            Assert.Equal(1, list.Pagination.TotalItems);
            var activation = await client.PostAsJsonAsync($"/api/v1/notification-configs/{created.Id}/activate", new { created.ConcurrencyToken });
            Assert.Equal(HttpStatusCode.OK, activation.StatusCode);
            var active = (await activation.Content.ReadFromJsonAsync<BaseResponse<NotificationConfigDto>>(json))!.Data;
            Assert.True(await scope.ServiceProvider.GetRequiredService<NotificationJobs>().GenerateNextOccurrenceAsync(CancellationToken.None));
            var inbox = await client.GetFromJsonAsync<PaginatedResponse<NotificationHistoryDto>>($"/api/v1/notifications?notificationConfigId={created.Id}", json);
            var history = Assert.Single(inbox!.Data);
            Assert.Equal("Mobile body", history.BodyText);
            Assert.Equal(adminId, history.Metadata.GetProperty("EntityId").GetGuid());
            Assert.Equal(adminId, history.EntityId);
            var read = await client.PostAsync($"/api/v1/notifications/{history.Id}/read", null);
            Assert.Equal(HttpStatusCode.OK, read.StatusCode);
            var readHistory = (await read.Content.ReadFromJsonAsync<BaseResponse<NotificationHistoryDto>>(json))!.Data;
            Assert.NotNull(readHistory.ReadAt);
            Assert.Equal(adminId, readHistory.UpdatedBy);
            var again = await client.PostAsync($"/api/v1/notifications/{history.Id}/read", null);
            var persistedReadAt = (await again.Content.ReadFromJsonAsync<BaseResponse<NotificationHistoryDto>>(json))!.Data.ReadAt;
            Assert.NotNull(persistedReadAt);
            // PostgreSQL stores microseconds, while .NET timestamps include 100 ns ticks.
            Assert.InRange((persistedReadAt.Value - readHistory.ReadAt!.Value).Duration(), TimeSpan.Zero, TimeSpan.FromMicroseconds(1));
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/notifications/unread-count")).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/v1/notification-configs/{created.Id}/cancel", new { created.ConcurrencyToken })).StatusCode);
            var latest = (await client.GetFromJsonAsync<BaseResponse<NotificationConfigDto>>($"/api/v1/notification-configs/{created.Id}", json))!.Data;
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/v1/notification-configs/{created.Id}/cancel", new { latest.ConcurrencyToken })).StatusCode);

            var memberTokens = await identity.LoginAsync("notification-member@example.test", "Testing-Password123!", null, null, CancellationToken.None);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberTokens.AccessToken);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/notification-configs")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/notification-configs", request)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/notifications/{history.Id}")).StatusCode);
            var spoofed = await client.GetFromJsonAsync<PaginatedResponse<NotificationHistoryDto>>($"/api/v1/notifications?adminView=true&actorUserId={adminId}", json);
            Assert.Empty(spoofed!.Data);
            Assert.NotEqual(adminId, memberId);
        }
        finally { await app.StopAsync(); }
    }
    private static NotificationInput Input(Guid userId, NotificationClock clock) => new(true, NotificationScheduleType.ONCE, null,
        NotificationCritical.INFO, "Title", "<p>Body</p>", clock.GetUtcNow(), null, 1,
        NotificationAudienceType.SELECTED_USERS, [userId], [], true, true, false, false, "{\"SchemaVersion\":1}", "Asia/Ho_Chi_Minh");

    private static async Task<NotificationConfigDto> CreateActiveAsync(ServiceProvider provider, Guid actorId, NotificationInput input)
    {
        await using var scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var config = await mediator.Send(new CreateNotificationConfigCommand(actorId, input));
        return await mediator.Send(new ActivateNotificationConfigCommand(actorId, config.Id, config.ConcurrencyToken));
    }
    private static async Task<bool> GenerateAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<NotificationJobs>().GenerateNextOccurrenceAsync(CancellationToken.None);
    }
    private static async Task<bool> DispatchAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<NotificationJobs>().DispatchNextEmailAsync(CancellationToken.None);
    }
    private static async Task<NotificationHistoryDto> ReadAsync(ServiceProvider provider, Guid userId, Guid id)
    {
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new MarkNotificationReadCommand(userId, id));
    }
    private sealed class Actor(Guid userId) : ICurrentUser { public Guid UserId => userId; }
    private sealed class NotificationClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 7, 3, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan interval) => _now += interval;
    }
    private sealed class FakeEmailSender : INotificationEmailSender
    {
        public bool IsConfigured => true;
        public int FailuresRemaining { get; set; }
        public ConcurrentBag<string> Keys { get; } = [];
        public Task SendAsync(string email, string title, string html, string idempotencyKey, CancellationToken cancellationToken)
        {
            Keys.Add(idempotencyKey);
            if (FailuresRemaining-- > 0) throw new NotificationDeliveryException(ErrorCodes.EmailSendFailed);
            return Task.CompletedTask;
        }
    }
}
