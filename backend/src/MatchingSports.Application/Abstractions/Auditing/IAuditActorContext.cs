namespace MatchingSports.Application.Abstractions;

public interface IAuditActorContext
{
    Guid? ActorUserId { get; }
    IDisposable BeginScope(Guid? actorUserId);
}
