using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Focus.Infrastructure.Persistence;

public static class CatalogSeeder
{
    private static readonly CatalogItem[] InitialItems = new CatalogItem[]
    {
            // Masalar
            new("desk_retro_oak", "Retro Meşe Masa", "Klasik ahşap çalışma masası", CatalogCategory.Desk, CatalogTier.Free, 1, 0, "/sprites/items/desk_retro_oak.png", false, 2, 1),
            new("desk_minimal_white", "Minimal Beyaz Masa", "Modern ve ferah çalışma alanı", CatalogCategory.Desk, CatalogTier.LevelLocked, 3, 80, "/sprites/items/desk_minimal_white.png", false, 2, 1),
            new("desk_cyber_neon", "Siberpunk Neon Masa", "Gelecekten gelen neon aydınlatmalı masa", CatalogCategory.Desk, CatalogTier.LevelLocked, 8, 250, "/sprites/items/desk_cyber_neon.png", false, 2, 1),
            new("desk_gold_master", "Altın Büyük Üstat Masası", "Saf altın kaplama usta çalışma masası", CatalogCategory.Desk, CatalogTier.LevelLocked, 25, 1000, "/sprites/items/desk_gold_master.png", false, 2, 1),

            // Sandalyeler
            new("chair_ergonomic_black", "Ergonomik Koltuk", "Omurgayı destekleyen siyah çalışma koltuğu", CatalogCategory.Chair, CatalogTier.Free, 1, 0, "/sprites/items/chair_ergonomic_black.png", false, 1, 1),
            new("chair_vintage_leather", "Vintage Deri Koltuk", "Konforlu taba rengi deri koltuk", CatalogCategory.Chair, CatalogTier.LevelLocked, 4, 120, "/sprites/items/chair_vintage_leather.png", false, 1, 1),
            new("chair_gaming_rgb", "RGB Oyuncu Koltuğu", "Yumuşak dolgulu renkli oyuncu koltuğu", CatalogCategory.Chair, CatalogTier.LevelLocked, 10, 300, "/sprites/items/chair_gaming_rgb.png", false, 1, 1),

            // Oda Mobilyaları & Aksesuarlar
            new("lamp_desk_brass", "Pirinç Masa Lambası", "Masaya sıcak bir ışık huzmesi veren lamba", CatalogCategory.Furniture, CatalogTier.Free, 1, 0, "/sprites/items/lamp_desk_brass.png", true, 1, 1),
            new("plant_bonsai", "Zen Bonsai Ağacı", "Masada sakinlik veren minyatür bonsai ağacı", CatalogCategory.Furniture, CatalogTier.LevelLocked, 2, 60, "/sprites/items/plant_bonsai.png", false, 1, 1),
            new("plant_monstera", "Büyük Monstera", "Odaya ferahlık katan geniş yapraklı bitki", CatalogCategory.Furniture, CatalogTier.LevelLocked, 5, 90, "/sprites/items/plant_monstera.png", false, 1, 1),
            new("neon_sign_focus", "Neon FOCUS Tabelası", "Duvara asılan mor neon tabela", CatalogCategory.Furniture, CatalogTier.LevelLocked, 6, 180, "/sprites/items/neon_sign_focus.png", true, 2, 1),
            new("record_player_vintage", "Retro Plak Çalar", "Lo-Fi çalan ahşap nostaljik plak çalar", CatalogCategory.Furniture, CatalogTier.LevelLocked, 7, 220, "/sprites/items/record_player_vintage.png", true, 1, 1),
            new("coffee_machine", "Espresso Kahve Barı", "Taze kahve hazırlayan espresso makinesi", CatalogCategory.Furniture, CatalogTier.LevelLocked, 9, 280, "/sprites/items/coffee_machine.png", true, 1, 1),
            new("bookshelf_wood", "Klasik Ahşap Kitaplık", "Ciltli kitaplarla dolu ahşap kitaplık", CatalogCategory.Furniture, CatalogTier.LevelLocked, 4, 150, "/sprites/items/bookshelf_wood.png", false, 2, 1),
            new("fireplace_cozy", "Çıtırtılı Taş Şömine", "Odayı ısıtan ve çıtırtı sesleri veren şömine", CatalogCategory.Furniture, CatalogTier.LevelLocked, 12, 400, "/sprites/items/fireplace_cozy.png", true, 2, 1),

            // Üst Kıyafetler
            new("tshirt_basic_white", "Temel Beyaz Tişört", "Sade pamuklu beyaz tişört", CatalogCategory.Clothing, CatalogTier.Free, 1, 0, "/sprites/avatar/tshirt_white.png"),
            new("hoodie_gray", "Gri Kapüşonlu", "Rahat gri kapüşonlu sweatshirt", CatalogCategory.Clothing, CatalogTier.Free, 1, 0, "/sprites/avatar/hoodie_gray.png"),
            new("sweater_warm", "Sıcak Örgü Kazak", "Kahverengi örgü sıcak kazak", CatalogCategory.Clothing, CatalogTier.LevelLocked, 2, 50, "/sprites/avatar/sweater_warm.png"),
            new("hoodie_cyber_neon", "Siberpunk Neon Hoodie", "Karanlıkta parlayan neon şeritli özel kapüşonlu", CatalogCategory.Clothing, CatalogTier.LevelLocked, 7, 200, "/sprites/avatar/hoodie_cyber_neon.png"),
            new("jacket_leather_vintage", "Vintage Deri Ceket", "Retro kahverengi şık deri ceket", CatalogCategory.Clothing, CatalogTier.LevelLocked, 11, 350, "/sprites/avatar/jacket_leather_vintage.png"),

            // Alt Kıyafetler
            new("jeans_basic_blue", "Temel Mavi Kot", "Klasik düz kesim mavi kot pantolon", CatalogCategory.Clothing, CatalogTier.Free, 1, 0, "/sprites/avatar/jeans_blue.png"),
            new("jeans_dark", "Koyu Kot Pantolon", "Koyu indigo rengi şık kot pantolon", CatalogCategory.Clothing, CatalogTier.LevelLocked, 2, 45, "/sprites/avatar/jeans_dark.png"),
            new("jogger_black", "Siyah Eşofman", "Çalışma seansları için rahat siyah jogger", CatalogCategory.Clothing, CatalogTier.LevelLocked, 4, 65, "/sprites/avatar/jogger_black.png"),

            // Ayakkabılar
            new("sneakers_white", "Beyaz Spor Ayakkabı", "Rahat beyaz spor ayakkabı", CatalogCategory.Clothing, CatalogTier.Free, 1, 0, "/sprites/avatar/sneakers_white.png"),
            new("boots_leather_brown", "Kahverengi Deri Bot", "Klasik dayanıklı deri bot", CatalogCategory.Clothing, CatalogTier.LevelLocked, 5, 80, "/sprites/avatar/boots_brown.png"),

            // Aksesuarlar
            new("beanie_orange", "Sıcak Turuncu Bere", "Örgü sıcak turuncu bere", CatalogCategory.Accessory, CatalogTier.LevelLocked, 2, 40, "/sprites/avatar/beanie_orange.png"),
            new("cap_black", "Siyah Klasik Kep", "Spor tarz siyah kep", CatalogCategory.Accessory, CatalogTier.LevelLocked, 3, 45, "/sprites/avatar/cap_black.png"),
            new("glasses_retro_round", "Retro Yuvarlak Gözlük", "Metal çerçeveli yuvarlak gözlük", CatalogCategory.Accessory, CatalogTier.LevelLocked, 4, 75, "/sprites/avatar/glasses_round.png"),
            new("headphones_pro", "Stüdyo Kulaklığı", "Gürültü engelleyici profesyonel kafa üstü kulaklık", CatalogCategory.Accessory, CatalogTier.LevelLocked, 8, 220, "/sprites/avatar/headphones_pro.png"),

            // Duvar Kağıtları
            new("wallpaper_brick_white", "Beyaz Tuğla Duvar", "Aydınlık ve ferah beyaz tuğla", CatalogCategory.Wallpaper, CatalogTier.Free, 1, 0, "/sprites/walls/brick_white.png"),
            new("wallpaper_brick_loft", "Endüstriyel Loft Tuğla", "Kırmızı tuğla loft duvar kağıdı", CatalogCategory.Wallpaper, CatalogTier.LevelLocked, 3, 70, "/sprites/walls/brick_loft.png"),
            new("wallpaper_slate_dark", "Koyu Arduvaz Duvar", "Modern koyu gri arduvaz taş kaplama", CatalogCategory.Wallpaper, CatalogTier.LevelLocked, 5, 100, "/sprites/walls/slate_dark.png"),
            new("wallpaper_wood_cozy", "Sıcak Doğal Ahşap Duvar", "Ahşap panel kaplama sıcak duvar", CatalogCategory.Wallpaper, CatalogTier.LevelLocked, 6, 120, "/sprites/walls/wood_cozy.png"),

            // Zeminler
            new("floor_parquet_oak", "Doğal Meşe Parke", "Açık renk dayanıklı meşe parke", CatalogCategory.Floor, CatalogTier.Free, 1, 0, "/sprites/floors/parquet_oak.png"),
            new("floor_tatami_japanese", "Japon Tatami Zemin", "Hasır dokulu geleneksel Japon zemin kaplaması", CatalogCategory.Floor, CatalogTier.LevelLocked, 4, 90, "/sprites/floors/tatami.png"),
            new("floor_marble_white", "Lüks Beyaz Mermer", "Zarif damarlı beyaz mermer zemin", CatalogCategory.Floor, CatalogTier.LevelLocked, 8, 180, "/sprites/floors/marble_white.png")
    };

    public static void SeedCatalogItems(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CatalogItem>().HasData(InitialItems);
    }
}
