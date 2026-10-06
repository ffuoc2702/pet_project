using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchingSports.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkUserSportProfileFormats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reject unmapped codes rather than silently dropping existing preferences.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM user_sport_profiles AS profile
                        CROSS JOIN LATERAL unnest(profile.preferred_format_codes) AS preference(code)
                        WHERE NOT EXISTS (
                            SELECT 1 FROM sport_formats AS format
                            WHERE format.sport_id = profile.sport_id AND format.code = preference.code
                        )
                    ) THEN
                        RAISE EXCEPTION 'Cannot migrate preferred formats: codes must match a format of the profile sport.';
                    END IF;
                END $$;
                """);
            migrationBuilder.AddUniqueConstraint(
                name: "ak_user_sport_profiles_id_sport_id",
                table: "user_sport_profiles",
                columns: new[] { "id", "sport_id" });

            migrationBuilder.CreateTable(
                name: "user_sport_profile_formats",
                columns: table => new
                {
                    user_sport_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    format_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sport_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_sport_profile_formats", x => new { x.user_sport_profile_id, x.format_id });
                    table.ForeignKey(
                        name: "FK_user_sport_profile_formats_sport_formats_format_id_sport_id",
                        columns: x => new { x.format_id, x.sport_id },
                        principalTable: "sport_formats",
                        principalColumns: new[] { "id", "sport_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_sport_profile_formats_user_sport_profiles_user_sport_p~",
                        columns: x => new { x.user_sport_profile_id, x.sport_id },
                        principalTable: "user_sport_profiles",
                        principalColumns: new[] { "id", "sport_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_sport_profile_formats_format_id_sport_id",
                table: "user_sport_profile_formats",
                columns: new[] { "format_id", "sport_id" });

            migrationBuilder.CreateIndex(
                name: "IX_user_sport_profile_formats_user_sport_profile_id_sport_id",
                table: "user_sport_profile_formats",
                columns: new[] { "user_sport_profile_id", "sport_id" });

            migrationBuilder.Sql("""
                INSERT INTO user_sport_profile_formats (user_sport_profile_id, format_id, sport_id)
                SELECT DISTINCT profile.id, format.id, profile.sport_id
                FROM user_sport_profiles AS profile
                CROSS JOIN LATERAL unnest(profile.preferred_format_codes) AS preference(code)
                JOIN sport_formats AS format
                    ON format.sport_id = profile.sport_id AND format.code = preference.code;
                """);

            migrationBuilder.DropColumn(
                name: "preferred_format_codes",
                table: "user_sport_profiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "preferred_format_codes",
                table: "user_sport_profiles",
                type: "varchar(32)[]",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE user_sport_profiles AS profile
                SET preferred_format_codes = (
                    SELECT array_agg(format.code ORDER BY format.code)
                    FROM user_sport_profile_formats AS preference
                    JOIN sport_formats AS format ON format.id = preference.format_id
                    WHERE preference.user_sport_profile_id = profile.id
                );
                """);

            migrationBuilder.DropTable(
                name: "user_sport_profile_formats");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_user_sport_profiles_id_sport_id",
                table: "user_sport_profiles");
        }
    }
}
