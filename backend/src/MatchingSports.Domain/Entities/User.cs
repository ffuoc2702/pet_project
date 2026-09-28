using MatchingSports.Domain.Enums;

namespace MatchingSports.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; }
    public string PhoneE164 { get; set; } = null!;
    public DateTimeOffset? PhoneVerifiedAt { get; set; }
    public UserStatus Status { get; set; } = UserStatus.ACTIVE;
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public UserProfile? Profile { get; set; }
    public ICollection<UserSportProfile> SportProfiles { get; set; } = [];
    public ICollection<AuthSession> AuthSessions { get; set; } = [];
    public ICollection<Team> CaptainTeams { get; set; } = [];
    public ICollection<TeamMember> TeamMemberships { get; set; } = [];
    public ICollection<Listing> OwnedListings { get; set; } = [];
    public ICollection<ListingRequest> ApplicantRequests { get; set; } = [];
    public ICollection<ListingRequest> DecidedRequests { get; set; } = [];
    public ICollection<Match> CancelledMatches { get; set; } = [];
    public ICollection<MatchParticipant> MatchParticipations { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<Report> SubmittedReports { get; set; } = [];
    public ICollection<Report> AssignedReports { get; set; } = [];
    public ICollection<AdminAction> AdminActions { get; set; } = [];
}
