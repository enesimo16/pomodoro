using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Focus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase3EconomyAndShop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_items",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    RequiredLevel = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CoinPrice = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    SpriteUrl = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsInteractive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    GridWidth = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    GridHeight = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_streaks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentStreak = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LongestStreak = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastActivityDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FreezesAvailable = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    LastFreezeUsedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_streaks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_streaks_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_inventory_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogItemId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AcquiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsEquipped = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_inventory_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_inventory_items_catalog_items_CatalogItemId",
                        column: x => x.CatalogItemId,
                        principalTable: "catalog_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_inventory_items_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CoinPrice", "CreatedAt", "Description", "GridHeight", "GridWidth", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[,]
                {
                    { "beanie_orange", 5, 40, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6042), "Örgü sıcak turuncu bere", 1, 1, "Sıcak Turuncu Bere", 2, "/sprites/avatar/beanie_orange.png", 2 },
                    { "bookshelf_wood", 3, 150, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6034), "Ciltli kitaplarla dolu ahşap kitaplık", 1, 2, "Klasik Ahşap Kitaplık", 4, "/sprites/items/bookshelf_wood.png", 2 },
                    { "boots_leather_brown", 4, 80, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6042), "Klasik dayanıklı deri bot", 1, 1, "Kahverengi Deri Bot", 5, "/sprites/avatar/boots_brown.png", 2 },
                    { "cap_black", 5, 45, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6043), "Spor tarz siyah kep", 1, 1, "Siyah Klasik Kep", 3, "/sprites/avatar/cap_black.png", 2 }
                });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "GridHeight", "GridWidth", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[] { "chair_ergonomic_black", 2, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6028), "Omurgayı destekleyen siyah çalışma koltuğu", 1, 1, "Ergonomik Koltuk", 1, "/sprites/items/chair_ergonomic_black.png", 1 });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CoinPrice", "CreatedAt", "Description", "GridHeight", "GridWidth", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[,]
                {
                    { "chair_gaming_rgb", 2, 300, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6030), "Yumuşak dolgulu renkli oyuncu koltuğu", 1, 1, "RGB Oyuncu Koltuğu", 10, "/sprites/items/chair_gaming_rgb.png", 2 },
                    { "chair_vintage_leather", 2, 120, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6029), "Konforlu taba rengi deri koltuk", 1, 1, "Vintage Deri Koltuk", 4, "/sprites/items/chair_vintage_leather.png", 2 }
                });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CoinPrice", "CreatedAt", "Description", "GridHeight", "GridWidth", "IsInteractive", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[] { "coffee_machine", 3, 280, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6034), "Taze kahve hazırlayan espresso makinesi", 1, 1, true, "Espresso Kahve Barı", 9, "/sprites/items/coffee_machine.png", 2 });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CoinPrice", "CreatedAt", "Description", "GridHeight", "GridWidth", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[,]
                {
                    { "desk_cyber_neon", 1, 250, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6027), "Gelecekten gelen neon aydınlatmalı masa", 1, 2, "Siberpunk Neon Masa", 8, "/sprites/items/desk_cyber_neon.png", 2 },
                    { "desk_gold_master", 1, 1000, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6027), "Saf altın kaplama usta çalışma masası", 1, 2, "Altın Büyük Üstat Masası", 25, "/sprites/items/desk_gold_master.png", 2 },
                    { "desk_minimal_white", 1, 80, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6025), "Modern ve ferah çalışma alanı", 1, 2, "Minimal Beyaz Masa", 3, "/sprites/items/desk_minimal_white.png", 2 }
                });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "GridHeight", "GridWidth", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[] { "desk_retro_oak", 1, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(5874), "Klasik ahşap çalışma masası", 1, 2, "Retro Meşe Masa", 1, "/sprites/items/desk_retro_oak.png", 1 });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CoinPrice", "CreatedAt", "Description", "GridHeight", "GridWidth", "IsInteractive", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[] { "fireplace_cozy", 3, 400, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6035), "Odayı ısıtan ve çıtırtı sesleri veren şömine", 1, 2, true, "Çıtırtılı Taş Şömine", 12, "/sprites/items/fireplace_cozy.png", 2 });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CoinPrice", "CreatedAt", "Description", "GridHeight", "GridWidth", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[] { "floor_marble_white", 7, 180, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6048), "Zarif damarlı beyaz mermer zemin", 1, 1, "Lüks Beyaz Mermer", 8, "/sprites/floors/marble_white.png", 2 });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "GridHeight", "GridWidth", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[] { "floor_parquet_oak", 7, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6047), "Açık renk dayanıklı meşe parke", 1, 1, "Doğal Meşe Parke", 1, "/sprites/floors/parquet_oak.png", 1 });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CoinPrice", "CreatedAt", "Description", "GridHeight", "GridWidth", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[,]
                {
                    { "floor_tatami_japanese", 7, 90, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6048), "Hasır dokulu geleneksel Japon zemin kaplaması", 1, 1, "Japon Tatami Zemin", 4, "/sprites/floors/tatami.png", 2 },
                    { "glasses_retro_round", 5, 75, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6043), "Metal çerçeveli yuvarlak gözlük", 1, 1, "Retro Yuvarlak Gözlük", 4, "/sprites/avatar/glasses_round.png", 2 },
                    { "headphones_pro", 5, 220, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6044), "Gürültü engelleyici profesyonel kafa üstü kulaklık", 1, 1, "Stüdyo Kulaklığı", 8, "/sprites/avatar/headphones_pro.png", 2 },
                    { "hoodie_cyber_neon", 4, 200, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6038), "Karanlıkta parlayan neon şeritli özel kapüşonlu", 1, 1, "Siberpunk Neon Hoodie", 7, "/sprites/avatar/hoodie_cyber_neon.png", 2 }
                });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "GridHeight", "GridWidth", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[] { "hoodie_gray", 4, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6036), "Rahat gri kapüşonlu sweatshirt", 1, 1, "Gri Kapüşonlu", 1, "/sprites/avatar/hoodie_gray.png", 1 });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CoinPrice", "CreatedAt", "Description", "GridHeight", "GridWidth", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[] { "jacket_leather_vintage", 4, 350, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6038), "Retro kahverengi şık deri ceket", 1, 1, "Vintage Deri Ceket", 11, "/sprites/avatar/jacket_leather_vintage.png", 2 });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "GridHeight", "GridWidth", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[] { "jeans_basic_blue", 4, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6039), "Klasik düz kesim mavi kot pantolon", 1, 1, "Temel Mavi Kot", 1, "/sprites/avatar/jeans_blue.png", 1 });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CoinPrice", "CreatedAt", "Description", "GridHeight", "GridWidth", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[,]
                {
                    { "jeans_dark", 4, 45, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6040), "Koyu indigo rengi şık kot pantolon", 1, 1, "Koyu Kot Pantolon", 2, "/sprites/avatar/jeans_dark.png", 2 },
                    { "jogger_black", 4, 65, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6040), "Çalışma seansları için rahat siyah jogger", 1, 1, "Siyah Eşofman", 4, "/sprites/avatar/jogger_black.png", 2 }
                });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "GridHeight", "GridWidth", "IsInteractive", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[] { "lamp_desk_brass", 3, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6030), "Masaya sıcak bir ışık huzmesi veren lamba", 1, 1, true, "Pirinç Masa Lambası", 1, "/sprites/items/lamp_desk_brass.png", 1 });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CoinPrice", "CreatedAt", "Description", "GridHeight", "GridWidth", "IsInteractive", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[] { "neon_sign_focus", 3, 180, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6032), "Duvara asılan mor neon tabela", 1, 2, true, "Neon FOCUS Tabelası", 6, "/sprites/items/neon_sign_focus.png", 2 });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CoinPrice", "CreatedAt", "Description", "GridHeight", "GridWidth", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[,]
                {
                    { "plant_bonsai", 3, 60, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6031), "Masada sakinlik veren minyatür bonsai ağacı", 1, 1, "Zen Bonsai Ağacı", 2, "/sprites/items/plant_bonsai.png", 2 },
                    { "plant_monstera", 3, 90, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6032), "Odaya ferahlık katan geniş yapraklı bitki", 1, 1, "Büyük Monstera", 5, "/sprites/items/plant_monstera.png", 2 }
                });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CoinPrice", "CreatedAt", "Description", "GridHeight", "GridWidth", "IsInteractive", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[] { "record_player_vintage", 3, 220, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6033), "Lo-Fi çalan ahşap nostaljik plak çalar", 1, 1, true, "Retro Plak Çalar", 7, "/sprites/items/record_player_vintage.png", 2 });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "GridHeight", "GridWidth", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[] { "sneakers_white", 4, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6041), "Rahat beyaz spor ayakkabı", 1, 1, "Beyaz Spor Ayakkabı", 1, "/sprites/avatar/sneakers_white.png", 1 });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CoinPrice", "CreatedAt", "Description", "GridHeight", "GridWidth", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[] { "sweater_warm", 4, 50, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6037), "Kahverengi örgü sıcak kazak", 1, 1, "Sıcak Örgü Kazak", 2, "/sprites/avatar/sweater_warm.png", 2 });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "GridHeight", "GridWidth", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[] { "tshirt_basic_white", 4, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6036), "Sade pamuklu beyaz tişört", 1, 1, "Temel Beyaz Tişört", 1, "/sprites/avatar/tshirt_white.png", 1 });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CoinPrice", "CreatedAt", "Description", "GridHeight", "GridWidth", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[] { "wallpaper_brick_loft", 6, 70, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6045), "Kırmızı tuğla loft duvar kağıdı", 1, 1, "Endüstriyel Loft Tuğla", 3, "/sprites/walls/brick_loft.png", 2 });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "GridHeight", "GridWidth", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[] { "wallpaper_brick_white", 6, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6045), "Aydınlık ve ferah beyaz tuğla", 1, 1, "Beyaz Tuğla Duvar", 1, "/sprites/walls/brick_white.png", 1 });

            migrationBuilder.InsertData(
                table: "catalog_items",
                columns: new[] { "Id", "Category", "CoinPrice", "CreatedAt", "Description", "GridHeight", "GridWidth", "Name", "RequiredLevel", "SpriteUrl", "Tier" },
                values: new object[,]
                {
                    { "wallpaper_slate_dark", 6, 100, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6046), "Modern koyu gri arduvaz taş kaplama", 1, 1, "Koyu Arduvaz Duvar", 5, "/sprites/walls/slate_dark.png", 2 },
                    { "wallpaper_wood_cozy", 6, 120, new DateTime(2026, 10, 4, 17, 45, 32, 56, DateTimeKind.Utc).AddTicks(6047), "Ahşap panel kaplama sıcak duvar", 1, 1, "Sıcak Doğal Ahşap Duvar", 6, "/sprites/walls/wood_cozy.png", 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_inventory_items_CatalogItemId",
                table: "user_inventory_items",
                column: "CatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_user_inventory_items_UserId_CatalogItemId",
                table: "user_inventory_items",
                columns: new[] { "UserId", "CatalogItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_user_streaks_UserId",
                table: "user_streaks",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_inventory_items");

            migrationBuilder.DropTable(
                name: "user_streaks");

            migrationBuilder.DropTable(
                name: "catalog_items");
        }
    }
}
