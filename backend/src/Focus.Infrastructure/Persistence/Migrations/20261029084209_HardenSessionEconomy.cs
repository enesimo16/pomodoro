using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Focus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenSessionEconomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_inventory_items_UserId_CatalogItemId",
                table: "user_inventory_items");

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyStamp",
                table: "users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyStamp",
                table: "user_streaks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyStamp",
                table: "focus_sessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Benzersiz indeksler olusturulmadan once yarisla olusmus kopya kayitlar temizlenir
            migrationBuilder.Sql(
                """
                DELETE FROM user_inventory_items a
                USING user_inventory_items b
                WHERE a."UserId" = b."UserId"
                  AND a."CatalogItemId" = b."CatalogItemId"
                  AND (a."AcquiredAt" > b."AcquiredAt" OR (a."AcquiredAt" = b."AcquiredAt" AND a."Id" > b."Id"));
                """);

            migrationBuilder.Sql(
                """
                UPDATE focus_sessions
                SET "Status" = 'Abandoned', "EndedAt" = COALESCE("EndedAt", NOW())
                WHERE "Status" IN ('Running', 'Paused')
                  AND "Id" NOT IN (
                    SELECT DISTINCT ON ("UserId") "Id"
                    FROM focus_sessions
                    WHERE "Status" IN ('Running', 'Paused')
                    ORDER BY "UserId", "StartedAt" DESC);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_user_inventory_items_UserId_CatalogItemId",
                table: "user_inventory_items",
                columns: new[] { "UserId", "CatalogItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_focus_sessions_UserId_Active",
                table: "focus_sessions",
                column: "UserId",
                unique: true,
                filter: "\"Status\" IN ('Running', 'Paused')");

            migrationBuilder.CreateIndex(
                name: "IX_coin_ledger_entries_UserId_SessionId_Reason",
                table: "coin_ledger_entries",
                columns: new[] { "UserId", "SessionId", "Reason" },
                unique: true,
                filter: "\"SessionId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_inventory_items_UserId_CatalogItemId",
                table: "user_inventory_items");

            migrationBuilder.DropIndex(
                name: "IX_focus_sessions_UserId_Active",
                table: "focus_sessions");

            migrationBuilder.DropIndex(
                name: "IX_coin_ledger_entries_UserId_SessionId_Reason",
                table: "coin_ledger_entries");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "user_streaks");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "focus_sessions");

            migrationBuilder.CreateIndex(
                name: "IX_user_inventory_items_UserId_CatalogItemId",
                table: "user_inventory_items",
                columns: new[] { "UserId", "CatalogItemId" });
        }
    }
}
