namespace Focus.Admin.Common;

public static class LocalTime
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    public static DateTime FromUtc(DateTime utc)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);
    }

    private static TimeZoneInfo ResolveZone()
    {
        return TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Istanbul", out var zone)
            ? zone
            : TimeZoneInfo.CreateCustomTimeZone("TRT", TimeSpan.FromHours(3), "TRT", "TRT");
    }
}
