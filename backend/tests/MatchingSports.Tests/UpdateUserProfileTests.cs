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
            repository,
            unitOfWork,
            CreateMapper());

        var result = await handler.Handle(
            new UpdateUserProfileCommand(
                userId,
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
            new StubUserProfileRepository(null),
            unitOfWork,
            CreateMapper());

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new UpdateUserProfileCommand(
                    Guid.NewGuid(),
                    "Nguyễn Văn A",
                    null,
                    null,
                    null),
                CancellationToken.None));

        Assert.Equal(ErrorCodes.UserProfileNotFound, exception.Code);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private sealed class StubUserProfileRepository(UserProfile? profile)
        : IUserProfileRepository
    {
        public Task<UserProfile?> GetForUpdateAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(profile?.UserId == userId ? profile : null);
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
