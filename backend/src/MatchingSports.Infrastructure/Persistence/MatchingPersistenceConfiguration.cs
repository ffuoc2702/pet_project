using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MatchingSports.Infrastructure.Persistence;

internal static class MatchingPersistenceConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<Match>().HasIndex(match => new { match.Status, match.StartAt }, "ix_matches_due_reminder")
            .HasFilter("status = 'CONFIRMED' AND reminder_published_at IS NULL").HasDatabaseName("ix_matches_due_reminder");
        builder.Entity<SystemSettings>().Property(settings => settings.MatchReminderLeadMinutes).HasDefaultValue(60);
        builder.Entity<SystemSettings>().ToTable("system_settings", table =>
            table.HasCheckConstraint("ck_system_settings_match_reminder", "match_reminder_lead_minutes BETWEEN 1 AND 10080"));
        var watches = builder.Entity<ListingWatch>();
        watches.ToTable("listing_watches", table =>
        {
            table.HasCheckConstraint("ck_listing_watches_coordinates", "latitude BETWEEN -90 AND 90 AND longitude BETWEEN -180 AND 180");
            table.HasCheckConstraint("ck_listing_watches_criteria", "radius_meters BETWEEN 1 AND 200000 AND start_from < start_to");
        });
        watches.HasKey(watch => watch.Id);
        watches.Property(watch => watch.Id).ValueGeneratedNever();
        watches.Property(watch => watch.Status).HasConversion<string>().HasMaxLength(20);
        watches.Property(watch => watch.Latitude).HasPrecision(9, 6);
        watches.Property(watch => watch.Longitude).HasPrecision(9, 6);
        watches.Property(watch => watch.ConcurrencyToken).IsConcurrencyToken();
        watches.HasOne(watch => watch.User).WithMany().HasForeignKey(watch => watch.UserId).OnDelete(DeleteBehavior.Restrict);
        watches.HasOne(watch => watch.Sport).WithMany().HasForeignKey(watch => watch.SportId).OnDelete(DeleteBehavior.Restrict);
        watches.HasOne(watch => watch.Format).WithMany().HasForeignKey(watch => new { watch.FormatId, watch.SportId })
            .HasPrincipalKey(format => new { format.Id, format.SportId }).OnDelete(DeleteBehavior.Restrict);
        watches.HasIndex(watch => new { watch.UserId, watch.Status, watch.StartTo });
        watches.HasIndex(watch => new { watch.SportId, watch.Status, watch.StartFrom, watch.StartTo });
        watches.HasIndex(watch => new { watch.Status, watch.StartTo });
        watches.HasQueryFilter(watch => watch.User.DeletedAt == null);

        var deliveries = builder.Entity<ListingWatchDelivery>();
        deliveries.ToTable("listing_watch_deliveries");
        deliveries.HasKey(delivery => new { delivery.UserId, delivery.ListingId });
        deliveries.HasOne<User>().WithMany().HasForeignKey(delivery => delivery.UserId).OnDelete(DeleteBehavior.Restrict);
        deliveries.HasOne<Listing>().WithMany().HasForeignKey(delivery => delivery.ListingId).OnDelete(DeleteBehavior.Restrict);
        deliveries.HasOne<ListingWatch>().WithMany().HasForeignKey(delivery => delivery.WatchId).OnDelete(DeleteBehavior.Restrict);
        // NotificationHistoryId remains a historical UUID after notification retention cleanup.
        deliveries.HasIndex(delivery => delivery.WatchId);
        builder.Entity<SystemSettings>().ToTable("system_settings", table => table.HasCheckConstraint("ck_system_settings_listing_watches",
            "max_active_listing_watches_per_user BETWEEN 0 AND 1000 AND max_listing_search_radius_meters BETWEEN 1 AND 200000 AND max_listing_watch_window_days BETWEEN 1 AND 365"));
        builder.Entity<SystemSettings>().Property(settings => settings.MaxActiveListingWatchesPerUser).HasDefaultValue(20).HasSentinel(-1);
        builder.Entity<SystemSettings>().Property(settings => settings.MaxListingSearchRadiusMeters).HasDefaultValue(50000);
        builder.Entity<SystemSettings>().Property(settings => settings.MaxListingWatchWindowDays).HasDefaultValue(30);
    }
}
