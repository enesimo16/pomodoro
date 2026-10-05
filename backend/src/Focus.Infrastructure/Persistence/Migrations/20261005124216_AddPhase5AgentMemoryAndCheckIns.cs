using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Focus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase5AgentMemoryAndCheckIns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("835bd9c9-ee1e-4277-ba1a-840eb7d48f5d"));

            migrationBuilder.DeleteData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("9a500a84-1f01-4e98-83ed-5480803a3d9a"));

            migrationBuilder.DeleteData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("9fd4da2f-3a05-409d-8e72-d9b638a1f7ba"));

            migrationBuilder.DeleteData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("daf737c7-8d50-42ac-a8bd-9582a9762139"));

            migrationBuilder.CreateTable(
                name: "AgentMemories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(768)", nullable: false),
                    Importance = table.Column<int>(type: "integer", nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentMemories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentMemories_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionCheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mood = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EnergyLevel = table.Column<int>(type: "integer", nullable: false),
                    TargetIntent = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionCheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionCheckIns_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionReflections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FocusQuality = table.Column<int>(type: "integer", nullable: false),
                    MoodAfter = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DistractionNote = table.Column<string>(type: "character varying(350)", maxLength: 350, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionReflections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionReflections_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "beanie_orange",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6127));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "bookshelf_wood",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6115));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "boots_leather_brown",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6125));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "cap_black",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6127));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "chair_ergonomic_black",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6107));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "chair_gaming_rgb",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6109));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "chair_vintage_leather",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6108));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "coffee_machine",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6114));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_cyber_neon",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6106));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_gold_master",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6106));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_minimal_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6104));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_retro_oak",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(5928));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "fireplace_cozy",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6116));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "floor_marble_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6136));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "floor_parquet_oak",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6133));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "floor_tatami_japanese",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6135));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "glasses_retro_round",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6128));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "headphones_pro",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6129));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "hoodie_cyber_neon",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6121));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "hoodie_gray",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6118));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jacket_leather_vintage",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6122));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jeans_basic_blue",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6122));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jeans_dark",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6123));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jogger_black",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6124));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "lamp_desk_brass",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6110));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "neon_sign_focus",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6113));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "plant_bonsai",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6111));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "plant_monstera",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6112));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "record_player_vintage",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6114));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "sneakers_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6125));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "sweater_warm",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6120));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "tshirt_basic_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6116));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_brick_loft",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6130));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_brick_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6129));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_slate_dark",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6130));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_wood_cozy",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 5, 12, 42, 16, 65, DateTimeKind.Utc).AddTicks(6132));

            migrationBuilder.InsertData(
                table: "study_rooms",
                columns: new[] { "Id", "AccessCode", "Code", "CreatedAt", "Description", "IsPrivate", "MaxCapacity", "MusicTrackId", "Name", "OwnerUserId", "ThemeId", "Type" },
                values: new object[,]
                {
                    { new Guid("084156ea-f683-4087-b750-3bab1f0c4bb4"), null, "LIB-WOOD", new DateTime(2026, 10, 5, 12, 42, 16, 66, DateTimeKind.Utc).AddTicks(6619), "Sıcak ahşap paneller, kitap kokusu ve huzurlu akustik tınılar.", false, 20, "Acoustic Focus Guitar", "Akustik Arşiv - Ahşap Salon", null, "cozy_fireplace", 2 },
                    { new Guid("5334285e-2ea0-4f32-b0b0-121117f6e15c"), null, "SHOP-BAZAAR", new DateTime(2026, 10, 5, 12, 42, 16, 66, DateTimeKind.Utc).AddTicks(7436), "Yeni mobilyaların, aksesuarların ve kıyafetlerin sergilendiği canlı piksel çarşı.", false, 50, "market_chill", "Piksel Çarşı & Mobilya Pazarı", null, "cozy_bazaar", 3 },
                    { new Guid("7739924f-719a-4e41-b0b5-d64b7100ac0c"), null, "LIB-LOFI", new DateTime(2026, 10, 5, 12, 42, 16, 66, DateTimeKind.Utc).AddTicks(6459), "Hafif yağmur sesi ve yumuşak lo-fi ritimleriyle sessiz kütüphane ortamı.", false, 20, "Midnight Lofi Study", "Sessiz Kütüphane - Lo-Fi Salonu", null, "rainy_window", 2 },
                    { new Guid("d96634cb-a53b-4fb6-b36c-2bf0992a7f4d"), null, "LIB-NIGHT", new DateTime(2026, 10, 5, 12, 42, 16, 66, DateTimeKind.Utc).AddTicks(6622), "Gece çalışanlar için neon ve karanlık tema, derin odaklanma ambiyansı.", false, 20, "Deep Cyber Ambient", "Gece Kuşu Salonu - Siber Kütüphane", null, "neon_city_night", 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentMemories_Category",
                table: "AgentMemories",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_AgentMemories_Embedding",
                table: "AgentMemories",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentMemories_UserId",
                table: "AgentMemories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionCheckIns_CreatedAt",
                table: "SessionCheckIns",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SessionCheckIns_SessionId",
                table: "SessionCheckIns",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionCheckIns_UserId",
                table: "SessionCheckIns",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionReflections_CreatedAt",
                table: "SessionReflections",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SessionReflections_SessionId",
                table: "SessionReflections",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionReflections_UserId",
                table: "SessionReflections",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentMemories");

            migrationBuilder.DropTable(
                name: "SessionCheckIns");

            migrationBuilder.DropTable(
                name: "SessionReflections");

            migrationBuilder.DeleteData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("084156ea-f683-4087-b750-3bab1f0c4bb4"));

            migrationBuilder.DeleteData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("5334285e-2ea0-4f32-b0b0-121117f6e15c"));

            migrationBuilder.DeleteData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("7739924f-719a-4e41-b0b5-d64b7100ac0c"));

            migrationBuilder.DeleteData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("d96634cb-a53b-4fb6-b36c-2bf0992a7f4d"));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "beanie_orange",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2373));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "bookshelf_wood",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2365));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "boots_leather_brown",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2373));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "cap_black",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2374));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "chair_ergonomic_black",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2358));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "chair_gaming_rgb",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2360));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "chair_vintage_leather",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2359));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "coffee_machine",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2364));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_cyber_neon",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2357));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_gold_master",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2358));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_minimal_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2356));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_retro_oak",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2186));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "fireplace_cozy",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2366));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "floor_marble_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2407));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "floor_parquet_oak",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2405));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "floor_tatami_japanese",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2406));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "glasses_retro_round",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2375));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "headphones_pro",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2375));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "hoodie_cyber_neon",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2368));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "hoodie_gray",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2367));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jacket_leather_vintage",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2369));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jeans_basic_blue",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2370));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jeans_dark",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2370));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jogger_black",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2371));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "lamp_desk_brass",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2361));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "neon_sign_focus",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2363));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "plant_bonsai",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2361));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "plant_monstera",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2362));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "record_player_vintage",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2363));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "sneakers_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2372));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "sweater_warm",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2368));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "tshirt_basic_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2366));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_brick_loft",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2377));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_brick_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2376));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_slate_dark",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2404));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_wood_cozy",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 20, 28, 29, 960, DateTimeKind.Utc).AddTicks(2405));

            migrationBuilder.InsertData(
                table: "study_rooms",
                columns: new[] { "Id", "AccessCode", "Code", "CreatedAt", "Description", "IsPrivate", "MaxCapacity", "MusicTrackId", "Name", "OwnerUserId", "ThemeId", "Type" },
                values: new object[,]
                {
                    { new Guid("835bd9c9-ee1e-4277-ba1a-840eb7d48f5d"), null, "SHOP-BAZAAR", new DateTime(2026, 10, 4, 20, 28, 29, 961, DateTimeKind.Utc).AddTicks(4766), "Yeni mobilyaların, aksesuarların ve kıyafetlerin sergilendiği canlı piksel çarşı.", false, 50, "market_chill", "Piksel Çarşı & Mobilya Pazarı", null, "cozy_bazaar", 3 },
                    { new Guid("9a500a84-1f01-4e98-83ed-5480803a3d9a"), null, "LIB-WOOD", new DateTime(2026, 10, 4, 20, 28, 29, 961, DateTimeKind.Utc).AddTicks(3986), "Sıcak ahşap paneller, kitap kokusu ve huzurlu akustik tınılar.", false, 20, "Acoustic Focus Guitar", "Akustik Arşiv - Ahşap Salon", null, "cozy_fireplace", 2 },
                    { new Guid("9fd4da2f-3a05-409d-8e72-d9b638a1f7ba"), null, "LIB-LOFI", new DateTime(2026, 10, 4, 20, 28, 29, 961, DateTimeKind.Utc).AddTicks(3840), "Hafif yağmur sesi ve yumuşak lo-fi ritimleriyle sessiz kütüphane ortamı.", false, 20, "Midnight Lofi Study", "Sessiz Kütüphane - Lo-Fi Salonu", null, "rainy_window", 2 },
                    { new Guid("daf737c7-8d50-42ac-a8bd-9582a9762139"), null, "LIB-NIGHT", new DateTime(2026, 10, 4, 20, 28, 29, 961, DateTimeKind.Utc).AddTicks(3990), "Gece çalışanlar için neon ve karanlık tema, derin odaklanma ambiyansı.", false, 20, "Deep Cyber Ambient", "Gece Kuşu Salonu - Siber Kütüphane", null, "neon_city_night", 2 }
                });
        }
    }
}
