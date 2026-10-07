using System.Net;
using MatchingSports.Domain.Enums;

namespace MatchingSports.Domain.Entities;

public sealed class OtpChallenge : AuditableEntity
{
    public Guid Id { get; set; }
    public string NormalizedEmail { get; set; } = null!;
    public string OtpHash { get; set; } = null!;
    public OtpPurpose Purpose { get; set; } = OtpPurpose.EMAIL_VERIFICATION;
    public short AttemptCount { get; set; }
    public short MaxAttempts { get; set; } = 5;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public IPAddress? RequestIp { get; set; }
}
