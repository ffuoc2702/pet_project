using AutoMapper;
using MatchingSports.Application;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.UserProfile;
using MatchingSports.Domain.Entities;
using MatchingSports.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.Tests;

public sealed class UpdateUserProfileTests
{
    [Fact]
    public async Task HandleUpdatesAndNormalizesProfile()
    {
        var userId = Guid.NewGuid();
        var profile = new UserProfile
        {
            UserId = userId,
            DisplayName = "Old name"
        };
        var repository = new StubUserProfileRepository(profile);
        var unitOfWork = new StubUnitOfWork();
        var handler = new UpdateUserProfileCommandHandler(
            new StubCurrentUser(userId),
            repository,
            unitOfWork,
            CreateMapper());

        var result = await handler.Handle(
            new UpdateUserProfileCommand(
                "  Nguyễn Văn A  ",
                "  https://example.com/avatar.png  ",
                "  Quận 1  ",
                "   "),
            CancellationToken.None);

        Assert.Equal("Nguyễn Văn A", profile.DisplayName);
        Assert.Equal("https://example.com/avatar.png", profile.AvatarUrl);
        Assert.Equal("Quận 1", profile.DefaultArea);
        Assert.Null(profile.ContactNote);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleThrowsWhenProfileDoesNotExist()
    {
        var unitOfWork = new StubUnitOfWork();
        var handler = new UpdateUserProfileCommandHandler(
            new StubCurrentUser(Guid.NewGuid()),
            new StubUserProfileRepository(null),
            unitOfWork,
            CreateMapper());

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new UpdateUserProfileCommand(
                    "Nguyễn Văn A",
                    null,
                    null,
                    null),
                CancellationToken.None));

        Assert.Equal(ErrorCodes.UserProfileNotFound, exception.Code);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task HandleRejectsInvalidIdentityBeforeReadingOrSaving()
    {
        var repository = new StubUserProfileRepository(null);
        var unitOfWork = new StubUnitOfWork();
        var handler = new UpdateUserProfileCommandHandler(
            new InvalidCurrentUser(),
            repository,
            unitOfWork,
            CreateMapper());

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(
            new UpdateUserProfileCommand("Name", null, null, null),
            CancellationToken.None));

        Assert.Equal(ErrorCodes.InvalidAccessToken, exception.Code);
        Assert.Equal(0, repository.ReadCount);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    private sealed class InvalidCurrentUser : ICurrentUser
    {
        public Guid UserId => throw new UnauthorizedException(ErrorCodes.InvalidAccessToken, "Access token không hợp lệ.");
    }

    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private sealed class StubCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId => userId;
    }

    private sealed class StubUserProfileRepository(UserProfile? profile)
        : IUserProfileRepository
    {
        public int ReadCount { get; private set; }

        public Task<UserProfile?> GetForUpdateAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return Task.FromResult(profile?.UserId == userId ? profile : null);
        }
    }

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
