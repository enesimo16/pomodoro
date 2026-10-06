using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Focus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDeviceSessionTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("0f2e0839-1090-4361-acd2-69eac6cd9a3e"));

            migrationBuilder.DeleteData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("d21a7435-8bf0-4c1d-8b0f-fe161b1834c3"));

            migrationBuilder.DeleteData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("e1ea1ad7-7769-4957-b73b-9d40405e5797"));

            migrationBuilder.DeleteData(
                table: "study_rooms",
                keyColumn: "Id",
                keyValue: new Guid("e72f5d15-ca76-4a51-bfe7-06f27504884b"));

            migrationBuilder.CreateTable(
                name: "UserDeviceSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DeviceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OperatingSystem = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Browser = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LoginCount = table.Column<int>(type: "integer", nullable: false),
                    ActiveMinutes = table.Column<int>(type: "integer", nullable: false),
                    LastPath = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FirstSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDeviceSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDeviceSessions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceSessions_IpAddress",
                table: "UserDeviceSessions",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceSessions_LastSeenAt",
                table: "UserDeviceSessions",
                column: "LastSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceSessions_UserId",
                table: "UserDeviceSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDeviceSessions");

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

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "beanie_orange",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6392));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "bookshelf_wood",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6385));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "boots_leather_brown",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6392));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "cap_black",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6393));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "chair_ergonomic_black",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6365));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "chair_gaming_rgb",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6380));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "chair_vintage_leather",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6379));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "coffee_machine",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6384));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_cyber_neon",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6364));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_gold_master",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6364));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_minimal_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6362));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_retro_oak",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6204));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "fireplace_cozy",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6385));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "floor_marble_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6399));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "floor_parquet_oak",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6397));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "floor_tatami_japanese",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6398));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "glasses_retro_round",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6394));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "headphones_pro",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6394));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "hoodie_cyber_neon",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6388));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "hoodie_gray",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6386));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jacket_leather_vintage",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6388));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jeans_basic_blue",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6389));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jeans_dark",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6390));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jogger_black",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6390));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "lamp_desk_brass",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6380));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "neon_sign_focus",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6383));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "plant_bonsai",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6381));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "plant_monstera",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6382));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "record_player_vintage",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6383));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "sneakers_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6391));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "sweater_warm",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6387));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "tshirt_basic_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6386));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_brick_loft",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6395));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_brick_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6395));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_slate_dark",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6396));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_wood_cozy",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 9, 12, 16, 643, DateTimeKind.Utc).AddTicks(6397));

            migrationBuilder.InsertData(
                table: "study_rooms",
                columns: new[] { "Id", "AccessCode", "Code", "CreatedAt", "Description", "IsPrivate", "MaxCapacity", "MusicTrackId", "Name", "OwnerUserId", "ThemeId", "Type" },
                values: new object[,]
                {
                    { new Guid("0f2e0839-1090-4361-acd2-69eac6cd9a3e"), null, "LIB-NIGHT", new DateTime(2026, 10, 6, 9, 12, 16, 644, DateTimeKind.Utc).AddTicks(5037), "Gece çalışanlar için neon ve karanlık tema, derin odaklanma ambiyansı.", false, 20, "Deep Cyber Ambient", "Gece Kuşu Salonu - Siber Kütüphane", null, "neon_city_night", 2 },
                    { new Guid("d21a7435-8bf0-4c1d-8b0f-fe161b1834c3"), null, "LIB-WOOD", new DateTime(2026, 10, 6, 9, 12, 16, 644, DateTimeKind.Utc).AddTicks(5035), "Sıcak ahşap paneller, kitap kokusu ve huzurlu akustik tınılar.", false, 20, "Acoustic Focus Guitar", "Akustik Arşiv - Ahşap Salon", null, "cozy_fireplace", 2 },
                    { new Guid("e1ea1ad7-7769-4957-b73b-9d40405e5797"), null, "SHOP-BAZAAR", new DateTime(2026, 10, 6, 9, 12, 16, 644, DateTimeKind.Utc).AddTicks(5739), "Yeni mobilyaların, aksesuarların ve kıyafetlerin sergilendiği canlı piksel çarşı.", false, 50, "market_chill", "Piksel Çarşı & Mobilya Pazarı", null, "cozy_bazaar", 3 },
                    { new Guid("e72f5d15-ca76-4a51-bfe7-06f27504884b"), null, "LIB-LOFI", new DateTime(2026, 10, 6, 9, 12, 16, 644, DateTimeKind.Utc).AddTicks(4901), "Hafif yağmur sesi ve yumuşak lo-fi ritimleriyle sessiz kütüphane ortamı.", false, 20, "Midnight Lofi Study", "Sessiz Kütüphane - Lo-Fi Salonu", null, "rainy_window", 2 }
                });
        }
    }
}
