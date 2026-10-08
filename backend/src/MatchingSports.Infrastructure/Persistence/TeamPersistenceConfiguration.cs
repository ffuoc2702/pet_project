using MatchingSports.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MatchingSports.Infrastructure.Persistence;

internal static class TeamPersistenceConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<Team>().Property(x => x.ConcurrencyToken).HasDefaultValueSql("gen_random_uuid()").IsConcurrencyToken();
        var invitations = builder.Entity<TeamInvitation>();
        invitations.ToTable("team_invitations");
        invitations.HasKey(x => x.Id);
        invitations.Property(x => x.ConcurrencyToken).HasDefaultValueSql("gen_random_uuid()").IsConcurrencyToken();
        invitations.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        invitations.HasOne(x => x.Team).WithMany(x => x.Invitations).HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Restrict);
        invitations.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        invitations.HasIndex(x => new { x.TeamId, x.UserId, x.Status }).IsUnique().HasFilter("status = 'PENDING'");
        invitations.HasIndex(x => new { x.UserId, x.Status });
        invitations.HasQueryFilter(x => x.Team.DeletedAt == null && x.Team.CaptainUser.DeletedAt == null && x.User.DeletedAt == null);
        var requests = builder.Entity<TeamJoinRequest>();
        requests.ToTable("team_join_requests");
        requests.HasKey(x => x.Id);
        requests.Property(x => x.ConcurrencyToken).HasDefaultValueSql("gen_random_uuid()").IsConcurrencyToken();
        requests.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        requests.Property(x => x.Message).HasMaxLength(1000);
        requests.HasOne(x => x.Team).WithMany(x => x.JoinRequests).HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Restrict);
        requests.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        requests.HasIndex(x => new { x.TeamId, x.UserId, x.Status }).IsUnique().HasFilter("status = 'PENDING'");
        requests.HasIndex(x => new { x.UserId, x.Status });
        requests.HasQueryFilter(x => x.Team.DeletedAt == null && x.Team.CaptainUser.DeletedAt == null && x.User.DeletedAt == null);
    }
}
