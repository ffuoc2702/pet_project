using MatchingSports.Domain.Enums;

namespace MatchingSports.Domain.Entities;

public sealed class Team : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid SportId { get; set; }
    public string Name { get; set; } = null!;
    public Guid CaptainUserId { get; set; }
    public string? Description { get; set; }
    public string? HomeArea { get; set; }
    public string? AvatarUrl { get; set; }
    public TeamStatus Status { get; set; } = TeamStatus.ACTIVE;
    public DateTimeOffset? DeletedAt { get; set; }

    public Sport Sport { get; set; } = null!;
    public User CaptainUser { get; set; } = null!;
    public ICollection<TeamMember> Members { get; set; } = [];
    public ICollection<Listing> HostedListings { get; set; } = [];
    public ICollection<ListingRequest> ApplicantRequests { get; set; } = [];
    public ICollection<MatchParticipant> MatchParticipants { get; set; } = [];
}
