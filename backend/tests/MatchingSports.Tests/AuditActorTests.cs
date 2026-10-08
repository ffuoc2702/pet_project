using MatchingSports.Application;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Entities;
using MatchingSports.Infrastructure.Persistence;
using MatchingSports.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.Tests;

public sealed class AuditActorTests
{
    [Fact]
    public void NestedScopesRestorePreviousActorAndRejectEmptyIds()
    {
        var context = new AuditActorContext();
        var actor = Guid.NewGuid();
        using (context.BeginScope(actor))
        {
            using (context.BeginScope(null))
                Assert.Null(context.ActorUserId);
            Assert.Equal(actor, context.ActorUserId);
            Assert.Throws<ArgumentException>(() => context.BeginScope(Guid.Empty));
            Assert.Equal(actor, context.ActorUserId);
        }
        Assert.Null(context.ActorUserId);
    }

    [Fact]
    public async Task ParallelAsyncFlowsDoNotShareOrLeakActors()
    {
        var context = new AuditActorContext();
        var firstActor = Guid.NewGuid();
        var secondActor = Guid.NewGuid();
        var outerActor = Guid.NewGuid();
        using var outerScope = context.BeginScope(outerActor);
        var results = await Task.WhenAll(ObserveAsync(firstActor), ObserveAsync(secondActor));
        Assert.Equal(new Guid?[] { firstActor, secondActor }, results);
        Assert.Equal(outerActor, context.ActorUserId);

        async Task<Guid?> ObserveAsync(Guid actor)
        {
            using var scope = context.BeginScope(actor);
            await Task.Yield();
            return context.ActorUserId;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MediatRScopesTrustedActorAndRestoresContextOnSuccessOrFailure(bool fail)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IUserRoleRepository, AllowedActorRepository>();
        services.AddTransient<IRequestHandler<AuditedProbe, Guid?>, AuditedProbeHandler>();
        using var provider = services.BuildServiceProvider();
        using var serviceScope = provider.CreateScope();
        var context = serviceScope.ServiceProvider.GetRequiredService<IAuditActorContext>();
        var mediator = serviceScope.ServiceProvider.GetRequiredService<IMediator>();
        var outerActor = Guid.NewGuid();
        var actor = Guid.NewGuid();
        using var scope = context.BeginScope(outerActor);
        if (fail)
            await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new AuditedProbe(actor, true)));
        else
            Assert.Equal(actor, await mediator.Send(new AuditedProbe(actor, false)));
        Assert.Equal(outerActor, context.ActorUserId);
    }

    [Fact]
    public void AuditBasesDoNotBecomeTablesAndEveryAuditedEntityHasItsOwnColumns()
    {
        using var db = new MatchingSportsDbContext(new DbContextOptionsBuilder<MatchingSportsDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation;Username=postgres;Password=postgres", postgres => postgres.UseNetTopologySuite()).Options);
        Assert.Null(db.Model.FindEntityType(typeof(AuditableEntity)));
        Assert.Null(db.Model.FindEntityType(typeof(ConcurrentAuditableEntity)));
        var entities = db.Model.GetEntityTypes().Where(entity => typeof(IAuditableEntity).IsAssignableFrom(entity.ClrType)).ToArray();
        Assert.Equal(22, entities.Length);
        foreach (var entity in entities)
        {
            Assert.Null(entity.BaseType);
            Assert.Null(entity.FindProperty("Discriminator"));
            Assert.False(entity.FindProperty(nameof(IAuditableEntity.CreatedAt))!.IsNullable);
            Assert.False(entity.FindProperty(nameof(IAuditableEntity.UpdatedAt))!.IsNullable);
            Assert.True(entity.FindProperty(nameof(IAuditableEntity.CreatedBy))!.IsNullable);
            Assert.True(entity.FindProperty(nameof(IAuditableEntity.UpdatedBy))!.IsNullable);
            var creatorProperty = entity.ClrType.GetProperty(nameof(IAuditableEntity.CreatedBy))!;
            Assert.True(creatorProperty.DeclaringType!.GetProperty(creatorProperty.Name)!.GetSetMethod(true)!.IsPrivate);
        }
        var concurrentEntities = entities.Where(entity => typeof(IHasConcurrencyToken).IsAssignableFrom(entity.ClrType)).ToArray();
        Assert.Equal(9, concurrentEntities.Length);
        Assert.All(concurrentEntities, entity => Assert.True(entity.FindProperty(nameof(IHasConcurrencyToken.ConcurrencyToken))!.IsConcurrencyToken));
        Assert.False(typeof(IAuditableEntity).IsAssignableFrom(typeof(AdminAction)));
        Assert.False(typeof(IAuditableEntity).IsAssignableFrom(typeof(RolePermission)));
    }

    public sealed record AuditedProbe(Guid ActorUserId, bool Fail) : IRequest<Guid?>, IPermissionAuthorizedRequest
    {
        IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.UpdateSports];
    }

    public sealed class AuditedProbeHandler(IAuditActorContext context) : IRequestHandler<AuditedProbe, Guid?>
    {
        public async Task<Guid?> Handle(AuditedProbe request, CancellationToken cancellationToken)
        {
            await Task.Yield();
            if (request.Fail)
                throw new InvalidOperationException("Probe failed.");
            return context.ActorUserId;
        }
    }

    private sealed class AllowedActorRepository : IUserRoleRepository
    {
        public Task<UserRoleAssignment?> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<UserRoleAssignment?>(new UserRoleAssignment(userId, [], [PermissionCodes.UpdateSports]));
        public Task ReplaceUserRolesAsync(UserRoleAssignment assignment, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
