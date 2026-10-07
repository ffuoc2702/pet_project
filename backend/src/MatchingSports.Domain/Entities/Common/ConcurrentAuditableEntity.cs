namespace MatchingSports.Domain.Entities;

public abstract class ConcurrentAuditableEntity : AuditableEntity, IHasConcurrencyToken
{
    public Guid ConcurrencyToken { get; private set; }

    protected void RenewConcurrencyToken() => ConcurrencyToken = Guid.NewGuid();
}
