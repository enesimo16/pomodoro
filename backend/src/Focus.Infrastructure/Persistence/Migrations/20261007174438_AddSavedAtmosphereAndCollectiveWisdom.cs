using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Focus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedAtmosphereAndCollectiveWisdom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("00ff7f58-28ad-42f5-89fa-8d8585f417e8"));

            migrationBuilder.DeleteData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("16da55f1-1228-4822-8e8c-2447559af1b1"));

            migrationBuilder.DeleteData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("4cf04f25-240d-47d9-b299-fc9a1866692a"));

            migrationBuilder.DeleteData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("8174bf8d-0745-47c7-8ac5-c17a0449f46d"));

            migrationBuilder.CreateTable(
                name: "SavedAtmospheres",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Aesthetic = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WallColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FloorColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AccentLightColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WallPaperId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FloorId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    WeatherEffect = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WindowVideoQuery = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MusicGenre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MusicSearchQuery = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    MusicVolume = table.Column<float>(type: "real", nullable: false),
                    AmbienceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AmbienceVolume = table.Column<float>(type: "real", nullable: false),
                    NoiseType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NoiseVolume = table.Column<float>(type: "real", nullable: false),
                    CutoffFrequencyHz = table.Column<int>(type: "integer", nullable: false),
                    TextureType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TextureVolume = table.Column<float>(type: "real", nullable: false),
                    RecommendedMinutes = table.Column<int>(type: "integer", nullable: false),
                    BreakMinutes = table.Column<int>(type: "integer", nullable: false),
                    FlowShieldLevel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    PromptUsed = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedAtmospheres", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedAtmospheres_users_UserId",
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
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5018));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "bookshelf_wood",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5001));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "boots_leather_brown",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5017));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "cap_black",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5019));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "chair_ergonomic_black",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(4988));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "chair_gaming_rgb",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(4991));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "chair_vintage_leather",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(4989));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "coffee_machine",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5000));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_cyber_neon",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(4984));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_gold_master",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(4987));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_minimal_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(4954));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_retro_oak",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(4613));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "fireplace_cozy",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5002));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "floor_marble_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5032));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "floor_parquet_oak",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5029));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "floor_tatami_japanese",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5030));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "glasses_retro_round",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5022));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "headphones_pro",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5023));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "hoodie_cyber_neon",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5009));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "hoodie_gray",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5005));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jacket_leather_vintage",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5011));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jeans_basic_blue",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5012));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jeans_dark",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5014));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jogger_black",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5015));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "lamp_desk_brass",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(4992));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "neon_sign_focus",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(4996));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "plant_bonsai",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(4994));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "plant_monstera",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(4995));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "record_player_vintage",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(4999));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "sneakers_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5016));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "sweater_warm",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5007));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "tshirt_basic_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5004));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_brick_loft",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5025));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_brick_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5024));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_slate_dark",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5026));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_wood_cozy",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 7, 17, 44, 37, 247, DateTimeKind.Utc).AddTicks(5028));

            migrationBuilder.InsertData(
                table: "study_rooms",
                columns: new[] { "Id", "AccessCode", "Code", "CreatedAt", "Description", "IsPrivate", "MaxCapacity", "MusicTrackId", "Name", "OwnerUserId", "ThemeId", "Type" },
                values: new object[,]
                {
                    { new Guid("527706df-6f43-4bb8-8637-96adc5a342ec"), null, "LIB-NIGHT", new DateTime(2026, 10, 7, 17, 44, 37, 249, DateTimeKind.Utc).AddTicks(6513), "Gece çalışanlar için neon ve karanlık tema, derin odaklanma ambiyansı.", false, 20, "Deep Cyber Ambient", "Gece Kuşu Salonu - Siber Kütüphane", null, "neon_city_night", 2 },
                    { new Guid("5ec20c31-815c-47b6-893c-92e3b1f4dc2a"), null, "LIB-LOFI", new DateTime(2026, 10, 7, 17, 44, 37, 249, DateTimeKind.Utc).AddTicks(6044), "Hafif yağmur sesi ve yumuşak lo-fi ritimleriyle sessiz kütüphane ortamı.", false, 20, "Midnight Lofi Study", "Sessiz Kütüphane - Lo-Fi Salonu", null, "rainy_window", 2 },
                    { new Guid("c4c20115-3d9e-419d-8275-19826fb544d1"), null, "SHOP-BAZAAR", new DateTime(2026, 10, 7, 17, 44, 37, 249, DateTimeKind.Utc).AddTicks(8195), "Yeni mobilyaların, aksesuarların ve kıyafetlerin sergilendiği canlı piksel çarşı.", false, 50, "market_chill", "Piksel Çarşı & Mobilya Pazarı", null, "cozy_bazaar", 3 },
                    { new Guid("eff56794-b4ab-4664-9284-94c90a20cd13"), null, "LIB-WOOD", new DateTime(2026, 10, 7, 17, 44, 37, 249, DateTimeKind.Utc).AddTicks(6376), "Sıcak ahşap paneller, kitap kokusu ve huzurlu akustik tınılar.", false, 20, "Acoustic Focus Guitar", "Akustik Arşiv - Ahşap Salon", null, "cozy_fireplace", 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedAtmospheres_CreatedAt",
                table: "SavedAtmospheres",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SavedAtmospheres_UserId",
                table: "SavedAtmospheres",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedAtmospheres");

            migrationBuilder.DeleteData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("527706df-6f43-4bb8-8637-96adc5a342ec"));

            migrationBuilder.DeleteData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("5ec20c31-815c-47b6-893c-92e3b1f4dc2a"));

            migrationBuilder.DeleteData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("c4c20115-3d9e-419d-8275-19826fb544d1"));

            migrationBuilder.DeleteData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("eff56794-b4ab-4664-9284-94c90a20cd13"));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "beanie_orange",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7522));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "bookshelf_wood",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7514));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "boots_leather_brown",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7522));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "cap_black",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7523));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "chair_ergonomic_black",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7508));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "chair_gaming_rgb",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7509));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "chair_vintage_leather",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7508));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "coffee_machine",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7513));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_cyber_neon",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7506));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_gold_master",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7507));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_minimal_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7505));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_retro_oak",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7325));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "fireplace_cozy",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7515));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "floor_marble_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7531));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "floor_parquet_oak",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7528));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "floor_tatami_japanese",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7530));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "glasses_retro_round",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7524));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "headphones_pro",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7524));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "hoodie_cyber_neon",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7518));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "hoodie_gray",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7516));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jacket_leather_vintage",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7518));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jeans_basic_blue",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7519));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jeans_dark",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7520));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jogger_black",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7520));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "lamp_desk_brass",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7510));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "neon_sign_focus",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7512));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "plant_bonsai",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7511));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "plant_monstera",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7511));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "record_player_vintage",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7513));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "sneakers_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7521));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "sweater_warm",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7517));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "tshirt_basic_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7516));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_brick_loft",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7525));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_brick_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7525));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_slate_dark",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7526));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_wood_cozy",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 11, 16, 43, 157, DateTimeKind.Utc).AddTicks(7527));

            migrationBuilder.InsertData(
                table: "study_rooms",
                columns: new[] { "Id", "AccessCode", "Code", "CreatedAt", "Description", "IsPrivate", "MaxCapacity", "MusicTrackId", "Name", "OwnerUserId", "ThemeId", "Type" },
                values: new object[,]
                {
                    { new Guid("00ff7f58-28ad-42f5-89fa-8d8585f417e8"), null, "SHOP-BAZAAR", new DateTime(2026, 10, 6, 11, 16, 43, 159, DateTimeKind.Utc).AddTicks(375), "Yeni mobilyaların, aksesuarların ve kıyafetlerin sergilendiği canlı piksel çarşı.", false, 50, "market_chill", "Piksel Çarşı & Mobilya Pazarı", null, "cozy_bazaar", 3 },
                    { new Guid("16da55f1-1228-4822-8e8c-2447559af1b1"), null, "LIB-NIGHT", new DateTime(2026, 10, 6, 11, 16, 43, 158, DateTimeKind.Utc).AddTicks(9181), "Gece çalışanlar için neon ve karanlık tema, derin odaklanma ambiyansı.", false, 20, "Deep Cyber Ambient", "Gece Kuşu Salonu - Siber Kütüphane", null, "neon_city_night", 2 },
                    { new Guid("4cf04f25-240d-47d9-b299-fc9a1866692a"), null, "LIB-LOFI", new DateTime(2026, 10, 6, 11, 16, 43, 158, DateTimeKind.Utc).AddTicks(9009), "Hafif yağmur sesi ve yumuşak lo-fi ritimleriyle sessiz kütüphane ortamı.", false, 20, "Midnight Lofi Study", "Sessiz Kütüphane - Lo-Fi Salonu", null, "rainy_window", 2 },
                    { new Guid("8174bf8d-0745-47c7-8ac5-c17a0449f46d"), null, "LIB-WOOD", new DateTime(2026, 10, 6, 11, 16, 43, 158, DateTimeKind.Utc).AddTicks(9179), "Sıcak ahşap paneller, kitap kokusu ve huzurlu akustik tınılar.", false, 20, "Acoustic Focus Guitar", "Akustik Arşiv - Ahşap Salon", null, "cozy_fireplace", 2 }
                });
        }
    }
}
