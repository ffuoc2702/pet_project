using MatchingSports.Application;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Entities;
using MatchingSports.Shared;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.Tests;

public sealed class PermissionAuthorizationBehaviorTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DenialStopsHandlerForMissingActorOrPermission(bool actorExists)
    {
        var repository = new StubUserRoleRepository(actorExists, PermissionCodes.ListRoles);
        using var provider = CreateProvider(repository);
        var mediator = provider.GetRequiredService<IMediator>();

        var error = await Assert.ThrowsAsync<ForbiddenException>(() => mediator.Send(new ProtectedRequest(Guid.NewGuid())));

        Assert.Equal(ErrorCodes.Forbidden, error.Code);
        Assert.Equal(1, repository.ReadCount);
        Assert.Equal(0, provider.GetRequiredService<HandlerCalls>().Count);
    }

    [Fact]
    public async Task AllPermissionsAuthorizeHandlerOnceAndForwardActorAndCancellation()
    {
        var repository = new StubUserRoleRepository(true, PermissionCodes.ListRoles, PermissionCodes.ListPermissions);
        using var provider = CreateProvider(repository);
        using var cancellation = new CancellationTokenSource();
        var actorUserId = Guid.NewGuid();

        var result = await provider.GetRequiredService<IMediator>().Send(new ProtectedRequest(actorUserId), cancellation.Token);

        Assert.Equal("allowed", result);
        Assert.Equal(actorUserId, repository.LastUserId);
        Assert.Equal(cancellation.Token, repository.LastCancellationToken);
        Assert.Equal(1, repository.ReadCount);
        var calls = provider.GetRequiredService<HandlerCalls>();
        Assert.Equal(1, calls.Count);
        Assert.Equal(cancellation.Token, calls.LastCancellationToken);
    }

    [Fact]
    public async Task CancellationDuringAuthorizationDoesNotRunHandler()
    {
        var repository = new StubUserRoleRepository(true, PermissionCodes.ListRoles, PermissionCodes.ListPermissions);
        using var provider = CreateProvider(repository);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GetRequiredService<IMediator>()
            .Send(new ProtectedRequest(Guid.NewGuid()), cancellation.Token));

        Assert.Equal(0, provider.GetRequiredService<HandlerCalls>().Count);
    }

    [Fact]
    public async Task UnprotectedRequestDoesNotRequireAuthorizationRepository()
    {
        using var provider = CreateProvider(null);

        var result = await provider.GetRequiredService<IMediator>().Send(new PublicRequest());

        Assert.Equal("public", result);
    }

    [Fact]
    public async Task EmptyPermissionDeclarationDoesNotRunHandler()
    {
        var repository = new StubUserRoleRepository(true);
        using var provider = CreateProvider(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetRequiredService<IMediator>()
            .Send(new MissingPermissionsRequest(Guid.NewGuid())));

        Assert.Equal(0, repository.ReadCount);
        Assert.Equal(0, provider.GetRequiredService<HandlerCalls>().Count);
    }

    private static ServiceProvider CreateProvider(StubUserRoleRepository? repository)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        if (repository is not null)
            services.AddSingleton<IUserRoleRepository>(repository);
        services.AddSingleton<HandlerCalls>();
        services.AddTransient<IRequestHandler<ProtectedRequest, string>, ProtectedHandler>();
        services.AddTransient<IRequestHandler<PublicRequest, string>, PublicHandler>();
        services.AddTransient<IRequestHandler<MissingPermissionsRequest, string>, MissingPermissionsHandler>();
        return services.BuildServiceProvider();
    }

    public sealed record ProtectedRequest(Guid ActorUserId) : IRequest<string>, IPermissionAuthorizedRequest
    {
        public IReadOnlyCollection<string> RequiredPermissions => [PermissionCodes.ListRoles, PermissionCodes.ListPermissions];
    }

    public sealed record MissingPermissionsRequest(Guid ActorUserId) : IRequest<string>, IPermissionAuthorizedRequest
    {
        public IReadOnlyCollection<string> RequiredPermissions => [];
    }

    public sealed record PublicRequest : IRequest<string>;

    public sealed class HandlerCalls
    {
        public int Count { get; set; }
        public CancellationToken LastCancellationToken { get; set; }
    }

    public sealed class ProtectedHandler(HandlerCalls calls) : IRequestHandler<ProtectedRequest, string>
    {
        public Task<string> Handle(ProtectedRequest request, CancellationToken cancellationToken)
        {
            calls.Count++;
            calls.LastCancellationToken = cancellationToken;
            return Task.FromResult("allowed");
        }
    }

    public sealed class MissingPermissionsHandler(HandlerCalls calls) : IRequestHandler<MissingPermissionsRequest, string>
    {
        public Task<string> Handle(MissingPermissionsRequest request, CancellationToken cancellationToken)
        {
            calls.Count++;
            return Task.FromResult("unexpected");
        }
    }

    public sealed class PublicHandler : IRequestHandler<PublicRequest, string>
    {
        public Task<string> Handle(PublicRequest request, CancellationToken cancellationToken) => Task.FromResult("public");
    }

    private sealed class StubUserRoleRepository(bool actorExists, params string[] permissions) : IUserRoleRepository
    {
        public int ReadCount { get; private set; }
        public Guid LastUserId { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public Task<UserRoleAssignment?> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken)
        {
            ReadCount++;
            LastUserId = userId;
            LastCancellationToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<UserRoleAssignment?>(actorExists ? new(userId, ["CUSTOM_ROLE"], permissions) : null);
        }

        public Task ReplaceUserRolesAsync(UserRoleAssignment assignment, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
