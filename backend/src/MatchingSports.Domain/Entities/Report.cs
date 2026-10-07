using MatchingSports.Domain.Enums;

namespace MatchingSports.Domain.Entities;

public sealed class Report : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid ReporterUserId { get; set; }
    public ReportTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public ReportCategory Category { get; set; }
    public string Description { get; set; } = null!;
    public ReportStatus Status { get; set; } = ReportStatus.OPEN;
    public Guid? AssignedAdminId { get; set; }
    public string? Resolution { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }

    public User ReporterUser { get; set; } = null!;
    public User? AssignedAdmin { get; set; }
}
