using MatchingSports.Application;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.User;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Shared;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.Tests;

public sealed class UserDirectoryTests
{
    [Theory]
    [InlineData(PermissionCodes.ReadUsers)]
    [InlineData(PermissionCodes.ListRoles)]
    public async Task ListUsersRequiresItsOwnPermissionBeforeReadingDirectory(string permission)
    {
        var directory = new DirectoryStub();
        using var provider = CreateProvider(directory, permission);
        await Assert.ThrowsAsync<ForbiddenException>(() => provider.GetRequiredService<IMediator>().Send(new GetUsersQuery { ActorUserId = Guid.NewGuid() }));
        Assert.Null(directory.LastQuery);
    }

    [Fact]
    public async Task AuthorizedQueryPreservesFiltersPaginationAndCancellation()
    {
        var directory = new DirectoryStub();
        using var provider = CreateProvider(directory, PermissionCodes.ListUsers);
        using var cancellation = new CancellationTokenSource();
        var request = new GetUsersQuery { ActorUserId = Guid.NewGuid(), SearchString = "Player", Status = UserStatus.SUSPENDED, PageNumber = 2, PageSize = 10 };
        var result = await provider.GetRequiredService<IMediator>().Send(request, cancellation.Token);
        Assert.Same(request, directory.LastQuery);
        Assert.Equal(cancellation.Token, directory.LastCancellation);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 101)]
    public async Task InvalidPaginationDoesNotReadDirectory(int page, int size)
    {
        var directory = new DirectoryStub();
        using var provider = CreateProvider(directory, PermissionCodes.ListUsers);
        await Assert.ThrowsAsync<RequestValidationException>(() => provider.GetRequiredService<IMediator>().Send(new GetUsersQuery { ActorUserId = Guid.NewGuid(), PageNumber = page, PageSize = size }));
        Assert.Null(directory.LastQuery);
    }

    [Fact]
    public async Task InvalidStatusDoesNotReadDirectory()
    {
        var directory = new DirectoryStub();
        using var provider = CreateProvider(directory, PermissionCodes.ListUsers);
        await Assert.ThrowsAsync<RequestValidationException>(() => provider.GetRequiredService<IMediator>().Send(new GetUsersQuery { ActorUserId = Guid.NewGuid(), Status = (UserStatus)123 }));
        Assert.Null(directory.LastQuery);
    }

    private static ServiceProvider CreateProvider(DirectoryStub directory, string permission)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IUserDirectoryRepository>(directory);
        services.AddSingleton<IUserRoleRepository>(new ActorStub(permission));
        return services.BuildServiceProvider();
    }

    private sealed class DirectoryStub : IUserDirectoryRepository
    {
        public GetUsersQuery? LastQuery { get; private set; }
        public CancellationToken LastCancellation { get; private set; }
        public Task<PaginatedResult<UserListDto>> ListAsync(GetUsersQuery query, CancellationToken cancellationToken)
        {
            LastQuery = query;
            LastCancellation = cancellationToken;
            return Task.FromResult(new PaginatedResult<UserListDto>([], query.PageNumber, query.PageSize, 0));
        }
    }

    private sealed class ActorStub(string permission) : IUserRoleRepository
    {
        public Task<UserRoleAssignment?> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult<UserRoleAssignment?>(new(userId, ["STAFF"], [permission]));
        public Task ReplaceUserRolesAsync(UserRoleAssignment assignment, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
