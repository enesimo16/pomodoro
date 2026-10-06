using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Wrapped.DTOs;
using Focus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Focus.Infrastructure.Services;

public class FocusWrappedService : IFocusWrappedService
{
    private readonly IApplicationDbContext _context;

    public FocusWrappedService(IApplicationDbContext context)
    {
        _context = context;
    }

    public Task<FocusWrappedDto> GetWeeklyWrappedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-7);
        return BuildWrappedAsync(userId, "Weekly", startDate, endDate, cancellationToken);
    }

    public Task<FocusWrappedDto> GetMonthlyWrappedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-30);
        return BuildWrappedAsync(userId, "Monthly", startDate, endDate, cancellationToken);
    }

    private async Task<FocusWrappedDto> BuildWrappedAsync(
        Guid userId,
        string period,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var allSessions = await _context.FocusSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.StartedAt >= startDate && s.StartedAt <= endDate)
            .ToListAsync(cancellationToken);

        var completedSessions = allSessions
            .Where(s => s.Status == SessionStatus.Completed)
            .ToList();

        var totalSessions = allSessions.Count;
        var completedCount = completedSessions.Count;
        var completionRate = totalSessions > 0 ? Math.Round((double)completedCount / totalSessions * 100, 1) : 0;
        var totalFocusMinutes = completedSessions.Sum(s => s.NetDurationSeconds) / 60;
        var coinsEarned = completedSessions.Sum(s => s.CoinsEarned);
        var xpEarned = completedSessions.Sum(s => s.XpEarned);

        // En verimli gün
        var topDayOfWeek = "Pazartesi";
        if (completedSessions.Count > 0)
        {
            var dayGroup = completedSessions
                .GroupBy(s => s.StartedAt.DayOfWeek)
                .OrderByDescending(g => g.Sum(s => s.NetDurationSeconds))
                .First().Key;

            topDayOfWeek = ToTurkishDay(dayGroup);
        }

        // Kronotip ve yoğun zaman aralığı
        var (chronotype, chronotypeName, peakTimeOfDay) = DetermineChronotype(completedSessions);

        // En popüler tema
        var topTheme = "rain window";
        if (completedSessions.Count > 0)
        {
            var favTheme = completedSessions
                .Where(s => !string.IsNullOrEmpty(s.ThemeId))
                .GroupBy(s => s.ThemeId!)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(favTheme))
                topTheme = favTheme;
        }

        // Seri bilgisi
        var streak = await _context.UserStreaks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        var currentStreak = streak?.CurrentStreak ?? 0;
        var longestStreak = streak?.LongestStreak ?? 0;

        // İlham verici özet
        var summaryInsight = GenerateSummaryInsight(period, totalFocusMinutes, chronotypeName, topDayOfWeek);

        return new FocusWrappedDto(
            period,
            startDate,
            endDate,
            totalFocusMinutes,
            completedCount,
            completionRate,
            topDayOfWeek,
            peakTimeOfDay,
            chronotype,
            chronotypeName,
            topTheme,
            coinsEarned,
            xpEarned,
            currentStreak,
            longestStreak,
            summaryInsight
        );
    }

    private static (Chronotype chronotype, string name, string peakTime) DetermineChronotype(List<Domain.Entities.FocusSession> sessions)
    {
        if (sessions.Count == 0)
            return (Chronotype.Balanced, "Dengeli Ritim", "14:00 - 18:00");

        int morning = 0, afternoon = 0, evening = 0, night = 0;

        foreach (var s in sessions)
        {
            var hour = s.StartedAt.Hour;
            if (hour >= 6 && hour < 12) morning++;
            else if (hour >= 12 && hour < 18) afternoon++;
            else if (hour >= 18 && hour < 24) evening++;
            else night++;
        }

        int max = Math.Max(Math.Max(morning, afternoon), Math.Max(evening, night));

        if (max == morning)
            return (Chronotype.MorningLark, "Erken Kalkan", "08:00 - 12:00");
        if (max == afternoon)
            return (Chronotype.DayAchiever, "Gündüz Odakçısı", "13:00 - 17:00");
        if (max == evening)
            return (Chronotype.EveningFlow, "Akşam Ritmi", "19:00 - 23:00");
        if (max == night)
            return (Chronotype.NightOwl, "Gece Baykuşu", "00:00 - 04:00");

        return (Chronotype.Balanced, "Dengeli Ritim", "10:00 - 16:00");
    }

    private static string ToTurkishDay(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Pazartesi",
        DayOfWeek.Tuesday => "Salı",
        DayOfWeek.Wednesday => "Çarşamba",
        DayOfWeek.Thursday => "Perşembe",
        DayOfWeek.Friday => "Cuma",
        DayOfWeek.Saturday => "Cumartesi",
        DayOfWeek.Sunday => "Pazar",
        _ => "Pazartesi"
    };

    private static string GenerateSummaryInsight(string period, int minutes, string chronotype, string topDay)
    {
        var hours = (minutes / 60.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        var periodName = period == "Weekly" ? "Bu hafta" : "Bu ay";

        if (minutes >= 600)
            return $"{periodName} masada tam {hours} saat geçirdin! {topDay} günleri zirve performansa ulaştın ve gerçek bir {chronotype} gibi parladın.";
        if (minutes > 0)
            return $"{periodName} {hours} saatlik odaklanma ritmi yakaladın. En aktif olduğun gün {topDay} oldu.";

        return $"{periodName} yeni seanslar başlatarak çalışma odanın enerjisini ve ritmini yükseltebilirsin.";
    }
}
