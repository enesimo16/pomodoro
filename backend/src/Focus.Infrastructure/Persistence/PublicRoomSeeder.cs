using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Focus.Infrastructure.Persistence;

public static class PublicRoomSeeder
{
    // Kimlikler mevcut veritabanlarindaki satirlarla ayni tutulur; boylece oda uyeleri ve diger bagimli kayitlar etkilenmez
    public static readonly StudyRoom[] InitialPublicRooms = new StudyRoom[]
    {
        StudyRoom.CreatePublicLibrary(
            "Sessiz Kütüphane - Lo-Fi Salonu",
            "LIB-LOFI",
            "Hafif yağmur sesi ve yumuşak lo-fi ritimleriyle sessiz kütüphane ortamı.",
            "rainy_window",
            "Midnight Lofi Study",
            capacity: 20,
            id: new Guid("5ec20c31-815c-47b6-893c-92e3b1f4dc2a"),
            createdAt: SeedDefaults.CreatedAt),

        StudyRoom.CreatePublicLibrary(
            "Akustik Arşiv - Ahşap Salon",
            "LIB-WOOD",
            "Sıcak ahşap paneller, kitap kokusu ve huzurlu akustik tınılar.",
            "cozy_fireplace",
            "Acoustic Focus Guitar",
            capacity: 20,
            id: new Guid("eff56794-b4ab-4664-9284-94c90a20cd13"),
            createdAt: SeedDefaults.CreatedAt),

        StudyRoom.CreatePublicLibrary(
            "Gece Kuşu Salonu - Siber Kütüphane",
            "LIB-NIGHT",
            "Gece çalışanlar için neon ve karanlık tema, derin odaklanma ambiyansı.",
            "neon_city_night",
            "Deep Cyber Ambient",
            capacity: 20,
            id: new Guid("527706df-6f43-4bb8-8637-96adc5a342ec"),
            createdAt: SeedDefaults.CreatedAt),

        StudyRoom.CreateShopMarket(
            "Piksel Çarşı & Mobilya Pazarı",
            "SHOP-BAZAAR",
            "Yeni mobilyaların, aksesuarların ve kıyafetlerin sergilendiği canlı piksel çarşı.",
            id: new Guid("c4c20115-3d9e-419d-8275-19826fb544d1"),
            createdAt: SeedDefaults.CreatedAt)
    };

    public static void SeedPublicRooms(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StudyRoom>().HasData(InitialPublicRooms);
    }
}
