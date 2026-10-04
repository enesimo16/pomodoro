using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Focus.Infrastructure.Persistence;

public static class PublicRoomSeeder
{
    public static readonly StudyRoom[] InitialPublicRooms = new StudyRoom[]
    {
        StudyRoom.CreatePublicLibrary(
            "Sessiz Kütüphane - Lo-Fi Salonu",
            "LIB-LOFI",
            "Hafif yağmur sesi ve yumuşak lo-fi ritimleriyle sessiz kütüphane ortamı.",
            "rainy_window",
            "Midnight Lofi Study",
            capacity: 20),

        StudyRoom.CreatePublicLibrary(
            "Akustik Arşiv - Ahşap Salon",
            "LIB-WOOD",
            "Sıcak ahşap paneller, kitap kokusu ve huzurlu akustik tınılar.",
            "cozy_fireplace",
            "Acoustic Focus Guitar",
            capacity: 20),

        StudyRoom.CreatePublicLibrary(
            "Gece Kuşu Salonu - Siber Kütüphane",
            "LIB-NIGHT",
            "Gece çalışanlar için neon ve karanlık tema, derin odaklanma ambiyansı.",
            "neon_city_night",
            "Deep Cyber Ambient",
            capacity: 20),

        StudyRoom.CreateShopMarket(
            "Piksel Çarşı & Mobilya Pazarı",
            "SHOP-BAZAAR",
            "Yeni mobilyaların, aksesuarların ve kıyafetlerin sergilendiği canlı piksel çarşı.")
    };

    public static void SeedPublicRooms(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StudyRoom>().HasData(InitialPublicRooms);
    }
}
