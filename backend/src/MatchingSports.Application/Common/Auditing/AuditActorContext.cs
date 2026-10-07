using MatchingSports.Application.Abstractions;

namespace MatchingSports.Application.Common;

public sealed class AuditActorContext : IAuditActorContext
{
    private readonly AsyncLocal<Guid?> _actorUserId = new();

    public Guid? ActorUserId => _actorUserId.Value;

    public IDisposable BeginScope(Guid? actorUserId)
    {
        if (actorUserId == Guid.Empty)
            throw new ArgumentException("ID người thực hiện không được là Guid.Empty.", nameof(actorUserId));
        var previousActor = _actorUserId.Value;
        _actorUserId.Value = actorUserId;
        return new ActorScope(this, previousActor);
    }

    private sealed class ActorScope(AuditActorContext context, Guid? previousActor) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;
            context._actorUserId.Value = previousActor;
            _disposed = true;
        }
    }
}
