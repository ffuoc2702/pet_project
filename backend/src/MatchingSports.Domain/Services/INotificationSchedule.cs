namespace MatchingSports.Domain.Services;

public interface INotificationSchedule
{
    DateTimeOffset? GetNextOccurrence(string cronString, string timeZoneId, DateTimeOffset from, bool inclusive = false);
}
