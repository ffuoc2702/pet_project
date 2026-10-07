using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchingSports.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityAuditMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "user_sport_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "user_sport_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "user_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "user_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "teams",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "teams",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "team_members",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "team_members",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "system_settings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "system_settings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "sports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "sports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "sport_formats",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "sport_formats",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                table: "roles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "roles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "permissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "permissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "outbox_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "outbox_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "otp_challenges",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "otp_challenges",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "otp_challenges",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "match_participants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "match_participants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "listings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "listings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "listing_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "listing_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "auth_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "auth_sessions",
                type: "uuid",
                nullable: true);

            // No historical updater is known. Preserve the known creation time for these new timestamps.
            migrationBuilder.Sql("""
                UPDATE notifications SET updated_at = created_at;
                UPDATE outbox_messages SET updated_at = created_at;
                UPDATE otp_challenges SET updated_at = created_at;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_by",
                table: "users");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "users");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "user_sport_profiles");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "user_sport_profiles");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "team_members");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "team_members");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "system_settings");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "system_settings");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "sports");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "sports");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "sport_formats");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "sport_formats");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "permissions");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "permissions");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "otp_challenges");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "otp_challenges");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "otp_challenges");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "match_participants");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "match_participants");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "listing_requests");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "listing_requests");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "auth_sessions");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "auth_sessions");
        }
    }
}
