using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Focus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFocusSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TargetRounds",
                table: "user_preferences",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "focus_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FocusMinutes = table.Column<int>(type: "integer", nullable: false),
                    BreakMinutes = table.Column<int>(type: "integer", nullable: false),
                    PlannedMinutes = table.Column<int>(type: "integer", nullable: false),
                    CurrentRound = table.Column<int>(type: "integer", nullable: false),
                    TargetRounds = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlannedEndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PausedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalPausedSeconds = table.Column<int>(type: "integer", nullable: false),
                    NetDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    ExtensionCount = table.Column<int>(type: "integer", nullable: false),
                    XpEarned = table.Column<int>(type: "integer", nullable: false),
                    CoinsEarned = table.Column<int>(type: "integer", nullable: false),
                    ThemeId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MixPresetId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_focus_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_focus_sessions_pixel_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "pixel_rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_focus_sessions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_focus_sessions_RoomId",
                table: "focus_sessions",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_focus_sessions_StartedAt",
                table: "focus_sessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_focus_sessions_Status",
                table: "focus_sessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_focus_sessions_UserId",
                table: "focus_sessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "focus_sessions");

            migrationBuilder.DropColumn(
                name: "TargetRounds",
                table: "user_preferences");
        }
    }
}
