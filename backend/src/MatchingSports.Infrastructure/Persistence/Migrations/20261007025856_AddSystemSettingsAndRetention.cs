using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchingSports.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemSettingsAndRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_outbox_pending",
                table: "outbox_messages");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "failed_at",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "purge_after",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "purge_after",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "system_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outbox_max_retry_count = table.Column<int>(type: "integer", nullable: false),
                    max_captain_teams_per_user = table.Column<int>(type: "integer", nullable: false),
                    max_member_teams_per_user = table.Column<int>(type: "integer", nullable: false),
                    max_deputies_per_team = table.Column<int>(type: "integer", nullable: false),
                    max_members_per_team = table.Column<int>(type: "integer", nullable: false),
                    team_invitation_lifetime_hours = table.Column<int>(type: "integer", nullable: false),
                    max_pending_team_requests = table.Column<int>(type: "integer", nullable: false),
                    outbox_retry_initial_delay_seconds = table.Column<int>(type: "integer", nullable: false),
                    outbox_retry_max_delay_seconds = table.Column<int>(type: "integer", nullable: false),
                    processed_outbox_retention_days = table.Column<int>(type: "integer", nullable: false),
                    notification_retention_days = table.Column<int>(type: "integer", nullable: false),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_settings", x => x.id);
                    table.CheckConstraint("ck_system_settings_limits", "outbox_max_retry_count BETWEEN 0 AND 100 AND max_captain_teams_per_user BETWEEN 0 AND 10000 AND max_member_teams_per_user BETWEEN 0 AND 10000 AND max_members_per_team BETWEEN 1 AND 10000 AND max_deputies_per_team >= 0 AND max_deputies_per_team < max_members_per_team AND team_invitation_lifetime_hours BETWEEN 1 AND 8760 AND max_pending_team_requests BETWEEN 0 AND 10000 AND outbox_retry_initial_delay_seconds BETWEEN 1 AND 604800 AND outbox_retry_max_delay_seconds BETWEEN outbox_retry_initial_delay_seconds AND 604800 AND processed_outbox_retention_days BETWEEN 1 AND 36500 AND notification_retention_days BETWEEN 1 AND 36500");
                    table.CheckConstraint("ck_system_settings_singleton", "id = '00000000-0000-0000-0002-000000000001'::uuid");
                });

            // Seed once in the migration; future deployments must preserve administrator edits.
            migrationBuilder.Sql("""
                INSERT INTO system_settings (
                    id, outbox_max_retry_count, max_captain_teams_per_user, max_member_teams_per_user,
                    max_deputies_per_team, max_members_per_team, team_invitation_lifetime_hours,
                    max_pending_team_requests, outbox_retry_initial_delay_seconds, outbox_retry_max_delay_seconds,
                    processed_outbox_retention_days, notification_retention_days)
                VALUES ('00000000-0000-0000-0002-000000000001', 5, 3, 10, 2, 50, 72, 100, 5, 3600, 30, 90);

                UPDATE outbox_messages SET purge_after =
                    ((created_at AT TIME ZONE 'UTC') + INTERVAL '1 day' *
                        (SELECT processed_outbox_retention_days FROM system_settings)) AT TIME ZONE 'UTC';
                UPDATE notifications SET purge_after =
                    ((created_at AT TIME ZONE 'UTC') + INTERVAL '1 day' *
                        (SELECT notification_retention_days FROM system_settings)) AT TIME ZONE 'UTC';
                """);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "purge_after", table: "outbox_messages", type: "timestamp with time zone", nullable: false,
                oldClrType: typeof(DateTimeOffset), oldType: "timestamp with time zone", oldNullable: true);
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "purge_after", table: "notifications", type: "timestamp with time zone", nullable: false,
                oldClrType: typeof(DateTimeOffset), oldType: "timestamp with time zone", oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_pending",
                table: "outbox_messages",
                columns: new[] { "available_at", "occurred_at" },
                filter: "processed_at IS NULL AND failed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_purge_after",
                table: "outbox_messages",
                column: "purge_after",
                filter: "processed_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_purge_after",
                table: "notifications",
                column: "purge_after");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "system_settings");

            migrationBuilder.DropIndex(
                name: "ix_outbox_pending",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "ix_outbox_purge_after",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "ix_notifications_purge_after",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "failed_at",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "purge_after",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "purge_after",
                table: "notifications");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_pending",
                table: "outbox_messages",
                columns: new[] { "available_at", "occurred_at" },
                filter: "processed_at IS NULL");
        }
    }
}
