namespace MatchingSports.Application.Common;

public abstract record ConcurrentBaseDto : BaseDto
{
    public Guid ConcurrencyToken { get; init; }
}
