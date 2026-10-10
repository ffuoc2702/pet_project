using AutoMapper;
using MatchingSports.Application;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.UserPreferences;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.ValueObjects;
using MatchingSports.Infrastructure.Persistence;
using MatchingSports.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.Tests;

public sealed class WebFoundationTests
{
    [Fact]
    public void InvalidPreferencesDoNotPartiallyMutateState()
    {
        var preferences = UserPreferences.Create(Guid.NewGuid());
        preferences.Update("en", "dark", "purple");
        var token = preferences.ConcurrencyToken;
        Assert.Throws<ArgumentException>(() => preferences.Update("vi", "light", "unsupported"));
        Assert.Equal("en", preferences.LanguageCode);
        Assert.Equal("dark", preferences.ColorMode);
        Assert.Equal(token, preferences.ConcurrencyToken);
    }

    [Fact]
    public void InvalidDescriptionDoesNotPartiallyChangeSportOrFormats()
    {
        var sport = Sport.Create("SPORT", "Original", [SportFormatDetails.Create("ONE", "Original format", 1)], "  Details  ");
        Assert.Equal("Details", sport.Description);
        Assert.Throws<ArgumentException>(() => sport.UpdateDetails("NEW", "Changed",
            [SportFormatDetails.Create("TWO", "New format", 2)], new string('x', 2001)));
        Assert.Equal("Original", sport.Name);
        Assert.Single(sport.Formats);
        Assert.Equal("Details", sport.Description);
        Assert.Null(ResourceDescription.Normalize("   "));
        Assert.Throws<ArgumentException>(() => ResourceDescription.Normalize(new string('x', 4001), 4000));
    }

    [Fact]
    public async Task PreferencesUseCurrentActorAndCommitExactlyOnce()
    {
        var userId = Guid.NewGuid();
        var audit = new AuditActorContext();
        var repository = new PreferencesRepository();
        var unitOfWork = new PreferencesUnitOfWork(audit);
        var handler = new UpdateUserPreferencesCommandHandler(new CurrentActor(userId), repository, unitOfWork, audit, Mapper());
        using var cancellation = new CancellationTokenSource();
        var result = await handler.Handle(new UpdateUserPreferencesCommand("en", "dark", "green", null), cancellation.Token);
        Assert.Equal(userId, repository.LastUserId);
        Assert.Equal(cancellation.Token, repository.LastCancellation);
        Assert.Equal(userId, unitOfWork.Actor);
        Assert.Equal(1, unitOfWork.Commits);
        Assert.Equal("en", result.LanguageCode);
        Assert.Equal("dark", result.ColorMode);
        Assert.Null(audit.ActorUserId);
    }

    [Fact]
    public async Task StalePreferencesVersionDoesNotMutateOrCommit()
    {
        var preferences = UserPreferences.Create(Guid.NewGuid());
        preferences.Update("vi", "system", "blue");
        var audit = new AuditActorContext();
        var repository = new PreferencesRepository { Value = preferences };
        var unitOfWork = new PreferencesUnitOfWork(audit);
        var handler = new UpdateUserPreferencesCommandHandler(new CurrentActor(preferences.UserId), repository, unitOfWork, audit, Mapper());
        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new UpdateUserPreferencesCommand("en", "dark", "green", Guid.NewGuid()), CancellationToken.None));
        Assert.Equal(ErrorCodes.ConcurrencyConflict, exception.Code);
        Assert.Equal("vi", preferences.LanguageCode);
        Assert.Equal(0, unitOfWork.Commits);
    }

    [Fact]
    public void PreferencesModelHasIndependentTableAndOptimisticConcurrency()
    {
        var options = new DbContextOptionsBuilder<MatchingSportsDbContext>()
            .UseNpgsql("Host=localhost;Database=model_test;Username=postgres", npgsql => npgsql.UseNetTopologySuite()).Options;
        using var db = new MatchingSportsDbContext(options);
        var entity = db.Model.FindEntityType(typeof(UserPreferences))!;
        Assert.Equal("user_preferences", entity.GetTableName());
        Assert.Null(entity.BaseType);
        Assert.True(entity.FindProperty(nameof(UserPreferences.ConcurrencyToken))!.IsConcurrencyToken);
        Assert.Single(entity.FindPrimaryKey()!.Properties);
        Assert.Equal(2000, db.Model.FindEntityType(typeof(Sport))!.FindProperty(nameof(Sport.Description))!.GetMaxLength());
        Assert.Equal(4000, db.Model.FindEntityType(typeof(Listing))!.FindProperty(nameof(Listing.Description))!.GetMaxLength());
    }

    private static IMapper Mapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication(null);
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMapper>();
    }

    private sealed class CurrentActor(Guid userId) : ICurrentUser { public Guid UserId => userId; }
    private sealed class PreferencesRepository : IUserPreferencesRepository
    {
        public UserPreferences? Value { get; set; }
        public Guid LastUserId { get; private set; }
        public CancellationToken LastCancellation { get; private set; }
        public Task<UserPreferences?> GetAsync(Guid userId, bool tracked, CancellationToken cancellationToken)
        {
            LastUserId = userId; LastCancellation = cancellationToken;
            return Task.FromResult(Value);
        }
        public void Add(UserPreferences preferences) => Value = preferences;
    }
    private sealed class PreferencesUnitOfWork(IAuditActorContext audit) : IUnitOfWork
    {
        public int Commits { get; private set; }
        public Guid? Actor { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            Commits++; Actor = audit.ActorUserId;
            return Task.FromResult(1);
        }
    }
}
