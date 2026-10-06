using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Focus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase6WrappedGuestbookNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ActionUrl = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomGuestbookEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudyRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderDisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    GiftType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomGuestbookEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomGuestbookEntries_study_rooms_StudyRoomId",
                        column: x => x.StudyRoomId,
                        principalTable: "study_rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoomGuestbookEntries_users_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomGuestbookEntries_CreatedAt",
                table: "RoomGuestbookEntries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RoomGuestbookEntries_SenderUserId",
                table: "RoomGuestbookEntries",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomGuestbookEntries_StudyRoomId",
                table: "RoomGuestbookEntries",
                column: "StudyRoomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "RoomGuestbookEntries");

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
        }
    }
}
