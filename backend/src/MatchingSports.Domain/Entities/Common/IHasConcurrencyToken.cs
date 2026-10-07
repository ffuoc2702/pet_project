namespace MatchingSports.Domain.Entities;

public interface IHasConcurrencyToken
{
    Guid ConcurrencyToken { get; }
}
