using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Focus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenSocialRooms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_study_rooms_OwnerUserId",
                table: "study_rooms");

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyStamp",
                table: "study_rooms",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("527706df-6f43-4bb8-8637-96adc5a342ec"),
                column: "ConcurrencyStamp",
                value: new Guid("527706df-6f43-4bb8-8637-96adc5a342ec"));

            migrationBuilder.UpdateData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("5ec20c31-815c-47b6-893c-92e3b1f4dc2a"),
                column: "ConcurrencyStamp",
                value: new Guid("5ec20c31-815c-47b6-893c-92e3b1f4dc2a"));

            migrationBuilder.UpdateData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("c4c20115-3d9e-419d-8275-19826fb544d1"),
                column: "ConcurrencyStamp",
                value: new Guid("c4c20115-3d9e-419d-8275-19826fb544d1"));

            migrationBuilder.UpdateData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("eff56794-b4ab-4664-9284-94c90a20cd13"),
                column: "ConcurrencyStamp",
                value: new Guid("eff56794-b4ab-4664-9284-94c90a20cd13"));

            migrationBuilder.CreateIndex(
                name: "IX_study_rooms_OwnerUserId_PrivateHome",
                table: "study_rooms",
                column: "OwnerUserId",
                unique: true,
                filter: "\"Type\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_room_members_RoomId_SeatIndex",
                table: "room_members",
                columns: new[] { "RoomId", "SeatIndex" },
                unique: true,
                filter: "\"SeatIndex\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_study_rooms_OwnerUserId_PrivateHome",
                table: "study_rooms");

            migrationBuilder.DropIndex(
                name: "IX_room_members_RoomId_SeatIndex",
                table: "room_members");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "study_rooms");

            migrationBuilder.CreateIndex(
                name: "IX_study_rooms_OwnerUserId",
                table: "study_rooms",
                column: "OwnerUserId");
        }
    }
}
