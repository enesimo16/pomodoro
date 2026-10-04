using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Focus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase4SocialCoWorkingAndLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPro",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "study_rooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaxCapacity = table.Column<int>(type: "integer", nullable: false),
                    IsPrivate = table.Column<bool>(type: "boolean", nullable: false),
                    AccessCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ThemeId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    MusicTrackId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_study_rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_study_rooms_users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "room_invitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    InviterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InviteeUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    InviteeUsername = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    InviteCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsAccepted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_invitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_room_invitations_study_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "study_rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_room_invitations_users_InviteeUserId",
                        column: x => x.InviteeUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_room_invitations_users_InviterUserId",
                        column: x => x.InviterUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "room_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatIndex = table.Column<int>(type: "integer", nullable: true),
                    IsFocusing = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastActiveAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_room_members_study_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "study_rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_room_members_users_UserId",
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

            migrationBuilder.CreateIndex(
                name: "IX_room_invitations_InviteCode",
                table: "room_invitations",
                column: "InviteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_room_invitations_InviteeUserId",
                table: "room_invitations",
                column: "InviteeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_room_invitations_InviterUserId",
                table: "room_invitations",
                column: "InviterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_room_invitations_RoomId",
                table: "room_invitations",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_room_members_RoomId_UserId",
                table: "room_members",
                columns: new[] { "RoomId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_room_members_UserId",
                table: "room_members",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_study_rooms_Code",
                table: "study_rooms",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_study_rooms_OwnerUserId",
                table: "study_rooms",
                column: "OwnerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "room_invitations");

            migrationBuilder.DropTable(
                name: "room_members");

            migrationBuilder.DropTable(
                name: "study_rooms");

            migrationBuilder.DropColumn(
                name: "IsPro",
                table: "users");

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "beanie_orange",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6042));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "bookshelf_wood",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6034));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "boots_leather_brown",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6042));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "cap_black",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6043));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "chair_ergonomic_black",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6028));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "chair_gaming_rgb",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6030));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "chair_vintage_leather",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6029));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "coffee_machine",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6034));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_cyber_neon",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6027));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_gold_master",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6027));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_minimal_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6025));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "desk_retro_oak",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(5874));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "fireplace_cozy",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6035));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "floor_marble_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6048));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "floor_parquet_oak",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6047));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "floor_tatami_japanese",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6048));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "glasses_retro_round",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6043));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "headphones_pro",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6044));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "hoodie_cyber_neon",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6038));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "hoodie_gray",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6036));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jacket_leather_vintage",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6038));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jeans_basic_blue",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6039));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jeans_dark",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6040));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "jogger_black",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6040));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "lamp_desk_brass",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6030));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "neon_sign_focus",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6032));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "plant_bonsai",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6031));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "plant_monstera",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6032));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "record_player_vintage",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6033));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "sneakers_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6041));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "sweater_warm",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6037));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "tshirt_basic_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6036));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_brick_loft",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6045));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_brick_white",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6045));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_slate_dark",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6046));

            migrationBuilder.UpdateData(
                table: "catalog_items",
                keyColumn: "Id",
                keyValue: "wallpaper_wood_cozy",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6047));
        }
    }
}
