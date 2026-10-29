namespace Focus.Infrastructure.Persistence;

// HasData satirlari model anlik goruntusunun parcasi oldugu icin zaman damgalari sabit olmalidir
internal static class SeedDefaults
{
    public static readonly DateTime CreatedAt = new(2026, 10, 7, 0, 0, 0, DateTimeKind.Utc);
}
