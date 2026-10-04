namespace MatchingSports.Application.Abstractions;

public interface ICurrentUser
{
    Guid UserId { get; }
}
