using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchingSports.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "match_reminder_lead_minutes",
                table: "system_settings",
                type: "integer",
                nullable: false,
                defaultValue: 60);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "reminder_published_at",
                table: "matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_system_settings_match_reminder",
                table: "system_settings",
                sql: "match_reminder_lead_minutes BETWEEN 1 AND 10080");

            migrationBuilder.CreateIndex(
                name: "ix_matches_due_reminder",
                table: "matches",
                columns: new[] { "status", "start_at" },
                filter: "status = 'CONFIRMED' AND reminder_published_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_system_settings_match_reminder",
                table: "system_settings");

            migrationBuilder.DropIndex(
                name: "ix_matches_due_reminder",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "match_reminder_lead_minutes",
                table: "system_settings");

            migrationBuilder.DropColumn(
                name: "reminder_published_at",
                table: "matches");
        }
    }
}
