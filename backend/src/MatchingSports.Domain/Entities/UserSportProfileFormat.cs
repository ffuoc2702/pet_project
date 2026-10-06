namespace MatchingSports.Domain.Entities;

public sealed class UserSportProfileFormat
{
    private UserSportProfileFormat() { }

    public Guid UserSportProfileId { get; private set; }
    public Guid FormatId { get; private set; }
    public Guid SportId { get; private set; }
}
