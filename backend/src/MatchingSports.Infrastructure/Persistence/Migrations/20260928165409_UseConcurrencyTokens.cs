using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchingSports.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UseConcurrencyTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "version",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "version",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "version",
                table: "listing_requests");

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_token",
                table: "matches",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_token",
                table: "listings",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_token",
                table: "listing_requests",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "concurrency_token",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "concurrency_token",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "concurrency_token",
                table: "listing_requests");

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "listings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "listing_requests",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
