using System.Net;

namespace MatchingSports.Domain.Entities;

public sealed class AuthSession : ConcurrentAuditableEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string RefreshTokenHash { get; set; } = null!;
    public string? DeviceInfo { get; set; }
    public IPAddress? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public User User { get; set; } = null!;
}
