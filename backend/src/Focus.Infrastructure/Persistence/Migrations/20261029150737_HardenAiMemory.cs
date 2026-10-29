using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Focus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenAiMemory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Vector>(
                name: "Embedding",
                table: "AgentMemories",
                type: "vector(768)",
                nullable: true,
                oldClrType: typeof(Vector),
                oldType: "vector(768)");

            // Onceki surumlerde uretilen hash tabanli sahte vektorleri temizler; acilista gercek vektorlerle yeniden doldurulur
            migrationBuilder.Sql("UPDATE \"AgentMemories\" SET \"Embedding\" = NULL;");

            migrationBuilder.AddColumn<Guid>(
                name: "SourceSessionId",
                table: "AgentMemories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AiTokenUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsageDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TokensUsed = table.Column<int>(type: "integer", nullable: false),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiTokenUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiTokenUsages_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentMemories_SourceSessionId",
                table: "AgentMemories",
                column: "SourceSessionId",
                unique: true,
                filter: "\"SourceSessionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AiTokenUsages_UserId_UsageDate",
                table: "AiTokenUsages",
                columns: new[] { "UserId", "UsageDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiTokenUsages");

            migrationBuilder.DropIndex(
                name: "IX_AgentMemories_SourceSessionId",
                table: "AgentMemories");

            migrationBuilder.DropColumn(
                name: "SourceSessionId",
                table: "AgentMemories");

            migrationBuilder.AlterColumn<Vector>(
                name: "Embedding",
                table: "AgentMemories",
                type: "vector(768)",
                nullable: false,
                oldClrType: typeof(Vector),
                oldType: "vector(768)",
                oldNullable: true);
        }
    }
}
