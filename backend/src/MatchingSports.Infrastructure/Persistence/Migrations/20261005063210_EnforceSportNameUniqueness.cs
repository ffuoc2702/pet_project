using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchingSports.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSportNameUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_sports_code",
                table: "sports");

            migrationBuilder.AddColumn<string>(
                name: "normalized_name",
                table: "sports",
                type: "text",
                nullable: true,
                computedColumnSql: "lower(btrim(name))",
                stored: true);

            migrationBuilder.AddColumn<string>(
                name: "normalized_name",
                table: "sport_formats",
                type: "text",
                nullable: true,
                computedColumnSql: "lower(btrim(display_name))",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "uq_sports_code",
                table: "sports",
                column: "code",
                unique: true,
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "uq_sports_name",
                table: "sports",
                column: "normalized_name",
                unique: true,
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "uq_sport_formats_sport_name",
                table: "sport_formats",
                columns: new[] { "sport_id", "normalized_name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_sports_code",
                table: "sports");

            migrationBuilder.DropIndex(
                name: "uq_sports_name",
                table: "sports");

            migrationBuilder.DropIndex(
                name: "uq_sport_formats_sport_name",
                table: "sport_formats");

            migrationBuilder.DropColumn(
                name: "normalized_name",
                table: "sports");

            migrationBuilder.DropColumn(
                name: "normalized_name",
                table: "sport_formats");

            migrationBuilder.CreateIndex(
                name: "uq_sports_code",
                table: "sports",
                column: "code",
                unique: true);
        }
    }
}
