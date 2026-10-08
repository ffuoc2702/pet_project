using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchingSports.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationConfigurationAndHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep the old rows until the new schema and data transfer both succeed.
            migrationBuilder.RenameTable(name: "notifications", newName: "notification_history_legacy");
            migrationBuilder.RenameIndex(name: "ix_notifications_purge_after", table: "notification_history_legacy", newName: "ix_notification_legacy_purge");
            migrationBuilder.RenameIndex(name: "ix_notifications_unread", table: "notification_history_legacy", newName: "ix_notification_legacy_unread");

            migrationBuilder.CreateTable(
                name: "notification_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_send_now = table.Column<bool>(type: "boolean", nullable: false),
                    schedule_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cron_string = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    critical = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    body = table.Column<string>(type: "character varying(100000)", maxLength: 100000, nullable: false),
                    body_text = table.Column<string>(type: "text", nullable: false),
                    available_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expired_after = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    max_send_times = table.Column<int>(type: "integer", nullable: true),
                    audience_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    in_app_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    email_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    push_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    sms_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    time_zone_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    scheduling_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    next_run_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_run_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    send_count = table.Column<int>(type: "integer", nullable: false),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lease_token = table.Column<Guid>(type: "uuid", nullable: true),
                    abort_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_configs", x => x.id);
                    table.CheckConstraint("ck_notification_configs_channels", "in_app_enabled OR email_enabled OR push_enabled OR sms_enabled");
                    table.CheckConstraint("ck_notification_configs_counts", "send_count >= 0 AND (max_send_times IS NULL OR max_send_times > 0)");
                    table.CheckConstraint("ck_notification_configs_lease", "(locked_until IS NULL) = (lease_token IS NULL)");
                    table.CheckConstraint("ck_notification_configs_schedule", "(schedule_type = 'ONCE' AND cron_string IS NULL AND max_send_times = 1) OR (schedule_type = 'SCHEDULED' AND cron_string IS NOT NULL)");
                    table.CheckConstraint("ck_notification_configs_window", "expired_after IS NULL OR expired_after > available_at");
                });

            migrationBuilder.CreateTable(
                name: "notification_config_teams",
                columns: table => new
                {
                    notification_config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_config_teams", x => new { x.notification_config_id, x.team_id });
                    table.ForeignKey(
                        name: "FK_notification_config_teams_notification_configs_notification~",
                        column: x => x.notification_config_id,
                        principalTable: "notification_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notification_config_teams_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_config_users",
                columns: table => new
                {
                    notification_config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_config_users", x => new { x.notification_config_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_notification_config_users_notification_configs_notification~",
                        column: x => x.notification_config_id,
                        principalTable: "notification_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notification_config_users_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notification_config_id = table.Column<Guid>(type: "uuid", nullable: true),
                    team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    send_number = table.Column<int>(type: "integer", nullable: false),
                    scheduled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    critical = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "INFO"),
                    body_text = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    in_app_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    email_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "DISABLED"),
                    push_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "DISABLED"),
                    sms_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "DISABLED"),
                    email_sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    push_sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sms_sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    email_error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    push_error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sms_error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    purge_after = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_history_notification_configs_notification_conf~",
                        column: x => x.notification_config_id,
                        principalTable: "notification_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notification_history_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_notification_history_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_config_teams_team_id",
                table: "notification_config_teams",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_config_users_user_id",
                table: "notification_config_users",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_configs_due",
                table: "notification_configs",
                columns: new[] { "next_run_at", "id" },
                filter: "status = 'ACTIVE' AND scheduling_status IN ('PENDING', 'SCHEDULING')");

            migrationBuilder.CreateIndex(
                name: "ix_notification_history_config",
                table: "notification_history",
                columns: new[] { "notification_config_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_history_team_id",
                table: "notification_history",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_purge_after",
                table: "notification_history",
                column: "purge_after");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_unread",
                table: "notification_history",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true },
                filter: "read_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_notification_history_recipient",
                table: "notification_history",
                columns: new[] { "notification_config_id", "send_number", "user_id" },
                unique: true,
                filter: "notification_config_id IS NOT NULL AND user_id IS NOT NULL");
            migrationBuilder.Sql("""
                INSERT INTO notification_history
                    (id, user_id, type, title, body, body_text, entity_type, entity_id, payload_json,
                     read_at, purge_after, created_at, updated_at, created_by, updated_by, send_number, scheduled_at)
                SELECT id, user_id, type, title,
                    replace(replace(replace(body, '&', '&amp;'), '<', '&lt;'), '>', '&gt;'),
                    body, entity_type, entity_id,
                    CASE WHEN jsonb_typeof(payload_json) = 'object'
                         THEN payload_json || '{"SchemaVersion":1}'::jsonb
                         ELSE jsonb_build_object('SchemaVersion', 1, 'LegacyPayload', payload_json) END,
                    read_at, purge_after, created_at, updated_at, created_by, updated_by, 0, created_at
                FROM notification_history_legacy;
                DROP TABLE notification_history_legacy;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only standalone histories fit the previous schema; configured delivery data is a new feature.
            migrationBuilder.Sql("""
                CREATE TABLE notification_history_legacy AS
                SELECT id, user_id, type, title, body_text AS body, entity_type, entity_id, payload_json,
                       read_at, purge_after, created_at, updated_at, created_by, updated_by
                FROM notification_history WHERE notification_config_id IS NULL AND user_id IS NOT NULL;
                """);
            migrationBuilder.DropTable(
                name: "notification_config_teams");

            migrationBuilder.DropTable(
                name: "notification_config_users");

            migrationBuilder.DropTable(
                name: "notification_history");

            migrationBuilder.DropTable(
                name: "notification_configs");

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    purge_after = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_purge_after",
                table: "notifications",
                column: "purge_after");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_unread",
                table: "notifications",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true },
                filter: "read_at IS NULL");
            migrationBuilder.Sql("""
                INSERT INTO notifications
                    (id, user_id, type, title, body, entity_type, entity_id, payload_json,
                     read_at, purge_after, created_at, updated_at, created_by, updated_by)
                SELECT id, user_id, type, title, body, entity_type, entity_id, payload_json,
                       read_at, purge_after, created_at, updated_at, created_by, updated_by
                FROM notification_history_legacy;
                DROP TABLE notification_history_legacy;
                """);
        }
    }
}
