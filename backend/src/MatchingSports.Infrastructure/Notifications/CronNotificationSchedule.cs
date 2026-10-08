using Cronos;
using MatchingSports.Domain.Services;

namespace MatchingSports.Infrastructure.Notifications;

public sealed class CronNotificationSchedule : INotificationSchedule
{
    public DateTimeOffset? GetNextOccurrence(string cronString, string timeZoneId, DateTimeOffset from, bool inclusive = false)
    {
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return CronExpression.Parse(cronString, CronFormat.Standard).GetNextOccurrence(from.ToUniversalTime(), zone, inclusive)?.ToUniversalTime();
        }
        catch (CronFormatException) { throw new ArgumentException("Cron phải có năm trường: phút, giờ, ngày, tháng, thứ.", nameof(cronString)); }
    }
}
