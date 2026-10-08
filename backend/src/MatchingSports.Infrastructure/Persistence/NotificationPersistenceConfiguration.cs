using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MatchingSports.Infrastructure.Persistence;

internal static class NotificationPersistenceConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        var config = builder.Entity<NotificationConfig>();
        config.ToTable("notification_configs", table =>
        {
            table.HasCheckConstraint("ck_notification_configs_counts", "send_count >= 0 AND (max_send_times IS NULL OR max_send_times > 0)");
            table.HasCheckConstraint("ck_notification_configs_window", "expired_after IS NULL OR expired_after > available_at");
            table.HasCheckConstraint("ck_notification_configs_channels", "in_app_enabled OR email_enabled OR push_enabled OR sms_enabled");
            table.HasCheckConstraint("ck_notification_configs_schedule", "(schedule_type = 'ONCE' AND cron_string IS NULL AND max_send_times = 1) OR (schedule_type = 'SCHEDULED' AND cron_string IS NOT NULL)");
            table.HasCheckConstraint("ck_notification_configs_lease", "(locked_until IS NULL) = (lease_token IS NULL)");
        });
        config.HasKey(x => x.Id);
        config.Property(x => x.Id).ValueGeneratedNever();
        config.Property(x => x.ConcurrencyToken).HasDefaultValueSql("gen_random_uuid()").IsConcurrencyToken();
        config.Property(x => x.Title).HasMaxLength(160);
        config.Property(x => x.Body).HasMaxLength(100000);
        config.Property(x => x.CronString).HasMaxLength(120);
        config.Property(x => x.TimeZoneId).HasMaxLength(100);
        config.Property(x => x.AbortReason).HasMaxLength(200);
        config.Property(x => x.Metadata).HasColumnType("jsonb");
        config.Property(x => x.ScheduleType).HasConversion<string>().HasMaxLength(20);
        config.Property(x => x.Critical).HasConversion<string>().HasMaxLength(10);
        config.Property(x => x.AudienceType).HasConversion<string>().HasMaxLength(30);
        config.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        config.Property(x => x.SchedulingStatus).HasConversion<string>().HasMaxLength(20);
        config.HasIndex(x => new { x.NextRunAt, x.Id }).HasFilter("status = 'ACTIVE' AND scheduling_status IN ('PENDING', 'SCHEDULING')")
            .HasDatabaseName("ix_notification_configs_due");
        config.HasMany(x => x.Users).WithOne().HasForeignKey(x => x.NotificationConfigId).OnDelete(DeleteBehavior.Cascade);
        config.HasMany(x => x.Teams).WithOne().HasForeignKey(x => x.NotificationConfigId).OnDelete(DeleteBehavior.Cascade);
        config.Navigation(x => x.Users).HasField("_users").UsePropertyAccessMode(PropertyAccessMode.Field);
        config.Navigation(x => x.Teams).HasField("_teams").UsePropertyAccessMode(PropertyAccessMode.Field);
        var users = builder.Entity<NotificationConfigUser>();
        users.ToTable("notification_config_users");
        users.HasKey(x => new { x.NotificationConfigId, x.UserId });
        users.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        var teams = builder.Entity<NotificationConfigTeam>();
        teams.ToTable("notification_config_teams");
        teams.HasKey(x => new { x.NotificationConfigId, x.TeamId });
        teams.HasOne<Team>().WithMany().HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Cascade);

        var history = builder.Entity<NotificationHistory>();
        history.HasOne<NotificationConfig>().WithMany().HasForeignKey(x => x.NotificationConfigId).OnDelete(DeleteBehavior.Restrict);
        history.HasOne<Team>().WithMany().HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.SetNull);
        history.Property(x => x.Critical).HasConversion<string>().HasMaxLength(10).HasDefaultValue(NotificationCritical.INFO);
        history.Property(x => x.EmailStatus).HasConversion<string>().HasMaxLength(20).HasDefaultValue(NotificationDeliveryStatus.DISABLED);
        history.Property(x => x.PushStatus).HasConversion<string>().HasMaxLength(20).HasDefaultValue(NotificationDeliveryStatus.DISABLED);
        history.Property(x => x.SmsStatus).HasConversion<string>().HasMaxLength(20).HasDefaultValue(NotificationDeliveryStatus.DISABLED);
        history.Property(x => x.InAppEnabled).HasDefaultValue(true).HasSentinel(true);
        history.Property(x => x.BodyText).HasDefaultValue(string.Empty);
        history.Property(x => x.EmailErrorCode).HasMaxLength(100);
        history.Property(x => x.PushErrorCode).HasMaxLength(100);
        history.Property(x => x.SmsErrorCode).HasMaxLength(100);
        history.HasIndex(x => new { x.NotificationConfigId, x.SendNumber, x.UserId }).IsUnique()
            .HasFilter("notification_config_id IS NOT NULL AND user_id IS NOT NULL").HasDatabaseName("uq_notification_history_recipient");
        history.HasIndex(x => new { x.NotificationConfigId, x.CreatedAt }).HasDatabaseName("ix_notification_history_config");
    }
}
