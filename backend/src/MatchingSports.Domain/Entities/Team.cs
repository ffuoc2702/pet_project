using MatchingSports.Domain.Enums;
using MatchingSports.Shared;

namespace MatchingSports.Domain.Entities;

public sealed class Team : ConcurrentAuditableEntity
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
    public ICollection<TeamInvitation> Invitations { get; private set; } = [];
    public ICollection<TeamJoinRequest> JoinRequests { get; private set; } = [];

    public static Team Create(Guid sportId, Guid captainId, string name, string? description,
        string? homeArea, string? avatarUrl, DateTimeOffset now, int captainTeamCount, SystemSettings settings)
    {
        TeamRules.Require(sportId != Guid.Empty && captainId != Guid.Empty, "Môn thể thao hoặc đội trưởng không hợp lệ.");
        TeamRules.RequireCaptainCapacity(captainTeamCount, settings);
        var team = new Team { Id = Guid.NewGuid(), SportId = sportId, CaptainUserId = captainId };
        team.UpdateDetails(name, description, homeArea, avatarUrl);
        team.Members.Add(TeamMember.Join(team.Id, captainId, TeamRole.CAPTAIN, now));
        return team;
    }

    public bool IsCaptain(Guid userId) => CaptainUserId == userId && DeletedAt is null;
    public bool IsMember(Guid userId) => Members.Any(x => x.UserId == userId && x.Status == MemberStatus.ACTIVE);
    public bool CanManageMembers(Guid userId) => IsCaptain(userId) || Members.Any(x =>
        x.UserId == userId && x.Status == MemberStatus.ACTIVE && x.Role == TeamRole.DEPUTY);
    public IReadOnlyCollection<Guid> ManagementUserIds() => Members.Where(x => x.Status == MemberStatus.ACTIVE
        && x.Role == TeamRole.DEPUTY).Select(x => x.UserId).Append(CaptainUserId).Distinct().ToArray();
    public IReadOnlyCollection<Guid> ActiveUserIds() => Members.Where(x => x.Status == MemberStatus.ACTIVE)
        .Select(x => x.UserId).Append(CaptainUserId).Distinct().ToArray();
    public void RequireActive() => TeamRules.Require(DeletedAt is null && Status == TeamStatus.ACTIVE, "Đội không hoạt động.");

    public void UpdateDetails(string name, string? description, string? homeArea, string? avatarUrl)
    {
        RequireActive();
        TeamRules.Require(!string.IsNullOrWhiteSpace(name) && name.Trim().Length <= 120
            && (description is null || description.Length <= 2000) && (homeArea is null || homeArea.Length <= 150)
            && (avatarUrl is null || avatarUrl.Length <= 2048), "Thông tin đội không hợp lệ.");
        Name = name.Trim(); Description = StringNormalizer.StartEndSpaceTrim(description);
        HomeArea = StringNormalizer.StartEndSpaceTrim(homeArea); AvatarUrl = StringNormalizer.StartEndSpaceTrim(avatarUrl);
        RenewConcurrencyToken();
    }

    public void RequireCanApply(Guid userId)
    {
        RequireActive();
        TeamRules.Require(userId != Guid.Empty && userId != CaptainUserId && !IsMember(userId), "Người dùng đã là thành viên đội.");
    }

    public TeamInvitation Invite(Guid actorId, Guid userId, DateTimeOffset now, SystemSettings settings)
    {
        RequireCanApply(userId);
        TeamRules.Require(!Invitations.Any(x => x.UserId == userId && x.Status == TeamRequestStatus.PENDING && x.ExpiresAt > now), "Đã có lời mời đang chờ.");
        RequirePendingCapacity(now, settings);
        var invitationToAdd = TeamInvitation.Create(Id, userId, actorId, now, settings);
        foreach (var invitation in Invitations) invitation.Expire(now);
        Invitations.Add(invitationToAdd); RenewConcurrencyToken();
        return invitationToAdd;
    }

    public TeamJoinRequest RequestToJoin(Guid userId, string? message, DateTimeOffset now, SystemSettings settings)
    {
        RequireCanApply(userId);
        TeamRules.Require(!JoinRequests.Any(x => x.UserId == userId && x.Status == TeamRequestStatus.PENDING), "Đã có yêu cầu đang chờ.");
        RequirePendingCapacity(now, settings);
        var request = TeamJoinRequest.Create(Id, userId, message);
        foreach (var invitation in Invitations) invitation.Expire(now);
        JoinRequests.Add(request); RenewConcurrencyToken();
        return request;
    }

    private void RequirePendingCapacity(DateTimeOffset now, SystemSettings settings) =>
        TeamRules.ValidateCapacity(() => settings.EnsureCanCreateTeamRequest(
            Invitations.Count(x => x.Status == TeamRequestStatus.PENDING && x.ExpiresAt > now)
            + JoinRequests.Count(x => x.Status == TeamRequestStatus.PENDING)));

    public void AddMember(Guid userId, DateTimeOffset now, int memberTeamCount, SystemSettings settings)
    {
        RequireCanApply(userId);
        TeamRules.ValidateCapacity(() => settings.EnsureCanJoinAnotherTeam(memberTeamCount, ActiveUserIds().Count));
        var member = Members.SingleOrDefault(x => x.UserId == userId);
        if (member is null) Members.Add(TeamMember.Join(Id, userId, TeamRole.MEMBER, now));
        else member.Rejoin(now);
        RenewConcurrencyToken();
    }

    public void AppointDeputy(Guid userId, SystemSettings settings)
    {
        RequireActive(); var member = RequireOrdinaryMember(userId);
        TeamRules.Require(member.Role == TeamRole.MEMBER, "Thành viên đã là đội phó.");
        TeamRules.ValidateCapacity(() => settings.EnsureCanDelegateAnotherDeputy(
            Members.Count(x => x.Status == MemberStatus.ACTIVE && x.Role == TeamRole.DEPUTY)));
        member.ChangeRole(TeamRole.DEPUTY); RenewConcurrencyToken();
    }
    public void RevokeDeputy(Guid userId)
    {
        RequireActive(); var member = RequireOrdinaryMember(userId);
        TeamRules.Require(member.Role == TeamRole.DEPUTY, "Thành viên không phải đội phó.");
        member.ChangeRole(TeamRole.MEMBER); RenewConcurrencyToken();
    }
    public void RemoveMember(Guid actorId, Guid userId, DateTimeOffset now)
    {
        RequireActive(); var member = RequireOrdinaryMember(userId);
        TeamRules.Require(IsCaptain(actorId) || (CanManageMembers(actorId) && member.Role == TeamRole.MEMBER),
            "Đội phó chỉ được loại thành viên thường.");
        TeamRules.Require(actorId != userId, "Sử dụng thao tác rời đội để tự rời.");
        member.Leave(MemberStatus.REMOVED, now); RenewConcurrencyToken();
    }
    public void Leave(Guid userId, DateTimeOffset now)
    {
        RequireActive(); var member = RequireOrdinaryMember(userId);
        member.Leave(MemberStatus.LEFT, now); RenewConcurrencyToken();
    }
    public void TransferCaptain(Guid newCaptainId, int newCaptainTeamCount, int oldCaptainMemberTeamCount, SystemSettings settings)
    {
        RequireActive(); var successor = RequireOrdinaryMember(newCaptainId);
        var predecessor = Members.SingleOrDefault(x => x.UserId == CaptainUserId && x.Status == MemberStatus.ACTIVE);
        TeamRules.Require(predecessor is not null, "Membership đội trưởng không hợp lệ.");
        TeamRules.RequireCaptainCapacity(newCaptainTeamCount, settings);
        TeamRules.RequireMemberCapacity(oldCaptainMemberTeamCount, settings);
        predecessor!.ChangeRole(TeamRole.MEMBER); successor.ChangeRole(TeamRole.CAPTAIN);
        CaptainUserId = newCaptainId; RenewConcurrencyToken();
    }
    public void Delete(DateTimeOffset now, bool hasOpenCommitments)
    {
        RequireActive();
        TeamRules.Require(Members.Any(x => x.UserId == CaptainUserId && x.Status == MemberStatus.ACTIVE && x.Role == TeamRole.CAPTAIN)
            && ActiveUserIds().Count == 1, "Chỉ được xóa đội khi chỉ còn đội trưởng.");
        TeamRules.Require(!hasOpenCommitments, "Đội còn kèo hoặc trận chưa kết thúc.");
        DeletedAt = now; Status = TeamStatus.ARCHIVED; RenewConcurrencyToken();
    }
    private TeamMember RequireOrdinaryMember(Guid userId)
    {
        TeamRules.Require(userId != CaptainUserId, "Không được tác động đến đội trưởng bằng thao tác này.");
        var member = Members.SingleOrDefault(x => x.UserId == userId && x.Status == MemberStatus.ACTIVE);
        TeamRules.Require(member is not null, "Không tìm thấy thành viên đang hoạt động.");
        return member!;
    }

    public ICollection<Listing> HostedListings { get; set; } = [];
    public ICollection<ListingRequest> ApplicantRequests { get; set; } = [];
    public ICollection<MatchParticipant> MatchParticipants { get; set; } = [];
}
