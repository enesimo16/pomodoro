using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Wrapped.DTOs;
using Focus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Focus.Infrastructure.Services;

public class FocusWrappedService : IFocusWrappedService
{
    private readonly IApplicationDbContext _context;
    private readonly TimeProvider _clock;

    public FocusWrappedService(IApplicationDbContext context, TimeProvider clock)
    {
        _context = context;
        _clock = clock;
    }

    public Task<FocusWrappedDto> GetWeeklyWrappedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var endDate = _clock.GetUtcNow().UtcDateTime;
        var startDate = endDate.AddDays(-7);
        return BuildWrappedAsync(userId, "Weekly", startDate, endDate, cancellationToken);
    }

    public Task<FocusWrappedDto> GetMonthlyWrappedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var endDate = _clock.GetUtcNow().UtcDateTime;
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
        var focusSessions = await _context.FocusSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId
                && s.Kind == SessionKind.Focus
                && s.StartedAt >= startDate
                && s.StartedAt <= endDate)
            .ToListAsync(cancellationToken);

        var completedSessions = focusSessions
            .Where(s => s.Status == SessionStatus.Completed)
            .ToList();

        // Hala suren (Running/Paused) seanslar tamamlanma oranini dusurmesin
        var finishedSessions = focusSessions
            .Where(s => s.Status == SessionStatus.Completed || s.Status == SessionStatus.Abandoned)
            .ToList();

        var completedCount = completedSessions.Count;
        double? completionRate = finishedSessions.Count > 0
            ? Math.Round((double)completedCount / finishedSessions.Count * 100, 1)
            : null;
        var totalFocusMinutes = completedSessions.Sum(s => s.NetDurationSeconds) / 60;
        var coinsEarned = finishedSessions.Sum(s => s.CoinsEarned);
        var xpEarned = finishedSessions.Sum(s => s.XpEarned);

        string? topDayOfWeek = null;
        string? topTheme = null;
        Chronotype? chronotype = null;
        string? chronotypeName = null;
        string? peakTimeOfDay = null;

        if (completedCount > 0)
        {
            var topDay = completedSessions
                .GroupBy(s => s.StartedAt.DayOfWeek)
                .OrderByDescending(g => g.Sum(s => s.NetDurationSeconds))
                .ThenBy(g => g.Key)
                .First().Key;
            topDayOfWeek = ToTurkishDay(topDay);

            (var detectedChronotype, chronotypeName, peakTimeOfDay) = DetermineChronotype(completedSessions);
            chronotype = detectedChronotype;

            topTheme = completedSessions
                .Where(s => !string.IsNullOrEmpty(s.ThemeId))
                .GroupBy(s => s.ThemeId!)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => g.Key)
                .FirstOrDefault();
        }

        var streak = await _context.UserStreaks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

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
            streak?.CurrentStreak ?? 0,
            streak?.LongestStreak ?? 0,
            summaryInsight
        );
    }

    private static (Chronotype chronotype, string name, string peakTime) DetermineChronotype(List<Domain.Entities.FocusSession> sessions)
    {
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

        return (Chronotype.NightOwl, "Gece Baykuşu", "00:00 - 04:00");
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
        _ => throw new ArgumentOutOfRangeException(nameof(day))
    };

    private static string GenerateSummaryInsight(string period, int minutes, string? chronotype, string? topDay)
    {
        var hours = (minutes / 60.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        var periodName = period == "Weekly" ? "Bu hafta" : "Bu ay";

        if (minutes >= 600 && chronotype != null && topDay != null)
            return $"{periodName} masada tam {hours} saat geçirdin! {topDay} günleri zirve performansa ulaştın ve gerçek bir {chronotype} gibi parladın.";
        if (minutes > 0 && topDay != null)
            return $"{periodName} {hours} saatlik odaklanma ritmi yakaladın. En aktif olduğun gün {topDay} oldu.";

        return $"{periodName} henüz tamamlanmış bir odak seansın yok. Yeni bir seans başlatarak ritmini yakalayabilirsin.";
    }
}
