using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchingSports.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MergeAuthorizationAuditIntoAdminActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_admin_actions_admin_user_id",
                table: "admin_actions");

            migrationBuilder.AlterColumn<string>(
                name: "reason",
                table: "admin_actions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "after_json",
                table: "admin_actions",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "before_json",
                table: "admin_actions",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.CreateIndex(
                name: "IX_admin_actions_admin_user_id_created_at",
                table: "admin_actions",
                columns: new[] { "admin_user_id", "created_at" });

            // Fail transactionally for unknown actions instead of dropping audit history.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM authorization_audit_entries
                        WHERE action NOT IN ('roles.create', 'roles.update', 'roles.permissions.update', 'roles.assign')) THEN
                        RAISE EXCEPTION 'Cannot merge authorization audit: unsupported action.';
                    END IF;
                END $$;

                INSERT INTO admin_actions
                    (id, admin_user_id, action_type, target_type, target_id, reason,
                     metadata_json, before_json, after_json, created_at)
                SELECT id, actor_user_id,
                    CASE action
                        WHEN 'roles.create' THEN 'CREATE_ROLE'
                        WHEN 'roles.update' THEN 'UPDATE_ROLE'
                        WHEN 'roles.permissions.update' THEN 'UPDATE_ROLE_PERMISSIONS'
                        WHEN 'roles.assign' THEN 'ASSIGN_USER_ROLES'
                    END,
                    CASE WHEN action = 'roles.assign' THEN 'USER' ELSE 'ROLE' END,
                    target_id, NULL, '{}'::jsonb, before_json, after_json, created_at
                FROM authorization_audit_entries;
                """);

            migrationBuilder.DropTable(name: "authorization_audit_entries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_admin_actions_admin_user_id_created_at",
                table: "admin_actions");

            migrationBuilder.CreateTable(
                name: "authorization_audit_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    after_json = table.Column<string>(type: "jsonb", nullable: false),
                    before_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authorization_audit_entries", x => x.id);
                });


            migrationBuilder.Sql("""
                INSERT INTO authorization_audit_entries
                    (id, actor_user_id, action, target_id, before_json, after_json, created_at)
                SELECT id, admin_user_id,
                    CASE action_type
                        WHEN 'CREATE_ROLE' THEN 'roles.create'
                        WHEN 'UPDATE_ROLE' THEN 'roles.update'
                        WHEN 'UPDATE_ROLE_PERMISSIONS' THEN 'roles.permissions.update'
                        WHEN 'ASSIGN_USER_ROLES' THEN 'roles.assign'
                    END,
                    target_id, before_json, after_json, created_at
                FROM admin_actions
                WHERE action_type IN ('CREATE_ROLE', 'UPDATE_ROLE', 'UPDATE_ROLE_PERMISSIONS', 'ASSIGN_USER_ROLES');

                DELETE FROM admin_actions
                WHERE action_type IN ('CREATE_ROLE', 'UPDATE_ROLE', 'UPDATE_ROLE_PERMISSIONS', 'ASSIGN_USER_ROLES');
                """);

            migrationBuilder.DropColumn(
                name: "after_json",
                table: "admin_actions");

            migrationBuilder.DropColumn(
                name: "before_json",
                table: "admin_actions");

            migrationBuilder.AlterColumn<string>(
                name: "reason",
                table: "admin_actions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_admin_actions_admin_user_id",
                table: "admin_actions",
                column: "admin_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_authorization_audit_entries_actor_user_id_created_at",
                table: "authorization_audit_entries",
                columns: new[] { "actor_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_authorization_audit_entries_target_id_created_at",
                table: "authorization_audit_entries",
                columns: new[] { "target_id", "created_at" });
        }
    }
}
