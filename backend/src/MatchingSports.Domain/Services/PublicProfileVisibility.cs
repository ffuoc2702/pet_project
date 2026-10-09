using System.Linq.Expressions;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;

namespace MatchingSports.Domain.Services;

public static class PublicProfileVisibility
{
    public static Expression<Func<User, bool>> Users { get; } = user =>
        user.DeletedAt == null && user.Status == UserStatus.ACTIVE && user.EmailConfirmed && user.Profile != null;

    public static Expression<Func<Team, bool>> Teams { get; } = team =>
        team.DeletedAt == null && team.Status == TeamStatus.ACTIVE && team.Sport.IsActive
        && team.CaptainUser.DeletedAt == null && team.CaptainUser.Status == UserStatus.ACTIVE
        && team.CaptainUser.EmailConfirmed && team.CaptainUser.Profile != null;
}
