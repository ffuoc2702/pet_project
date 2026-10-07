using MatchingSports.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace MatchingSports.Domain.Entities;

public sealed class User : IdentityUser<Guid>, IAuditableEntity
{
    public static User CreateVerifiedAccount(string email, string displayName)
    {
        if (email is null)
            throw new ArgumentNullException(nameof(email), "Email không được để trống.");
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email không được để trống.", nameof(email));
        if (displayName is null)
            throw new ArgumentNullException(nameof(displayName), "Tên hiển thị không được để trống.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Tên hiển thị không được để trống.", nameof(displayName));
        var canonicalEmail = email.Trim().ToLowerInvariant();
        if (canonicalEmail.Length > 256)
            throw new ArgumentException("Email không được vượt quá 256 ký tự.", nameof(email));
        if (displayName.Trim().Length > 100)
            throw new ArgumentException("Tên hiển thị không được vượt quá 100 ký tự.", nameof(displayName));
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
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
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
