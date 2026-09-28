using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchingSports.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenDataIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_listings_sport_formats_format_id",
                table: "listings");

            migrationBuilder.DropIndex(
                name: "IX_listings_format_id",
                table: "listings");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_sport_formats_id_sport_id",
                table: "sport_formats",
                columns: new[] { "id", "sport_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_matches_coordinates",
                table: "matches",
                sql: "latitude BETWEEN -90 AND 90 AND longitude BETWEEN -180 AND 180");

            migrationBuilder.AddCheckConstraint(
                name: "ck_matches_time",
                table: "matches",
                sql: "duration_minutes BETWEEN 15 AND 480");

            migrationBuilder.CreateIndex(
                name: "IX_listings_format_id_sport_id",
                table: "listings",
                columns: new[] { "format_id", "sport_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_listings_levels",
                table: "listings",
                sql: "(level_min = 'BEGINNER') OR (level_min = 'INTERMEDIATE' AND level_max IN ('INTERMEDIATE','ADVANCED')) OR (level_min = 'ADVANCED' AND level_max = 'ADVANCED')");

            migrationBuilder.AddForeignKey(
                name: "FK_listings_sport_formats_format_id_sport_id",
                table: "listings",
                columns: new[] { "format_id", "sport_id" },
                principalTable: "sport_formats",
                principalColumns: new[] { "id", "sport_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_listings_sport_formats_format_id_sport_id",
                table: "listings");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_sport_formats_id_sport_id",
                table: "sport_formats");

            migrationBuilder.DropCheckConstraint(
                name: "ck_matches_coordinates",
                table: "matches");

            migrationBuilder.DropCheckConstraint(
                name: "ck_matches_time",
                table: "matches");

            migrationBuilder.DropIndex(
                name: "IX_listings_format_id_sport_id",
                table: "listings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_listings_levels",
                table: "listings");

            migrationBuilder.CreateIndex(
                name: "IX_listings_format_id",
                table: "listings",
                column: "format_id");

            migrationBuilder.AddForeignKey(
                name: "FK_listings_sport_formats_format_id",
                table: "listings",
                column: "format_id",
                principalTable: "sport_formats",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
