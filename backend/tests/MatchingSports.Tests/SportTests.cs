using MatchingSports.Api.Contracts;
using MatchingSports.Api.Controllers;
using MatchingSports.Application;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Sport;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.ValueObjects;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.Tests;

public sealed class SportTests
{
    [Fact]
    public async Task ControllerAppliesMixedFormatActionsInOneCommit()
    {
        var sport = Sport.Create("TEST", "Test", [
            SportFormatDetails.Create("ONE", "One", 1), SportFormatDetails.Create("TWO", "Two", 2)]);
        var first = sport.Formats.Single(format => format.Code == "ONE");
        var deleted = sport.Formats.Single(format => format.Code == "TWO");
        var store = new StubStore { Sport = sport };
        using var provider = CreateProvider(store);
        var controller = new SportController(provider.GetRequiredService<IMediator>(),
            provider.GetRequiredService<AutoMapper.IMapper>(), new StubCurrentUser(Guid.NewGuid()))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var response = await controller.UpdateSport(sport.Id, new UpdateSportRequest
        {
            Code = "TEST",
            Name = "Updated",
            Formats = [
                new SportFormatRequest { Action = "MODIFY", Id = first.Id, Code = "FIRST", DisplayName = "First", SideSize = 3 },
                new SportFormatRequest { Action = "DELETE", Id = deleted.Id },
                new SportFormatRequest { Action = "NEW", Code = "THREE", DisplayName = "Three", SideSize = 3 }]
        }, CancellationToken.None);

        var result = Assert.IsType<BaseResponse<SportDto>>(Assert.IsType<OkObjectResult>(response.Result).Value);
        Assert.Equal(1, store.SaveCount);
        Assert.Equal(3, result.Data!.Formats.Count);
        Assert.Equal("FIRST", result.Data.Formats.Single(format => format.Id == first.Id).Code);
        Assert.False(result.Data.Formats.Single(format => format.Id == deleted.Id).IsActive);
        Assert.True(result.Data.Formats.Single(format => format.Code == "THREE").IsActive);
    }

