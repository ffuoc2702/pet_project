using MatchingSports.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace MatchingSports.Domain.Entities;

public sealed class User : IdentityUser<Guid>
{
    public static User CreateVerifiedAccount(string email, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        var canonicalEmail = email.Trim().ToLowerInvariant();
        if (canonicalEmail.Length > 256)
            throw new ArgumentException("Email must contain at most 256 characters.", nameof(email));
        if (displayName.Trim().Length > 100)
            throw new ArgumentException("Display name must contain at most 100 characters.", nameof(displayName));
        var userId = Guid.NewGuid();
        var profile = new UserProfile { UserId = userId };
        profile.UpdateDetails(displayName, null, null, null);
        return new User
        {
            Id = userId,
            UserName = canonicalEmail,
            Email = canonicalEmail,
            EmailConfirmed = true,
            Status = UserStatus.ACTIVE,
            Profile = profile
        };
    }

    public bool CanReceiveInitialAdministratorRole() =>
        Status == UserStatus.ACTIVE && DeletedAt is null && EmailConfirmed;

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
