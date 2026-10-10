using System.Text;
using MatchingSports.Application.Abstractions;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NetTopologySuite.Geometries;

namespace MatchingSports.Infrastructure.Persistence;

public sealed class MatchingSportsDbContext(DbContextOptions<MatchingSportsDbContext> options,
    IAuditActorContext? auditActorContext = null, TimeProvider? timeProvider = null)
    : IdentityDbContext<User, Role, Guid>(options)
{
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Sport> Sports => Set<Sport>();
    public DbSet<SportFormat> SportFormats => Set<SportFormat>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserSportProfile> UserSportProfiles => Set<UserSportProfile>();
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();
    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<TeamInvitation> TeamInvitations => Set<TeamInvitation>();
    public DbSet<TeamJoinRequest> TeamJoinRequests => Set<TeamJoinRequest>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ListingWatch> ListingWatches => Set<ListingWatch>();
    public DbSet<ListingWatchDelivery> ListingWatchDeliveries => Set<ListingWatchDelivery>();
    public DbSet<ListingRequest> ListingRequests => Set<ListingRequest>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchParticipant> MatchParticipants => Set<MatchParticipant>();
    public DbSet<NotificationHistory> NotificationHistories => Set<NotificationHistory>();
    public DbSet<NotificationConfig> NotificationConfigs => Set<NotificationConfig>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<AdminAction> AdminActions => Set<AdminAction>();
    public DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Ignore<AuditableEntity>();
        builder.Ignore<ConcurrentAuditableEntity>();
        builder.HasPostgresExtension("pgcrypto");
        builder.HasPostgresExtension("postgis");

        ConfigureCatalog(builder);
        var preferences = builder.Entity<UserPreferences>();
        preferences.ToTable("user_preferences");
        preferences.HasKey(x => x.UserId);
        preferences.HasOne<User>().WithOne().HasForeignKey<UserPreferences>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        preferences.Property(x => x.LanguageCode).HasMaxLength(2).IsRequired();
        preferences.Property(x => x.ColorMode).HasMaxLength(10).IsRequired();
        preferences.Property(x => x.ThemePreset).HasMaxLength(20).IsRequired();
        preferences.Property(x => x.ConcurrencyToken).IsConcurrencyToken();
        builder.Entity<Sport>().Property(x => x.Description).HasMaxLength(2000);
        builder.Entity<SportFormat>().Property(x => x.Description).HasMaxLength(2000);
        builder.Entity<Permission>().Property(x => x.Description).HasMaxLength(2000);
        builder.Entity<Listing>().Property(x => x.Description).HasMaxLength(4000);
        builder.Entity<NotificationConfig>().Property(x => x.Description).HasMaxLength(2000);
        ConfigureIdentityAndProfiles(builder);
        ConfigureTeams(builder);
        TeamPersistenceConfiguration.Configure(builder);
        ConfigureListingsAndMatching(builder);
        ConfigureNotificationsAndSafety(builder);
        NotificationPersistenceConfiguration.Configure(builder);
        ConfigureSystemSettings(builder);
        MatchingPersistenceConfiguration.Configure(builder);

        foreach (var entity in builder.Model.GetEntityTypes().Where(entity => typeof(IAuditableEntity).IsAssignableFrom(entity.ClrType)).ToArray())
        {
            // Reuse CLR properties without introducing an EF inheritance hierarchy or base table.
            var configuration = builder.Entity(entity.ClrType).HasBaseType((Type?)null);
            configuration.Property(nameof(IAuditableEntity.CreatedAt)).HasDefaultValueSql("now()");
            configuration.Property(nameof(IAuditableEntity.UpdatedAt)).HasDefaultValueSql("now()");
        }

        foreach (var entity in builder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var settings = NeedsRetentionInitialization() ? GetPendingSettingsUpdate() ??
            SystemSettings.AsNoTracking().Single(settings => settings.Id == Domain.Entities.SystemSettings.SingletonId) : null;
        ApplyPersistenceConventions();
        InitializeRetention(settings);
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var settings = NeedsRetentionInitialization() ? GetPendingSettingsUpdate() ??
            await SystemSettings.AsNoTracking().SingleAsync(settings => settings.Id == Domain.Entities.SystemSettings.SingletonId, cancellationToken) : null;
        ApplyPersistenceConventions();
        InitializeRetention(settings);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private bool NeedsRetentionInitialization() =>
        ChangeTracker.Entries<NotificationHistory>().Any(entry => entry.State == EntityState.Added) ||
        ChangeTracker.Entries<OutboxMessage>().Any(entry => entry.State == EntityState.Added);

    private SystemSettings? GetPendingSettingsUpdate() =>
        ChangeTracker.Entries<SystemSettings>().FirstOrDefault(entry => entry.State is EntityState.Added or EntityState.Modified)?.Entity;

    private void InitializeRetention(SystemSettings? settings)
    {
        if (settings is null)
            return;
        // CreatedAt has been finalized by persistence conventions before calculating retention.
        foreach (var entry in ChangeTracker.Entries<NotificationHistory>().Where(entry => entry.State == EntityState.Added))
            entry.Entity.InitializeRetention(settings);
        foreach (var entry in ChangeTracker.Entries<OutboxMessage>().Where(entry => entry.State == EntityState.Added))
            entry.Entity.InitializeRetention(settings);
    }

    private static void ConfigureSystemSettings(ModelBuilder modelBuilder)
    {
        var settings = modelBuilder.Entity<SystemSettings>();
        settings.ToTable("system_settings", table =>
        {
            table.HasCheckConstraint("ck_system_settings_singleton", "id = '00000000-0000-0000-0002-000000000001'::uuid");
            table.HasCheckConstraint("ck_system_settings_limits",
                "outbox_max_retry_count BETWEEN 0 AND 100 AND max_captain_teams_per_user BETWEEN 0 AND 10000 " +
                "AND max_member_teams_per_user BETWEEN 0 AND 10000 AND max_members_per_team BETWEEN 1 AND 10000 " +
                "AND max_deputies_per_team >= 0 AND max_deputies_per_team < max_members_per_team " +
                "AND team_invitation_lifetime_hours BETWEEN 1 AND 8760 AND max_pending_team_requests BETWEEN 0 AND 10000 " +
                "AND outbox_retry_initial_delay_seconds BETWEEN 1 AND 604800 " +
                "AND outbox_retry_max_delay_seconds BETWEEN outbox_retry_initial_delay_seconds AND 604800 " +
                "AND processed_outbox_retention_days BETWEEN 1 AND 36500 AND notification_retention_days BETWEEN 1 AND 36500");
        });
        settings.HasKey(x => x.Id);
        settings.Property(x => x.Id).ValueGeneratedNever();
        settings.Property(x => x.ConcurrencyToken).HasDefaultValueSql("gen_random_uuid()").IsConcurrencyToken();
        settings.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        settings.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
    }

    private static void ConfigureCatalog(ModelBuilder modelBuilder)
    {
        var sports = modelBuilder.Entity<Sport>();
        sports.ToTable("sports");
        sports.HasKey(x => x.Id);
        sports.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        sports.Property(x => x.Code).HasMaxLength(32);
        sports.Property(x => x.Name).HasMaxLength(100);
        sports.Property(x => x.IsActive).HasDefaultValue(true);
        sports.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        sports.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        sports.HasIndex(x => x.Code).IsUnique().HasFilter("is_active").HasDatabaseName("uq_sports_code");
        sports.Property<string>("NormalizedName").HasComputedColumnSql("lower(btrim(name))", stored: true);
        sports.HasIndex("NormalizedName").IsUnique().HasFilter("is_active").HasDatabaseName("uq_sports_name");

        var formats = modelBuilder.Entity<SportFormat>();
        formats.ToTable("sport_formats", t => t.HasCheckConstraint("ck_sport_formats_side_size", "side_size > 0"));
        formats.HasKey(x => x.Id);
        formats.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        formats.Property(x => x.Code).HasMaxLength(32);
        formats.Property(x => x.DisplayName).HasMaxLength(100);
        formats.Property(x => x.IsActive).HasDefaultValue(true);
        formats.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        formats.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        formats.HasOne(x => x.Sport).WithMany(x => x.Formats).HasForeignKey(x => x.SportId).OnDelete(DeleteBehavior.Restrict);
        formats.HasIndex(x => new { x.SportId, x.Code }).IsUnique().HasDatabaseName("uq_sport_formats_sport_code");
        formats.Property<string>("NormalizedName").HasComputedColumnSql("lower(btrim(display_name))", stored: true);
        formats.HasIndex(nameof(SportFormat.SportId), "NormalizedName").IsUnique().HasDatabaseName("uq_sport_formats_sport_name");
        formats.HasAlternateKey(x => new { x.Id, x.SportId }).HasName("ak_sport_formats_id_sport_id");
    }

    private static void ConfigureIdentityAndProfiles(ModelBuilder modelBuilder)
    {
        var users = modelBuilder.Entity<User>();
        users.ToTable("users");
        users.HasKey(x => x.Id);
        users.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        users.Property(x => x.Email).HasMaxLength(256).IsRequired();
        users.Property(x => x.NormalizedEmail).HasMaxLength(256).IsRequired();
        users.Property(x => x.UserName).HasMaxLength(256).IsRequired();
        users.Property(x => x.NormalizedUserName).HasMaxLength(256).IsRequired();
        users.Ignore(x => x.PhoneNumber);
        users.Ignore(x => x.PhoneNumberConfirmed);
        users.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValueSql("'ACTIVE'").HasSentinel((UserStatus)(-1));
        users.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        users.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        users.HasIndex(x => x.NormalizedEmail).IsUnique().HasFilter("deleted_at IS NULL").HasDatabaseName("uq_users_normalized_email");
        users.HasIndex(x => x.NormalizedUserName).IsUnique().HasFilter("deleted_at IS NULL").HasDatabaseName("uq_users_normalized_user_name");
        users.HasQueryFilter(x => x.DeletedAt == null);

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(x => x.PermissionsInitialized).HasDefaultValue(false);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.IsActive).HasDefaultValue(true).HasSentinel(true);
            entity.Property(x => x.IsDefaultForRegistration).HasDefaultValue(false);
            entity.Property(x => x.RegistrationDefaultInitialized).HasDefaultValue(false);
            entity.Property(x => x.SeedKey).HasMaxLength(100);
            entity.Property(x => x.BootstrapCompleted).HasDefaultValue(false);
            entity.HasIndex(x => x.SeedKey).IsUnique();
        });
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");

        var permissions = modelBuilder.Entity<Permission>();
        permissions.ToTable("permissions");
        permissions.HasKey(x => x.Id);
        permissions.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        permissions.Property(x => x.Code).HasMaxLength(100).IsRequired();
        permissions.Property(x => x.Name).HasMaxLength(150).IsRequired();
        permissions.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        permissions.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        permissions.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_permissions_code");

        var rolePermissions = modelBuilder.Entity<RolePermission>();
        rolePermissions.ToTable("role_permissions");
        rolePermissions.HasKey(x => new { x.RoleId, x.PermissionId });
        rolePermissions.HasOne(x => x.Role).WithMany(x => x.Permissions)
            .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        rolePermissions.HasOne(x => x.Permission).WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Role>().Navigation(x => x.Permissions).HasField("_permissions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        permissions.Navigation(x => x.RolePermissions).HasField("_rolePermissions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        var profiles = modelBuilder.Entity<UserProfile>();
        profiles.ToTable("user_profiles");
        profiles.HasKey(x => x.UserId);
        profiles.Property(x => x.DisplayName).HasMaxLength(100);
        profiles.Property(x => x.DefaultArea).HasMaxLength(150);
        profiles.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        profiles.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        profiles.HasOne(x => x.User).WithOne(x => x.Profile).HasForeignKey<UserProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        profiles.HasQueryFilter(x => x.User.DeletedAt == null);

        var sportProfiles = modelBuilder.Entity<UserSportProfile>();
        sportProfiles.ToTable("user_sport_profiles");
        sportProfiles.HasKey(x => x.Id);
        sportProfiles.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        sportProfiles.Property(x => x.Level).HasConversion<string>().HasMaxLength(20).HasDefaultValueSql("'BEGINNER'").HasSentinel((SportLevel)(-1));
        sportProfiles.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        sportProfiles.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        sportProfiles.HasOne(x => x.User).WithMany(x => x.SportProfiles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        sportProfiles.HasOne(x => x.Sport).WithMany(x => x.UserSportProfiles).HasForeignKey(x => x.SportId).OnDelete(DeleteBehavior.Restrict);
        sportProfiles.HasIndex(x => new { x.UserId, x.SportId }).IsUnique().HasDatabaseName("uq_user_sport_profiles_user_sport");
        sportProfiles.HasQueryFilter(x => x.User.DeletedAt == null);
        sportProfiles.HasAlternateKey(x => new { x.Id, x.SportId }).HasName("ak_user_sport_profiles_id_sport_id");
        sportProfiles.HasMany(x => x.PreferredFormats).WithMany()
            .UsingEntity<UserSportProfileFormat>(
                formats => formats.HasOne<SportFormat>().WithMany()
                    .HasForeignKey(x => new { x.FormatId, x.SportId })
                    .HasPrincipalKey(x => new { x.Id, x.SportId })
                    .OnDelete(DeleteBehavior.Restrict),
                profiles => profiles.HasOne<UserSportProfile>().WithMany()
                    .HasForeignKey(x => new { x.UserSportProfileId, x.SportId })
                    .HasPrincipalKey(x => new { x.Id, x.SportId })
                    .OnDelete(DeleteBehavior.Cascade),
                preferences =>
                {
                    preferences.ToTable("user_sport_profile_formats");
                    preferences.HasKey(x => new { x.UserSportProfileId, x.FormatId });
                });

        var otp = modelBuilder.Entity<OtpChallenge>();
        otp.ToTable("otp_challenges");
        otp.HasKey(x => x.Id);
        otp.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        otp.Property(x => x.NormalizedEmail).HasMaxLength(256);
        otp.Property(x => x.OtpHash).HasColumnType("char(64)");
        otp.Property(x => x.Purpose).HasConversion<string>().HasMaxLength(24).HasDefaultValueSql("'EMAIL_VERIFICATION'").HasSentinel((OtpPurpose)(-1));
        otp.Property(x => x.AttemptCount).HasDefaultValue((short)0);
        otp.Property(x => x.MaxAttempts).HasDefaultValue((short)5);
        otp.Property(x => x.RequestIp).HasColumnType("inet");
        otp.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        otp.HasIndex(x => new { x.NormalizedEmail, x.Purpose, x.CreatedAt }).IsDescending(false, false, true)
            .HasFilter("verified_at IS NULL").HasDatabaseName("ix_otp_challenges_lookup");
        otp.HasIndex(x => x.ExpiresAt).HasFilter("verified_at IS NULL").HasDatabaseName("ix_otp_challenges_expiry");

        var sessions = modelBuilder.Entity<AuthSession>();
        sessions.ToTable("auth_sessions");
        sessions.HasKey(x => x.Id);
        sessions.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        sessions.Property(x => x.RefreshTokenHash).HasColumnType("char(64)");
        sessions.Property(x => x.DeviceInfo).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        sessions.Property(x => x.IpAddress).HasColumnType("inet");
        sessions.Property(x => x.ConcurrencyToken)
            .HasDefaultValueSql("gen_random_uuid()")
            .IsConcurrencyToken();
        sessions.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        sessions.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        sessions.HasOne(x => x.User).WithMany(x => x.AuthSessions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        sessions.HasIndex(x => x.RefreshTokenHash).IsUnique().HasDatabaseName("uq_auth_sessions_token_hash");
        sessions.HasIndex(x => new { x.UserId, x.ExpiresAt }).HasFilter("revoked_at IS NULL").HasDatabaseName("ix_auth_sessions_user_active");
        sessions.HasQueryFilter(x => x.User.DeletedAt == null);
    }

    private static void ConfigureTeams(ModelBuilder modelBuilder)
    {
        var teams = modelBuilder.Entity<Team>();
        teams.ToTable("teams");
        teams.HasKey(x => x.Id);
        teams.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        teams.Property(x => x.Name).HasMaxLength(120);
        teams.Property(x => x.HomeArea).HasMaxLength(150);
        teams.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValueSql("'ACTIVE'").HasSentinel((TeamStatus)(-1));
        teams.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        teams.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        teams.HasOne(x => x.Sport).WithMany(x => x.Teams).HasForeignKey(x => x.SportId).OnDelete(DeleteBehavior.Restrict);
        teams.HasOne(x => x.CaptainUser).WithMany(x => x.CaptainTeams).HasForeignKey(x => x.CaptainUserId).OnDelete(DeleteBehavior.Restrict);
        teams.HasIndex(x => new { x.SportId, x.Status }).HasFilter("deleted_at IS NULL").HasDatabaseName("ix_teams_sport_status");
        teams.HasQueryFilter(x => x.DeletedAt == null && x.CaptainUser.DeletedAt == null);

        var members = modelBuilder.Entity<TeamMember>();
        members.ToTable("team_members");
        members.HasKey(x => new { x.TeamId, x.UserId }).HasName("pk_team_members");
        members.Property(x => x.Role).HasConversion<string>().HasMaxLength(20).HasDefaultValueSql("'MEMBER'").HasSentinel((TeamRole)(-1));
        members.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValueSql("'ACTIVE'").HasSentinel((MemberStatus)(-1));
        members.Property(x => x.JoinedAt).HasDefaultValueSql("now()");
        members.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        members.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        members.HasOne(x => x.Team).WithMany(x => x.Members).HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Cascade);
        members.HasOne(x => x.User).WithMany(x => x.TeamMemberships).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        members.HasIndex(x => new { x.UserId, x.Status }).HasDatabaseName("ix_team_members_user_status");
        members.HasQueryFilter(x => x.Team.DeletedAt == null && x.User.DeletedAt == null);
    }

    private static void ConfigureListingsAndMatching(ModelBuilder modelBuilder)
    {
        var listings = modelBuilder.Entity<Listing>();
        listings.ToTable("listings", t =>
        {
            t.HasCheckConstraint("ck_listings_slots", "open_slots > 0 AND accepted_slots BETWEEN 0 AND open_slots");
            t.HasCheckConstraint("ck_listings_time", "duration_minutes BETWEEN 15 AND 480 AND cutoff_at <= start_at");
            t.HasCheckConstraint("ck_listings_coordinates", "latitude BETWEEN -90 AND 90 AND longitude BETWEEN -180 AND 180");
            t.HasCheckConstraint("ck_listings_levels", "(level_min = 'BEGINNER') OR (level_min = 'INTERMEDIATE' AND level_max IN ('INTERMEDIATE','ADVANCED')) OR (level_min = 'ADVANCED' AND level_max = 'ADVANCED')");
        });
        listings.HasKey(x => x.Id);
        listings.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        listings.Property(x => x.Mode).HasConversion<string>().HasMaxLength(24);
        listings.Property(x => x.Title).HasMaxLength(160);
        listings.Property(x => x.LevelMin).HasConversion<string>().HasMaxLength(20).HasDefaultValueSql("'BEGINNER'").HasSentinel((SportLevel)(-1));
        listings.Property(x => x.LevelMax).HasConversion<string>().HasMaxLength(20).HasDefaultValueSql("'ADVANCED'").HasSentinel((SportLevel)(-1));
        listings.Property(x => x.AcceptedSlots).HasDefaultValue((short)0);
        listings.Property(x => x.AddressText).HasMaxLength(255);
        listings.Property(x => x.Latitude).HasPrecision(9, 6);
        listings.Property(x => x.Longitude).HasPrecision(9, 6);
        listings.Property<Point>("LocationGeography").HasColumnType("geography (point, 4326)");
        listings.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValueSql("'OPEN'").HasSentinel((ListingStatus)(-1));
        listings.Property(x => x.ConcurrencyToken)
            .HasDefaultValueSql("gen_random_uuid()")
            .IsConcurrencyToken();
        listings.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        listings.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        listings.HasOne(x => x.OwnerUser).WithMany(x => x.OwnedListings).HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        listings.HasOne(x => x.HostTeam).WithMany(x => x.HostedListings).HasForeignKey(x => x.HostTeamId).OnDelete(DeleteBehavior.SetNull);
        listings.HasOne(x => x.Sport).WithMany(x => x.Listings).HasForeignKey(x => x.SportId).OnDelete(DeleteBehavior.Restrict);
        listings.HasOne(x => x.Format).WithMany(x => x.Listings)
            .HasForeignKey(x => new { x.FormatId, x.SportId })
            .HasPrincipalKey(x => new { x.Id, x.SportId })
            .OnDelete(DeleteBehavior.Restrict);
        listings.HasIndex("LocationGeography").HasMethod("gist")
            .HasFilter("status IN ('OPEN','MATCHED') AND deleted_at IS NULL").HasDatabaseName("ix_listings_geo");
        listings.HasIndex(x => new { x.Status, x.SportId, x.StartAt }).HasFilter("deleted_at IS NULL").HasDatabaseName("ix_listings_feed");
        listings.HasIndex(x => new { x.OwnerUserId, x.CreatedAt }).IsDescending(false, true)
            .HasFilter("deleted_at IS NULL").HasDatabaseName("ix_listings_owner");
        listings.HasIndex(x => new { x.Status, x.CutoffAt }).HasFilter("status IN ('OPEN','MATCHED')").HasDatabaseName("ix_listings_expiry");
        listings.HasQueryFilter(x => x.DeletedAt == null && x.OwnerUser.DeletedAt == null);

        var requests = modelBuilder.Entity<ListingRequest>();
        requests.ToTable("listing_requests", t =>
        {
            t.HasCheckConstraint("ck_listing_requests_applicant", "(type='PLAYER' AND applicant_user_id IS NOT NULL AND applicant_team_id IS NULL) OR (type='OPPONENT' AND applicant_team_id IS NOT NULL AND applicant_user_id IS NULL)");
            t.HasCheckConstraint("ck_listing_requests_slots", "requested_slots > 0");
        });
        requests.HasKey(x => x.Id);
        requests.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        requests.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        requests.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValueSql("'PENDING'").HasSentinel((RequestStatus)(-1));
        requests.Property(x => x.RequestedSlots).HasDefaultValue((short)1);
        requests.Property(x => x.ConcurrencyToken)
            .HasDefaultValueSql("gen_random_uuid()")
            .IsConcurrencyToken();
        requests.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        requests.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        requests.HasOne(x => x.Listing).WithMany(x => x.Requests).HasForeignKey(x => x.ListingId).OnDelete(DeleteBehavior.Cascade);
        requests.HasOne(x => x.ApplicantUser).WithMany(x => x.ApplicantRequests).HasForeignKey(x => x.ApplicantUserId).OnDelete(DeleteBehavior.Restrict);
        requests.HasOne(x => x.ApplicantTeam).WithMany(x => x.ApplicantRequests).HasForeignKey(x => x.ApplicantTeamId).OnDelete(DeleteBehavior.Restrict);
        requests.HasOne(x => x.DecidedByUser).WithMany(x => x.DecidedRequests).HasForeignKey(x => x.DecidedByUserId).OnDelete(DeleteBehavior.SetNull);
        requests.HasIndex(x => new { x.ListingId, x.ApplicantUserId }).IsUnique()
            .HasFilter("applicant_user_id IS NOT NULL AND status IN ('PENDING','ACCEPTED')").HasDatabaseName("uq_listing_requests_active_user");
        requests.HasIndex(x => new { x.ListingId, x.ApplicantTeamId }).IsUnique()
            .HasFilter("applicant_team_id IS NOT NULL AND status IN ('PENDING','ACCEPTED')").HasDatabaseName("uq_listing_requests_active_team");
        requests.HasIndex(x => new { x.ListingId, x.Status, x.CreatedAt }).HasDatabaseName("ix_listing_requests_listing_status");
        requests.HasQueryFilter(x => x.Listing.DeletedAt == null && x.Listing.OwnerUser.DeletedAt == null);

        var matches = modelBuilder.Entity<Match>();
        matches.ToTable("matches", t =>
        {
            t.HasCheckConstraint("ck_matches_time", "duration_minutes BETWEEN 15 AND 480");
            t.HasCheckConstraint("ck_matches_coordinates", "latitude BETWEEN -90 AND 90 AND longitude BETWEEN -180 AND 180");
        });
        matches.HasKey(x => x.Id);
        matches.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        matches.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValueSql("'PENDING'").HasSentinel((MatchStatus)(-1));
        matches.Property(x => x.AddressText).HasMaxLength(255);
        matches.Property(x => x.Latitude).HasPrecision(9, 6);
        matches.Property(x => x.Longitude).HasPrecision(9, 6);
        matches.Property<Point>("LocationGeography").HasColumnType("geography (point, 4326)");
        matches.Property(x => x.ConcurrencyToken)
            .HasDefaultValueSql("gen_random_uuid()")
            .IsConcurrencyToken();
        matches.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        matches.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        matches.HasOne(x => x.Listing).WithOne(x => x.Match).HasForeignKey<Match>(x => x.ListingId).OnDelete(DeleteBehavior.Restrict);
        matches.HasOne(x => x.CancelledByUser).WithMany(x => x.CancelledMatches).HasForeignKey(x => x.CancelledByUserId).OnDelete(DeleteBehavior.SetNull);
        matches.HasIndex(x => x.ListingId).IsUnique().HasDatabaseName("uq_matches_listing");
        matches.HasIndex(x => new { x.Status, x.StartAt }).HasFilter("status IN ('PENDING','CONFIRMED')").HasDatabaseName("ix_matches_schedule");
        matches.HasQueryFilter(x => x.Listing.DeletedAt == null && x.Listing.OwnerUser.DeletedAt == null);

        var participants = modelBuilder.Entity<MatchParticipant>();
        participants.ToTable("match_participants", t =>
        {
            t.HasCheckConstraint("ck_match_participants_subject", "(user_id IS NOT NULL)::int + (team_id IS NOT NULL)::int = 1");
            t.HasCheckConstraint("ck_match_participants_side", "side_no IN (1,2)");
        });
        participants.HasKey(x => x.Id);
        participants.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        participants.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        participants.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValueSql("'CONFIRMED'").HasSentinel((ParticipantStatus)(-1));
        participants.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        participants.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        participants.HasOne(x => x.Match).WithMany(x => x.Participants).HasForeignKey(x => x.MatchId).OnDelete(DeleteBehavior.Cascade);
        participants.HasOne(x => x.User).WithMany(x => x.MatchParticipations).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        participants.HasOne(x => x.Team).WithMany(x => x.MatchParticipants).HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Restrict);
        participants.HasOne(x => x.SourceRequest).WithMany(x => x.MatchParticipants).HasForeignKey(x => x.SourceRequestId).OnDelete(DeleteBehavior.SetNull);
        participants.HasIndex(x => new { x.MatchId, x.UserId }).IsUnique().HasFilter("user_id IS NOT NULL").HasDatabaseName("uq_match_participants_user");
        participants.HasIndex(x => new { x.MatchId, x.TeamId }).IsUnique().HasFilter("team_id IS NOT NULL").HasDatabaseName("uq_match_participants_team");
        participants.HasQueryFilter(x => x.Match.Listing.DeletedAt == null && x.Match.Listing.OwnerUser.DeletedAt == null);
    }

    private static void ConfigureNotificationsAndSafety(ModelBuilder modelBuilder)
    {
        var notifications = modelBuilder.Entity<NotificationHistory>();
        notifications.ToTable("notification_history");
        notifications.HasKey(x => x.Id);
        notifications.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        notifications.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
        notifications.Property(x => x.Title).HasMaxLength(160);
        notifications.Property(x => x.EntityType).HasMaxLength(40);
        notifications.Property(x => x.PayloadJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        notifications.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        notifications.HasIndex(x => x.PurgeAfter).HasDatabaseName("ix_notifications_purge_after");
        notifications.HasOne(x => x.User).WithMany(x => x.NotificationHistories).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        notifications.HasIndex(x => new { x.UserId, x.CreatedAt }).IsDescending(false, true)
            .HasFilter("read_at IS NULL").HasDatabaseName("ix_notifications_unread");

        var outbox = modelBuilder.Entity<OutboxMessage>();
        outbox.ToTable("outbox_messages", t => t.HasCheckConstraint("ck_outbox_retry_count", "retry_count >= 0"));
        outbox.HasKey(x => x.Id);
        outbox.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        outbox.Property(x => x.AggregateType).HasMaxLength(60);
        outbox.Property(x => x.EventType).HasMaxLength(100);
        outbox.Property(x => x.PayloadJson).HasColumnType("jsonb");
        outbox.Property(x => x.OccurredAt).HasDefaultValueSql("now()");
        outbox.Property(x => x.AvailableAt).HasDefaultValueSql("now()");
        outbox.Property(x => x.RetryCount).HasDefaultValue(0);
        outbox.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        outbox.HasIndex(x => new { x.AvailableAt, x.OccurredAt }).HasFilter("processed_at IS NULL AND failed_at IS NULL").HasDatabaseName("ix_outbox_pending");
        outbox.HasIndex(x => x.PurgeAfter).HasFilter("processed_at IS NOT NULL").HasDatabaseName("ix_outbox_purge_after");

        var reports = modelBuilder.Entity<Report>();
        reports.ToTable("reports", t => t.HasCheckConstraint("ck_reports_target", "target_type IN ('USER','LISTING','MATCH')"));
        reports.HasKey(x => x.Id);
        reports.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        reports.Property(x => x.TargetType).HasConversion<string>().HasMaxLength(20);
        reports.Property(x => x.Category).HasConversion<string>().HasMaxLength(30);
        reports.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValueSql("'OPEN'").HasSentinel((ReportStatus)(-1));
        reports.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        reports.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        reports.HasOne(x => x.ReporterUser).WithMany(x => x.SubmittedReports).HasForeignKey(x => x.ReporterUserId).OnDelete(DeleteBehavior.Restrict);
        reports.HasOne(x => x.AssignedAdmin).WithMany(x => x.AssignedReports).HasForeignKey(x => x.AssignedAdminId).OnDelete(DeleteBehavior.SetNull);
        reports.HasIndex(x => new { x.Status, x.CreatedAt }).HasFilter("status IN ('OPEN','IN_REVIEW')").HasDatabaseName("ix_reports_queue");
        reports.HasQueryFilter(x => x.ReporterUser.DeletedAt == null);

        var actions = modelBuilder.Entity<AdminAction>();
        actions.ToTable("admin_actions");
        actions.HasKey(x => x.Id);
        actions.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        actions.Property(x => x.ActionType).HasConversion<string>().HasMaxLength(40);
        actions.Property(x => x.TargetType).HasMaxLength(30);
        actions.Property(x => x.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        actions.Property(x => x.BeforeJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb").IsRequired();
        actions.Property(x => x.AfterJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb").IsRequired();
        actions.HasIndex(x => new { x.AdminUserId, x.CreatedAt });
        actions.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        // Keep only the actor ID: a required navigation to filtered User can hide audit history.
        actions.HasOne<User>().WithMany(x => x.AdminActions).HasForeignKey(x => x.AdminUserId).OnDelete(DeleteBehavior.Restrict);
        actions.HasIndex(x => new { x.TargetType, x.TargetId, x.CreatedAt }).IsDescending(false, false, true).HasDatabaseName("ix_admin_actions_target");
    }

    private void ApplyPersistenceConventions()
    {
        var now = (timeProvider ?? TimeProvider.System).GetUtcNow();
        var actorUserId = auditActorContext?.ActorUserId;
        if (actorUserId == Guid.Empty)
            throw new InvalidOperationException("ID người thực hiện audit không được là Guid.Empty.");
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditableEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Property(nameof(IAuditableEntity.CreatedAt)).CurrentValue = now;
                    entry.Property(nameof(IAuditableEntity.CreatedBy)).CurrentValue = actorUserId;
                    entry.Property(nameof(IAuditableEntity.UpdatedAt)).CurrentValue = now;
                    entry.Property(nameof(IAuditableEntity.UpdatedBy)).CurrentValue = actorUserId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    // Creator metadata is immutable even if a tracked entity is marked fully Modified.
                    var createdAt = entry.Property(nameof(IAuditableEntity.CreatedAt));
                    var createdBy = entry.Property(nameof(IAuditableEntity.CreatedBy));
                    createdAt.CurrentValue = createdAt.OriginalValue;
                    createdAt.IsModified = false;
                    createdBy.CurrentValue = createdBy.OriginalValue;
                    createdBy.IsModified = false;
                    entry.Property(nameof(IAuditableEntity.UpdatedAt)).CurrentValue = now;
                    entry.Property(nameof(IAuditableEntity.UpdatedBy)).CurrentValue = actorUserId;
                }
                continue;
            }
            var created = entry.Metadata.FindProperty("CreatedAt");
            var updated = entry.Metadata.FindProperty("UpdatedAt");

            if (entry.State == EntityState.Added && created is not null)
                entry.Property("CreatedAt").CurrentValue = now;

            if ((entry.State == EntityState.Added || entry.State == EntityState.Modified) && updated is not null)
                entry.Property("UpdatedAt").CurrentValue = now;

        }

        RotateConcurrencyTokens();

        SyncGeography(ChangeTracker.Entries<Listing>());
        SyncGeography(ChangeTracker.Entries<Match>());
    }

    private void RotateConcurrencyTokens()
    {
        foreach (var entry in ChangeTracker.Entries<IHasConcurrencyToken>()
                     .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
        {
            entry.Property(nameof(IHasConcurrencyToken.ConcurrencyToken)).CurrentValue = Guid.NewGuid();
        }
    }

    private static void SyncGeography<TEntity>(IEnumerable<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<TEntity>> entries)
        where TEntity : class
    {
        foreach (var entry in entries.Where(entry =>
                     entry.State is EntityState.Added or EntityState.Modified))
        {
            var (latitude, longitude) = entry.Entity switch
            {
                Listing listing => (listing.Latitude, listing.Longitude),
                Match match => (match.Latitude, match.Longitude),
                _ => throw new InvalidOperationException($"Không hỗ trợ thực thể dữ liệu địa lý {typeof(TEntity).Name}.")
            };

            entry.Property<Point>("LocationGeography").CurrentValue = new Point(
                Convert.ToDouble(longitude),
                Convert.ToDouble(latitude))
            {
                SRID = 4326
            };
        }
    }

    private static string ToSnakeCase(string value)
    {
        var result = new StringBuilder(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (char.IsUpper(current) && i > 0)
                result.Append('_');
            result.Append(char.ToLowerInvariant(current));
        }
        return result.ToString();
    }
}
