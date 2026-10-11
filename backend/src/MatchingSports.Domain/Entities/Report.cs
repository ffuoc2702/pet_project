using MatchingSports.Domain.Enums;
using MatchingSports.Shared;

namespace MatchingSports.Domain.Entities;

public sealed class Report : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid ReporterUserId { get; set; }
    public ReportTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public ReportCategory Category { get; set; }
    private string _description = null!;
    public string Description
    {
        get => _description;
        set
        {
            if (value?.Length > ResourceLimits.DescriptionMaxLength)
                throw new ArgumentException($"Mô tả không được vượt quá {ResourceLimits.DescriptionMaxLength} ký tự.", nameof(value));
            _description = value!;
        }
    }
    public ReportStatus Status { get; set; } = ReportStatus.OPEN;
    public Guid? AssignedAdminId { get; set; }
    public string? Resolution { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }

    public User ReporterUser { get; set; } = null!;
    public User? AssignedAdmin { get; set; }
}