    [Theory]
    [InlineData("MODIFY")]
    [InlineData("DELETE")]
    public async Task CreateRejectsActionsForExistingFormatsWithoutCommit(string action)
    {
        var store = new StubStore();
        using var provider = CreateProvider(store);
        var error = await Assert.ThrowsAsync<RequestValidationException>(() => provider.GetRequiredService<IMediator>().Send(
            new CreateSportCommand(Guid.NewGuid(), "TEST", "Test", [new(Guid.NewGuid(), "ONE", "One", 1, action)])));
        Assert.Equal(ErrorCodes.InvalidSport, error.Code);
        Assert.Equal(0, store.SaveCount);
        Assert.Equal(0, store.AddCount);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task ActiveDuplicateCodeOrNameDoesNotCommit(bool create, bool duplicateCode)
    {
        var duplicate = Sport.Create("EXISTING", "Existing name", CreateFormatDetails());
        var target = Sport.Create("TARGET", "Target name", CreateFormatDetails());
        var store = new StubStore { Sport = target, OtherSport = duplicate };
        using var provider = CreateProvider(store);
        var code = duplicateCode ? " existing " : "UNIQUE";
        var name = duplicateCode ? "Unique name" : " existing NAME ";
        IRequest<SportDto> request = create
            ? new CreateSportCommand(Guid.NewGuid(), code, name, CreateFormatInputs())
            : new UpdateSportCommand(Guid.NewGuid(), target.Id, code, name, CreateFormatInputs(target.Formats.Single().Id));

        var error = await Assert.ThrowsAsync<ConflictException>(() => provider.GetRequiredService<IMediator>().Send(request));
        Assert.Equal(ErrorCodes.DuplicateResource, error.Code);
        Assert.Equal(0, store.SaveCount);
        Assert.Equal(0, store.AddCount);
        Assert.Equal("TARGET", target.Code);
    }

    [Fact]
    public async Task InactiveSportDoesNotReserveCodeOrName()
    {
        var duplicate = Sport.Create("EXISTING", "Existing name", CreateFormatDetails());
        duplicate.Deactivate();
        var store = new StubStore { OtherSport = duplicate };
        using var provider = CreateProvider(store);
        var created = await provider.GetRequiredService<IMediator>().Send(
            new CreateSportCommand(Guid.NewGuid(), " existing ", " Existing name ", CreateFormatInputs()));
        Assert.True(created.IsActive);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public void CreateNormalizesDetailsAndStartsActive()
    {
        var sport = Sport.Create("  badminton  ", "  Cầu lông  ", CreateFormatDetails());

        Assert.NotEqual(Guid.Empty, sport.Id);
        Assert.Equal("BADMINTON", sport.Code);
        Assert.Equal("Cầu lông", sport.Name);
        Assert.True(sport.IsActive);
    }

    [Theory]
    [InlineData(null, "Name")]
    [InlineData(" ", "Name")]
    [InlineData("CODE", null)]
    [InlineData("CODE", " ")]
    public void CreateRejectsMissingDetails(string? code, string? name) =>
        Assert.ThrowsAny<ArgumentException>(() => Sport.Create(code!, name!, CreateFormatDetails()));

    [Theory]
    [InlineData(33, 4)]
    [InlineData(4, 101)]
    [InlineData(4, 0)]
    public void InvalidUpdateDoesNotPartiallyChangeDetails(int codeLength, int nameLength)
    {
        var sport = Sport.Create("OLD", "Old name", CreateFormatDetails());

        Assert.ThrowsAny<ArgumentException>(() => sport.UpdateDetails(new string('C', codeLength), new string('N', nameLength), CreateFormatDetails()));

        Assert.Equal("OLD", sport.Code);
        Assert.Equal("Old name", sport.Name);
    }

    [Fact]
    public void UpdatingDeactivatedSportPreservesInactiveState()
    {
        var sport = Sport.Create("OLD", "Old name", CreateFormatDetails());
        sport.Deactivate();
        sport.Deactivate();
        sport.UpdateDetails("  new  ", "  New name  ", [SportFormatDetails.Create("DEFAULT", "Default format", 1, sport.Formats.Single().Id)]);

        Assert.Equal("NEW", sport.Code);
        Assert.Equal("New name", sport.Name);
        Assert.False(sport.IsActive);
    }

    [Fact]
    public async Task CrudThroughMediatorCommitsWritesAndMapsReadResults()
    {
        var store = new StubStore();
        using var provider = CreateProvider(store);
        var mediator = provider.GetRequiredService<IMediator>();
        using var cancellation = new CancellationTokenSource();
        var actorId = Guid.NewGuid();

        var created = await mediator.Send(new CreateSportCommand(actorId, "  football  ", "  Bóng đá  ", CreateFormatInputs()), cancellation.Token);
        Assert.Equal("FOOTBALL", created.Code);
        Assert.True(created.IsActive);
        Assert.Equal(1, store.AddCount);
        Assert.Equal(1, store.SaveCount);

        var updated = await mediator.Send(new UpdateSportCommand(actorId, created.Id, "  soccer  ", "  Football  ", CreateFormatInputs(created.Formats.Single().Id)), cancellation.Token);
        Assert.Equal(created.Id, store.LastSportId);
        Assert.Equal("SOCCER", updated.Code);
        Assert.Equal("Football", updated.Name);
        Assert.Equal(2, store.SaveCount);

        var deactivated = await mediator.Send(new DeactivateSportCommand(actorId, created.Id), cancellation.Token);
        Assert.False(deactivated.IsActive);
        Assert.Equal(3, store.SaveCount);

        var detail = await mediator.Send(new GetSportByIdQuery(actorId, created.Id), cancellation.Token);
        Assert.False(detail.IsActive);
        Assert.Equal(created.Id, detail.Id);
        var page = await mediator.Send(new GetSportsQuery { ActorUserId = actorId }, cancellation.Token);
        Assert.Equal(created.Id, Assert.Single(page.Items).Id);
        Assert.Equal(1, page.TotalItems);
        Assert.Equal(PaginationDefaults.DefaultPageNumber, page.PageNumber);
        Assert.Equal(PaginationDefaults.DefaultPageSize, page.PageSize);
        var activePage = await mediator.Send(new GetSportsQuery { ActorUserId = actorId, IsActive = true }, cancellation.Token);
        Assert.Empty(activePage.Items);
        Assert.Equal(3, store.SaveCount);
        Assert.Equal(6, store.AuthorizationReadCount);
        Assert.Equal(actorId, store.LastActorId);
        Assert.Equal(cancellation.Token, store.LastAuthorizationToken);
        Assert.Equal(cancellation.Token, store.LastReadToken);
        Assert.Equal(cancellation.Token, store.LastSaveToken);
    }

    [Theory]
    [InlineData("create", false)]
    [InlineData("update", false)]
    [InlineData("deactivate", false)]
    [InlineData("activate", false)]
    [InlineData("create", true)]
    [InlineData("update", true)]
    [InlineData("deactivate", true)]
    [InlineData("activate", true)]
    public async Task MissingActorOrWritePermissionStopsRepositoryAndCommit(string action, bool actorExists)
    {
        var store = new StubStore { ActorExists = actorExists, Permissions = [PermissionCodes.ReadSports, PermissionCodes.ListSports] };
        using var provider = CreateProvider(store);

        var error = await Assert.ThrowsAsync<ForbiddenException>(() => provider.GetRequiredService<IMediator>()
            .Send(CreateWriteRequest(action, Guid.NewGuid(), Guid.NewGuid())));

        Assert.Equal(ErrorCodes.Forbidden, error.Code);
        Assert.Equal(1, store.AuthorizationReadCount);
        Assert.Equal(0, store.ReadCount);
        Assert.Equal(0, store.AddCount);
        Assert.Equal(0, store.SaveCount);
    }

    [Theory]
    [InlineData("create", PermissionCodes.UpdateSports)]
    [InlineData("update", PermissionCodes.DeactivateSports)]
    [InlineData("deactivate", PermissionCodes.CreateSports)]
    [InlineData("activate", PermissionCodes.DeactivateSports)]
    public async Task OtherWritePermissionDoesNotAuthorizeAction(string action, string permission)
    {
        var store = new StubStore { Permissions = [permission] };
        using var provider = CreateProvider(store);

        await Assert.ThrowsAsync<ForbiddenException>(() => provider.GetRequiredService<IMediator>()
            .Send(CreateWriteRequest(action, Guid.NewGuid(), Guid.NewGuid())));

        Assert.Equal(0, store.ReadCount);
        Assert.Equal(0, store.AddCount);
        Assert.Equal(0, store.SaveCount);
    }

    [Theory]
    [InlineData("update")]
    [InlineData("deactivate")]
    [InlineData("activate")]
    public async Task MissingSportDoesNotCommit(string action)
    {
        var store = new StubStore();
        using var provider = CreateProvider(store);

        var error = await Assert.ThrowsAsync<NotFoundException>(() => provider.GetRequiredService<IMediator>()
            .Send(CreateWriteRequest(action, Guid.NewGuid(), Guid.NewGuid())));

        Assert.Equal(ErrorCodes.SportNotFound, error.Code);
        Assert.Equal(0, store.SaveCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InvalidInputReturnsValidationErrorWithoutMutationOrCommit(bool create)
    {
        var sport = Sport.Create("OLD", "Old name", CreateFormatDetails());
        var store = new StubStore { Sport = sport };
        using var provider = CreateProvider(store);
        IRequest<SportDto> request = create
            ? new CreateSportCommand(Guid.NewGuid(), "NEW", " ", CreateFormatInputs())
            : new UpdateSportCommand(Guid.NewGuid(), sport.Id, "NEW", " ", CreateFormatInputs());

        var error = await Assert.ThrowsAsync<RequestValidationException>(() => provider.GetRequiredService<IMediator>().Send(request));

        Assert.Equal(ErrorCodes.InvalidSport, error.Code);
        Assert.Contains("name", error.Errors.Keys);
        Assert.Equal("OLD", sport.Code);
        Assert.Equal("Old name", sport.Name);
        Assert.Equal(0, store.AddCount);
        Assert.Equal(0, store.SaveCount);
    }

    [Theory]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("deactivate")]
    [InlineData("activate")]
    public async Task CancellationAtCommitIsPropagated(string action)
    {
        var sport = Sport.Create("OLD", "Old name", CreateFormatDetails());
        var store = new StubStore { Sport = sport, CancelAtCommit = true };
        using var provider = CreateProvider(store);
        using var cancellation = new CancellationTokenSource();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GetRequiredService<IMediator>()
            .Send(CreateWriteRequest(action, Guid.NewGuid(), sport.Id), cancellation.Token));

        Assert.Equal(1, store.SaveCount);
        Assert.Equal(cancellation.Token, store.LastSaveToken);
    }

    [Fact]
    public async Task ControllerUsesTrustedActorAndReturnsSportContract()
    {
        var store = new StubStore();
        using var provider = CreateProvider(store);
        var actorId = Guid.NewGuid();
        var controller = new SportController(provider.GetRequiredService<IMediator>(),
            provider.GetRequiredService<AutoMapper.IMapper>(), new StubCurrentUser(actorId))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        using var cancellation = new CancellationTokenSource();

        var response = await controller.CreateSport(new CreateSportRequest
        {
            Code = "  TEST  ",
            Name = "  Test sport  ",
            Formats = [new SportFormatRequest { Action = "NEW", Code = "  singles  ", DisplayName = "  Singles  ", SideSize = 1 }]
        }, cancellation.Token);

        var result = Assert.IsType<OkObjectResult>(response.Result);
        var contract = Assert.IsType<BaseResponse<SportDto>>(result.Value);
        Assert.Equal("TEST", contract.Data!.Code);
        Assert.Equal("Test sport", contract.Data.Name);
        Assert.True(contract.Data.IsActive);
        var createdFormat = Assert.Single(contract.Data.Formats);
        Assert.Equal("SINGLES", createdFormat.Code);
        Assert.Equal("Singles", createdFormat.DisplayName);
        Assert.Equal(1, createdFormat.SideSize);
        Assert.True(createdFormat.IsActive);

        var updatedResponse = await controller.UpdateSport(contract.Data.Id,
            new UpdateSportRequest
            {
                Code = "TEST",
                Name = "Updated sport",
                Formats = [new SportFormatRequest { Action = "MODIFY", Id = createdFormat.Id, Code = "DOUBLES", DisplayName = "Doubles", SideSize = 2 }]
            }, cancellation.Token);
        var updatedContract = Assert.IsType<BaseResponse<SportDto>>(Assert.IsType<OkObjectResult>(updatedResponse.Result).Value);
        var updatedFormat = Assert.Single(updatedContract.Data!.Formats);
        Assert.Equal(createdFormat.Id, updatedFormat.Id);
        Assert.Equal("DOUBLES", updatedFormat.Code);
        Assert.Equal(2, updatedFormat.SideSize);
        Assert.Equal(2, store.SaveCount);
        Assert.Equal(actorId, store.LastActorId);
        Assert.Equal(cancellation.Token, store.LastAuthorizationToken);
        Assert.Equal(cancellation.Token, store.LastSaveToken);
    }

    [Theory]
    [InlineData(true, "missing")]
    [InlineData(false, "missing")]
    [InlineData(true, "empty")]
    [InlineData(false, "empty")]
    [InlineData(true, "null-item")]
    [InlineData(false, "null-item")]
    [InlineData(true, "duplicate-code")]
    [InlineData(false, "duplicate-code")]
    [InlineData(true, "invalid-side-size")]
    [InlineData(false, "invalid-side-size")]
    [InlineData(true, "foreign-id")]
    [InlineData(false, "foreign-id")]
    public async Task InvalidFormatsDoNotAddOrCommitSport(bool create, string invalidCase)
    {
        var sport = Sport.Create("OLD", "Old sport", CreateFormatDetails());
        var existing = Assert.Single(sport.Formats);
        var store = new StubStore { Sport = sport };
        using var provider = CreateProvider(store);
        IReadOnlyCollection<SportFormatInput> formats = invalidCase switch
        {
            "missing" => null!,
            "empty" => [],
            "null-item" => [null!],
            "duplicate-code" => [new(null, " default ", "One", 1, "NEW"), new(null, "DEFAULT", "Two", 2, "NEW")],
            "invalid-side-size" => [new(null, "DEFAULT", "Changed", 0, "NEW")],
            _ => [new(Guid.NewGuid(), "FOREIGN", "Foreign", 1, "MODIFY")]
        };
        IRequest<SportDto> request = create
            ? new CreateSportCommand(Guid.NewGuid(), "NEW", "New sport", formats)
            : new UpdateSportCommand(Guid.NewGuid(), sport.Id, "NEW", "New sport", formats);

        var error = await Assert.ThrowsAsync<RequestValidationException>(() => provider.GetRequiredService<IMediator>().Send(request));

        Assert.Equal(ErrorCodes.InvalidSport, error.Code);
        Assert.Equal("OLD", sport.Code);
        Assert.Equal("Old sport", sport.Name);
        Assert.Same(existing, Assert.Single(sport.Formats));
        Assert.Equal("Default format", existing.DisplayName);
        Assert.True(existing.IsActive);
        Assert.Equal(0, store.AddCount);
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public async Task ActivationThroughControllerPreservesFormatsAndPassesActorAndCancellation()
    {
        var sport = Sport.Create("TEST", "Test", CreateFormatDetails());
        sport.Deactivate();
        var store = new StubStore { Sport = sport, Permissions = [PermissionCodes.UpdateSports] };
        using var provider = CreateProvider(store);
        var actorId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        var controller = new SportController(provider.GetRequiredService<IMediator>(), provider.GetRequiredService<AutoMapper.IMapper>(), new StubCurrentUser(actorId))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var response = await controller.ActivateSport(sport.Id, cancellation.Token);
        var contract = Assert.IsType<BaseResponse<SportDto>>(Assert.IsType<OkObjectResult>(response.Result).Value);
        Assert.True(contract.Data!.IsActive);
        Assert.Equal(sport.Id, contract.Data.Id);
        Assert.Equal(Assert.Single(sport.Formats).Id, Assert.Single(contract.Data.Formats).Id);
        Assert.Equal(actorId, store.LastActorId);
        Assert.Equal(cancellation.Token, store.LastReadToken);
        Assert.Equal(cancellation.Token, store.LastSaveToken);
        Assert.Equal(1, store.SaveCount);
        await controller.ActivateSport(sport.Id, cancellation.Token);
        Assert.True(sport.IsActive);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ActivationRejectsActiveDuplicateWithoutChangingStateOrCommitting(bool duplicateCode)
    {
        var sport = Sport.Create("TEST", "Test", CreateFormatDetails());
        sport.Deactivate();
        var duplicate = Sport.Create(duplicateCode ? "TEST" : "OTHER", duplicateCode ? "Other name" : "Test", CreateFormatDetails());
        var store = new StubStore { Sport = sport, OtherSport = duplicate };
        using var provider = CreateProvider(store);
        var error = await Assert.ThrowsAsync<ConflictException>(() => provider.GetRequiredService<IMediator>()
            .Send(new ActivateSportCommand(Guid.NewGuid(), sport.Id)));
        Assert.Equal(ErrorCodes.DuplicateResource, error.Code);
        Assert.False(sport.IsActive);
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public async Task ActivationRequiresAnActiveFormatBeforeChangingState()
    {
        var sport = Sport.Create("TEST", "Test", CreateFormatDetails());
        sport.Deactivate();
        Assert.Single(sport.Formats).Deactivate();
        Assert.Throws<ArgumentException>(() => sport.Activate());
        Assert.False(sport.IsActive);
        var store = new StubStore { Sport = sport };
        using var provider = CreateProvider(store);
        var error = await Assert.ThrowsAsync<RequestValidationException>(() => provider.GetRequiredService<IMediator>()
            .Send(new ActivateSportCommand(Guid.NewGuid(), sport.Id)));
        Assert.Equal(ErrorCodes.InvalidSport, error.Code);
        Assert.Equal(0, store.SaveCount);
    }

    private static SportFormatInput[] CreateFormatInputs(Guid? id = null) => [new(id, "DEFAULT", "Default format", 1, id.HasValue ? "MODIFY" : "NEW")];
    private static SportFormatDetails[] CreateFormatDetails() => [SportFormatDetails.Create("DEFAULT", "Default format", 1)];

    private static IRequest<SportDto> CreateWriteRequest(string action, Guid actorId, Guid sportId) => action switch
    {
        "create" => new CreateSportCommand(actorId, "NEW", "New name", CreateFormatInputs()),
        "update" => new UpdateSportCommand(actorId, sportId, "NEW", "New name", [new(null, "EXTRA", "Extra format", 2, "NEW")]),
        "activate" => new ActivateSportCommand(actorId, sportId),
        _ => new DeactivateSportCommand(actorId, sportId)
    };

    private static ServiceProvider CreateProvider(StubStore store)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication(null, typeof(ContractMappingProfile).Assembly);
        services.AddSingleton<ISportRepository>(store);
        services.AddSingleton<IUserRoleRepository>(store);
        services.AddSingleton<IUnitOfWork>(store);
        return services.BuildServiceProvider();
    }

    private sealed class StubCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId => userId;
    }

    private sealed class StubStore : ISportRepository, IUserRoleRepository, IUnitOfWork
    {
        public Sport? Sport { get; set; }
        public Sport? OtherSport { get; init; }
        public bool ActorExists { get; init; } = true;
        public bool CancelAtCommit { get; init; }
        public IReadOnlyCollection<string> Permissions { get; init; } =
            [PermissionCodes.CreateSports, PermissionCodes.UpdateSports, PermissionCodes.DeactivateSports,
             PermissionCodes.ReadSports, PermissionCodes.ListSports];
        public int AuthorizationReadCount { get; private set; }
        public int ReadCount { get; private set; }
        public int AddCount { get; private set; }
        public int SaveCount { get; private set; }
        public Guid LastActorId { get; private set; }
        public Guid LastSportId { get; private set; }
        public CancellationToken LastAuthorizationToken { get; private set; }
        public CancellationToken LastReadToken { get; private set; }
        public CancellationToken LastSaveToken { get; private set; }

        public Task<UserRoleAssignment?> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken)
        {
            AuthorizationReadCount++;
            LastActorId = userId;
            LastAuthorizationToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<UserRoleAssignment?>(ActorExists ? new(userId, ["CUSTOM"], Permissions) : null);
        }

        public Task ReplaceUserRolesAsync(UserRoleAssignment assignment, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> HasActiveDuplicateAsync(string code, string name, Guid? excludedSportId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new[] { Sport, OtherSport }.Any(sport => sport is { IsActive: true } && sport.Id != excludedSportId
                && (sport.Code.Equals(code, StringComparison.OrdinalIgnoreCase) || sport.Name.Equals(name, StringComparison.OrdinalIgnoreCase))));
        }

        public void Update(Sport sport) { }

        public void Add(Sport sport)
        {
            AddCount++;
            Sport = sport;
        }

        public Task<Sport?> GetForUpdateAsync(Guid sportId, CancellationToken cancellationToken)
        {
            ReadCount++;
            LastSportId = sportId;
            LastReadToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Sport?.Id == sportId ? Sport : null);
        }

        public Task<Sport?> GetByIdAsync(Guid sportId, CancellationToken cancellationToken) =>
            GetForUpdateAsync(sportId, cancellationToken);

        public Task<PaginatedResult<Sport>> ListAsync(GetSportsQuery query, CancellationToken cancellationToken = default)
        {
            ReadCount++;
            LastReadToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<Sport> items = Sport is not null && (!query.IsActive.HasValue || Sport.IsActive == query.IsActive.Value) ? [Sport] : [];
            return Task.FromResult(new PaginatedResult<Sport>(items, query.PageNumber, query.PageSize, items.Count));
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            LastSaveToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            if (CancelAtCommit)
                throw new OperationCanceledException(cancellationToken);
            return Task.FromResult(1);
        }
    }
}
