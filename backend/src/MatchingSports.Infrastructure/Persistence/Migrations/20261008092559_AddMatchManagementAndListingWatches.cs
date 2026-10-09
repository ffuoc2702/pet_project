using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchingSports.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchManagementAndListingWatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Older direct SQL inserts may not have initialized the searchable geography column.
            migrationBuilder.Sql("UPDATE listings SET location_geography = ST_SetSRID(ST_MakePoint(longitude::double precision, latitude::double precision), 4326)::geography WHERE location_geography IS NULL");
            migrationBuilder.Sql("UPDATE matches SET location_geography = ST_SetSRID(ST_MakePoint(longitude::double precision, latitude::double precision), 4326)::geography WHERE location_geography IS NULL");
            migrationBuilder.AddColumn<int>(
                name: "max_active_listing_watches_per_user",
                table: "system_settings",
                type: "integer",
                nullable: false,
                defaultValue: 20);

            migrationBuilder.AddColumn<int>(
                name: "max_listing_search_radius_meters",
                table: "system_settings",
                type: "integer",
                nullable: false,
                defaultValue: 50000);

            migrationBuilder.AddColumn<int>(
                name: "max_listing_watch_window_days",
                table: "system_settings",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.CreateTable(
                name: "listing_watches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sport_id = table.Column<Guid>(type: "uuid", nullable: false),
                    format_id = table.Column<Guid>(type: "uuid", nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    radius_meters = table.Column<int>(type: "integer", nullable: false),
                    start_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    start_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listing_watches", x => x.id);
                    table.CheckConstraint("ck_listing_watches_coordinates", "latitude BETWEEN -90 AND 90 AND longitude BETWEEN -180 AND 180");
                    table.CheckConstraint("ck_listing_watches_criteria", "radius_meters BETWEEN 1 AND 200000 AND start_from < start_to");
                    table.ForeignKey(
                        name: "FK_listing_watches_sport_formats_format_id_sport_id",
                        columns: x => new { x.format_id, x.sport_id },
                        principalTable: "sport_formats",
                        principalColumns: new[] { "id", "sport_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_listing_watches_sports_sport_id",
                        column: x => x.sport_id,
                        principalTable: "sports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_listing_watches_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "listing_watch_deliveries",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    watch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_history_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listing_watch_deliveries", x => new { x.user_id, x.listing_id });
                    table.ForeignKey(
                        name: "FK_listing_watch_deliveries_listing_watches_watch_id",
                        column: x => x.watch_id,
                        principalTable: "listing_watches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_listing_watch_deliveries_listings_listing_id",
                        column: x => x.listing_id,
                        principalTable: "listings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_listing_watch_deliveries_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_system_settings_listing_watches",
                table: "system_settings",
                sql: "max_active_listing_watches_per_user BETWEEN 0 AND 1000 AND max_listing_search_radius_meters BETWEEN 1 AND 200000 AND max_listing_watch_window_days BETWEEN 1 AND 365");

            migrationBuilder.CreateIndex(
                name: "IX_listing_watch_deliveries_listing_id",
                table: "listing_watch_deliveries",
                column: "listing_id");

            migrationBuilder.CreateIndex(
                name: "IX_listing_watch_deliveries_watch_id",
                table: "listing_watch_deliveries",
                column: "watch_id");

            migrationBuilder.CreateIndex(
                name: "IX_listing_watches_format_id_sport_id",
                table: "listing_watches",
                columns: new[] { "format_id", "sport_id" });

            migrationBuilder.CreateIndex(
                name: "IX_listing_watches_sport_id_status_start_from_start_to",
                table: "listing_watches",
                columns: new[] { "sport_id", "status", "start_from", "start_to" });

            migrationBuilder.CreateIndex(
                name: "IX_listing_watches_status_start_to",
                table: "listing_watches",
                columns: new[] { "status", "start_to" });

            migrationBuilder.CreateIndex(
                name: "IX_listing_watches_user_id_status_start_to",
                table: "listing_watches",
                columns: new[] { "user_id", "status", "start_to" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "listing_watch_deliveries");

            migrationBuilder.DropTable(
                name: "listing_watches");

            migrationBuilder.DropCheckConstraint(
                name: "ck_system_settings_listing_watches",
                table: "system_settings");

            migrationBuilder.DropColumn(
                name: "max_active_listing_watches_per_user",
                table: "system_settings");

            migrationBuilder.DropColumn(
                name: "max_listing_search_radius_meters",
                table: "system_settings");

            migrationBuilder.DropColumn(
                name: "max_listing_watch_window_days",
                table: "system_settings");
        }
    }
}
