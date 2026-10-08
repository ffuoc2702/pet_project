using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchingSports.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing teams may have stored the captain only on teams. Repair membership without inventing an audit actor.
            migrationBuilder.Sql("""
                INSERT INTO team_members (team_id, user_id, role, status, joined_at, created_at, updated_at)
                SELECT id, captain_user_id, 'CAPTAIN', 'ACTIVE', now(), now(), now() FROM teams
                WHERE deleted_at IS NULL
                ON CONFLICT (team_id, user_id) DO UPDATE SET role = 'CAPTAIN', status = 'ACTIVE', left_at = NULL,
                    joined_at = CASE WHEN team_members.status <> 'ACTIVE' THEN now() ELSE team_members.joined_at END,
                    updated_at = now(), updated_by = NULL
                WHERE team_members.role <> 'CAPTAIN' OR team_members.status <> 'ACTIVE' OR team_members.left_at IS NOT NULL;
                UPDATE team_members m SET role = 'MEMBER', updated_at = now(), updated_by = NULL
                FROM teams t WHERE m.team_id = t.id AND m.user_id <> t.captain_user_id AND m.role = 'CAPTAIN';
                """);
            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_token",
                table: "teams",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.CreateTable(
                name: "team_invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inviter_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    responded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_invitations", x => x.id);
                    table.ForeignKey(
                        name: "FK_team_invitations_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_team_invitations_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "team_join_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    responded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_join_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_team_join_requests_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_team_join_requests_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_team_invitations_team_id_user_id_status",
                table: "team_invitations",
                columns: new[] { "team_id", "user_id", "status" },
                unique: true,
                filter: "status = 'PENDING'");

            migrationBuilder.CreateIndex(
                name: "IX_team_invitations_user_id_status",
                table: "team_invitations",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_team_join_requests_team_id_user_id_status",
                table: "team_join_requests",
                columns: new[] { "team_id", "user_id", "status" },
                unique: true,
                filter: "status = 'PENDING'");

            migrationBuilder.CreateIndex(
                name: "IX_team_join_requests_user_id_status",
                table: "team_join_requests",
                columns: new[] { "user_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "team_invitations");

            migrationBuilder.DropTable(
                name: "team_join_requests");

            migrationBuilder.DropColumn(
                name: "concurrency_token",
                table: "teams");
        }
    }
}
