using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using MatchingSports.Api.Contracts;
using MatchingSports.Api.Controllers;
using MatchingSports.Application;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.SystemSettings;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Domain.ValueObjects;
using MatchingSports.Infrastructure.Persistence.Seed;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.Tests;

public sealed class SystemSettingsTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingPermissionOrActorCannotReadOrWrite(bool missingActor)
    {
        var repository = new StubRepository(missingActor ? [PermissionCodes.ReadSystemSettings, PermissionCodes.UpdateSystemSettings] : []);
        using var provider = CreateProvider(repository);
        var mediator = provider.GetRequiredService<IMediator>();
        var actorId = missingActor ? Guid.Empty : repository.Actor.UserId;
        await Assert.ThrowsAsync<ForbiddenException>(() => mediator.Send(new GetSystemSettingsQuery(actorId)));
        await Assert.ThrowsAsync<ForbiddenException>(() => mediator.Send(new UpdateSystemSettingsCommand(actorId,
            repository.Settings!.ConcurrencyToken, Input())));
        Assert.Equal(0, repository.ReadCount);
        Assert.Equal(0, repository.SaveCount);
        Assert.Empty(repository.AuditEntries);
    }

    [Fact]
    public async Task ReadRequiresOnlyReadPermissionAndDoesNotCommit()
    {
        var repository = new StubRepository(PermissionCodes.ReadSystemSettings);
        using var provider = CreateProvider(repository);
        using var cancellation = new CancellationTokenSource();
        var result = await provider.GetRequiredService<IMediator>().Send(new GetSystemSettingsQuery(repository.Actor.UserId), cancellation.Token);
        Assert.Equal(50, result.MaxMembersPerTeam);
        Assert.Equal(repository.Settings!.ConcurrencyToken, result.ConcurrencyToken);
        Assert.Equal(cancellation.Token, repository.LastCancellationToken);
        Assert.Equal(0, repository.SaveCount);
        await Assert.ThrowsAsync<ForbiddenException>(() => provider.GetRequiredService<IMediator>().Send(
            new UpdateSystemSettingsCommand(repository.Actor.UserId, result.ConcurrencyToken, Input())));
    }

    [Fact]
    public async Task UpdatePersistsAllValuesAndAuditInOneCommitWithCancellation()
    {
        var repository = new StubRepository(PermissionCodes.UpdateSystemSettings);
        using var provider = CreateProvider(repository);
        using var cancellation = new CancellationTokenSource();
        var values = new SystemSettingsInput(3, 4, 5, 1, 20, 24, 30, 10, 100, 7, 14);
        var result = await provider.GetRequiredService<IMediator>().Send(new UpdateSystemSettingsCommand(
            repository.Actor.UserId, repository.Settings!.ConcurrencyToken, values), cancellation.Token);
        Assert.Equal(new SystemSettingsValues(3, 4, 5, 1, 20, 24, 30, 10, 100, 7, 14), repository.Settings.GetValues());
        Assert.Equal(14, result.NotificationRetentionDays);
        Assert.Equal(1, repository.SaveCount);
        Assert.Equal(cancellation.Token, repository.LastCancellationToken);
        var audit = Assert.Single(repository.AuditEntries);
        Assert.Equal(AdminActionType.UPDATE_SYSTEM_SETTINGS, audit.ActionType);
        Assert.Equal(EntityTypes.SystemSettings, audit.TargetType);
        Assert.Equal(repository.Actor.UserId, audit.AdminUserId);
        Assert.Equal(SystemSettings.SingletonId, audit.TargetId);
        Assert.Equal(90, JsonSerializer.Deserialize<SystemSettingsValues>(audit.BeforeJson)!.NotificationRetentionDays);
        Assert.Equal(14, JsonSerializer.Deserialize<SystemSettingsValues>(audit.AfterJson)!.NotificationRetentionDays);
    }

    [Fact]
    public async Task InvalidValuesAreRejectedBeforeMutationAuditOrCommit()
    {
        var repository = new StubRepository(PermissionCodes.UpdateSystemSettings);
        using var provider = CreateProvider(repository);
        var before = repository.Settings!.GetValues();
        var error = await Assert.ThrowsAsync<RequestValidationException>(() => provider.GetRequiredService<IMediator>().Send(
            new UpdateSystemSettingsCommand(repository.Actor.UserId, repository.Settings.ConcurrencyToken,
                Input() with { MaxMembersPerTeam = 2, MaxDeputiesPerTeam = 2 })));
        Assert.Equal(ErrorCodes.InvalidSystemSettings, error.Code);
        Assert.Equal(before, repository.Settings.GetValues());
        Assert.Equal(0, repository.SaveCount);
        Assert.Empty(repository.AuditEntries);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StaleOrMissingVersionDoesNotMutateSettings(bool emptyVersion)
    {
        var repository = new StubRepository(PermissionCodes.UpdateSystemSettings);
        using var provider = CreateProvider(repository);
        var before = repository.Settings!.GetValues();
        var error = await Record.ExceptionAsync(() => provider.GetRequiredService<IMediator>().Send(
            new UpdateSystemSettingsCommand(repository.Actor.UserId, emptyVersion ? Guid.Empty : Guid.NewGuid(), Input())));
        if (emptyVersion)
            Assert.IsType<RequestValidationException>(error);
        else
            Assert.Equal(ErrorCodes.ConcurrencyConflict, Assert.IsType<ConflictException>(error).Code);
        Assert.Equal(before, repository.Settings.GetValues());
        Assert.Equal(0, repository.SaveCount);
        Assert.Empty(repository.AuditEntries);
    }

    [Fact]
    public async Task MissingConfigurationReturnsStableNotFoundError()
    {
        var repository = new StubRepository(PermissionCodes.ReadSystemSettings, PermissionCodes.UpdateSystemSettings) { Settings = null };
        using var provider = CreateProvider(repository);
        var mediator = provider.GetRequiredService<IMediator>();
        var readError = await Assert.ThrowsAsync<NotFoundException>(() => mediator.Send(new GetSystemSettingsQuery(repository.Actor.UserId)));
        var writeError = await Assert.ThrowsAsync<NotFoundException>(() => mediator.Send(
            new UpdateSystemSettingsCommand(repository.Actor.UserId, Guid.NewGuid(), Input())));
        Assert.Equal(ErrorCodes.SystemSettingsNotFound, readError.Code);
        Assert.Equal(ErrorCodes.SystemSettingsNotFound, writeError.Code);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public void HttpContractRequiresEveryValueAndSettingsPermissionsAreAdminOnlyByDefault()
    {
        var errors = new List<ValidationResult>();
        var request = new UpdateSystemSettingsRequest();
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), errors, true));
        Assert.Equal(16, errors.Count);
        var adminPermissions = InitialPermissionCatalog.GetDefaultPermissionCodes(RoleNames.Admin).ToArray();
        var userPermissions = InitialPermissionCatalog.GetDefaultPermissionCodes(RoleNames.User).ToArray();
        Assert.Contains(PermissionCodes.ReadSystemSettings, adminPermissions);
        Assert.Contains(PermissionCodes.UpdateSystemSettings, adminPermissions);
        Assert.DoesNotContain(PermissionCodes.ReadSystemSettings, userPermissions);
        Assert.DoesNotContain(PermissionCodes.UpdateSystemSettings, userPermissions);
    }

    [Fact]
    public async Task ControllerUsesTrustedActorAndReturnsSettingsContract()
    {
        var repository = new StubRepository(PermissionCodes.ReadSystemSettings, PermissionCodes.UpdateSystemSettings);
        using var provider = CreateProvider(repository);
        var controller = new SystemSettingsController(provider.GetRequiredService<IMediator>(), new TestCurrentUser(repository.Actor.UserId))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var readResult = await controller.GetSystemSettings(CancellationToken.None);
        var readResponse = Assert.IsType<BaseResponse<SystemSettingsDto>>(Assert.IsType<OkObjectResult>(readResult.Result).Value);
        var writeResult = await controller.UpdateSystemSettings(new UpdateSystemSettingsRequest
        {
            OutboxMaxRetryCount = 3,
            MaxCaptainTeamsPerUser = 2,
            MaxMemberTeamsPerUser = 5,
            MaxDeputiesPerTeam = 1,
            MaxMembersPerTeam = 20,
            TeamInvitationLifetimeHours = 48,
            MaxPendingTeamRequests = 25,
            OutboxRetryInitialDelaySeconds = 5,
            OutboxRetryMaxDelaySeconds = 120,
            ProcessedOutboxRetentionDays = 10,
            NotificationRetentionDays = 60,
            MaxActiveListingWatchesPerUser = 20,
            MaxListingSearchRadiusMeters = 50000,
            MaxListingWatchWindowDays = 30,
            MatchReminderLeadMinutes = 60,
            ConcurrencyToken = readResponse.Data.ConcurrencyToken
        }, CancellationToken.None);
        var writeResponse = Assert.IsType<BaseResponse<SystemSettingsDto>>(Assert.IsType<OkObjectResult>(writeResult.Result).Value);
        Assert.Equal(60, writeResponse.Data.NotificationRetentionDays);
        Assert.Equal(repository.Actor.UserId, Assert.Single(repository.AuditEntries).AdminUserId);
    }

    [Fact]
    public async Task CancelledRequestDoesNotMutateOrCommitSettings()
    {
        var repository = new StubRepository(PermissionCodes.UpdateSystemSettings);
        using var provider = CreateProvider(repository);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GetRequiredService<IMediator>().Send(
            new UpdateSystemSettingsCommand(repository.Actor.UserId, repository.Settings!.ConcurrencyToken, Input()), cancellation.Token));
        Assert.Equal(0, repository.SaveCount);
        Assert.Empty(repository.AuditEntries);
    }

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId => userId;
    }

    private static SystemSettingsInput Input() => new(5, 3, 10, 2, 50, 72, 100, 5, 3600, 30, 90);

    private static ServiceProvider CreateProvider(StubRepository repository)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<ISystemSettingsRepository>(repository);
        services.AddSingleton<IUserRoleRepository>(repository);
        services.AddSingleton<IAdminActionRepository>(repository);
        services.AddSingleton<IUnitOfWork>(repository);
        return services.BuildServiceProvider();
    }

    private sealed class StubRepository(params string[] permissions)
        : ISystemSettingsRepository, IUserRoleRepository, IAdminActionRepository, IUnitOfWork
    {
        public UserRoleAssignment Actor { get; } = new(Guid.NewGuid(), [RoleNames.Admin], permissions);
        public SystemSettings? Settings { get; set; } = SystemSettings.CreateDefaults();
        public List<AdminAction> AuditEntries { get; } = [];
        public int ReadCount { get; private set; }
        public int SaveCount { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public Task<SystemSettings?> GetAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastCancellationToken = cancellationToken;
            ReadCount++;
            return Task.FromResult(Settings);
        }

        public Task<SystemSettings?> GetForUpdateAsync(CancellationToken cancellationToken) => GetAsync(cancellationToken);
        public Task<UserRoleAssignment?> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(userId == Actor.UserId ? Actor : null);
        public Task ReplaceUserRolesAsync(UserRoleAssignment assignment, CancellationToken cancellationToken) => throw new NotSupportedException();
        public void Add(AdminAction entry) => AuditEntries.Add(entry);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastCancellationToken = cancellationToken;
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
