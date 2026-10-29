namespace Focus.Application.Common;

public static class UserTimeZone
{
    public const string DefaultTimeZoneId = "Europe/Istanbul";

    public static TimeZoneInfo Resolve(string? timeZoneId)
    {
        foreach (var id in new[] { timeZoneId, DefaultTimeZoneId })
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                // Tanimsiz saat dilimi icin varsayilana dusulur
            }
        }

        return TimeZoneInfo.Utc;
    }

    public static DateTime ToLocal(DateTime utc, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), timeZone);
}
