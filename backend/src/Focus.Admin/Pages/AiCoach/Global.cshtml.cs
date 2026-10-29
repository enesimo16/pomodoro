using Focus.Admin.Common;
using Focus.Application.Common.Interfaces;
using Focus.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Focus.Admin.Pages.AiCoach;

public class GlobalModel : PageModel
{
    private readonly IApplicationDbContext _context;

    public const int SessionSampleSize = 1000;

    public GlobalModel(IApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? CategoryFilter { get; set; }

    // KPI Metrikleri
    public int TotalGlobalMemories { get; set; }
    public int TotalPlatformSessions { get; set; }
    public double GlobalCompletionRate { get; set; }
    public int TotalPersonalMemories { get; set; }
    public int SampleSessionCount { get; set; }

    // Kolektif Sentez İstatistikleri
    public List<DurationStatItem> DurationStats { get; set; } = new();
    public List<CircadianStatItem> CircadianStats { get; set; } = new();
    public List<CollectiveInsightItem> CollectiveInsights { get; set; } = new();

    // Global Hafıza Listesi
    public List<GlobalMemoryItem> Memories { get; set; } = new();

    public record DurationStatItem(string Label, int SessionCount, double CompletionRate, double AbandonRate);

    public record CircadianStatItem(string TimeSlot, int SessionCount, double Percentage, double CompletionRate);

    public record CollectiveInsightItem(string Title, string Finding, string Category, string Reliability);

    public record GlobalMemoryItem(
        Guid Id,
        MemoryCategory Category,
        string Content,
        int Importance,
        int UsageCount,
        DateTime CreatedAt);

    public async Task OnGetAsync()
    {
        TotalGlobalMemories = await _context.AgentMemories.CountAsync(m => m.UserId == null);
        TotalPersonalMemories = await _context.AgentMemories.CountAsync(m => m.UserId != null);

        var focusSessions = _context.FocusSessions.Where(s => s.Kind == SessionKind.Focus);
        TotalPlatformSessions = await focusSessions.CountAsync();

        var completedSessions = await focusSessions.CountAsync(s => s.Status == SessionStatus.Completed);
        GlobalCompletionRate = TotalPlatformSessions > 0
            ? (completedSessions * 100.0 / TotalPlatformSessions)
            : 0;

        // 1. Global Vektör Bellek Sorgusu
        var memQuery = _context.AgentMemories
            .Where(m => m.UserId == null)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(CategoryFilter) && Enum.TryParse<MemoryCategory>(CategoryFilter, true, out var cat))
        {
            memQuery = memQuery.Where(m => m.Category == cat);
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var pattern = $"%{Search.Trim()}%";
            memQuery = memQuery.Where(m => EF.Functions.ILike(m.Content, pattern));
        }

        Memories = await memQuery
            .OrderByDescending(m => m.Importance)
            .ThenByDescending(m => m.CreatedAt)
            .Take(100)
            .Select(m => new GlobalMemoryItem(
                m.Id,
                m.Category,
                m.Content,
                m.Importance,
                m.UsageCount,
                m.CreatedAt))
            .ToListAsync();

        // 2. Son odak seanslarından örneklem
        var sample = await focusSessions
            .OrderByDescending(s => s.StartedAt)
            .Select(s => new SessionSample(s.FocusMinutes, s.Status, s.StartedAt))
            .Take(SessionSampleSize)
            .ToListAsync();

        SampleSessionCount = sample.Count;
        DurationStats = BuildDurationStats(sample);
        CircadianStats = BuildCircadianStats(sample);
        CollectiveInsights = BuildCollectiveInsights(sample.Count, DurationStats, CircadianStats, Memories);
    }

    public record SessionSample(int FocusMinutes, SessionStatus Status, DateTime StartedAt);

    public static List<DurationStatItem> BuildDurationStats(IReadOnlyCollection<SessionSample> sessions)
    {
        var buckets = new (string Label, Func<SessionSample, bool> Match)[]
        {
            ("Standart Pomodoro (15-25 dk)", s => s.FocusMinutes <= 25),
            ("Derin Seans (26-45 dk)", s => s.FocusMinutes > 25 && s.FocusMinutes <= 45),
            ("Ultra Odak (46+ dk)", s => s.FocusMinutes > 45)
        };

        var result = new List<DurationStatItem>();
        foreach (var (label, match) in buckets)
        {
            var group = sessions.Where(match).ToList();
            if (group.Count == 0) continue;

            result.Add(new DurationStatItem(
                label,
                group.Count,
                group.Count(s => s.Status == SessionStatus.Completed) * 100.0 / group.Count,
                group.Count(s => s.Status == SessionStatus.Abandoned) * 100.0 / group.Count));
        }

        return result;
    }

    public static List<CircadianStatItem> BuildCircadianStats(IReadOnlyCollection<SessionSample> sessions)
    {
        if (sessions.Count == 0) return new List<CircadianStatItem>();

        var slots = new (string Label, int From, int To)[]
        {
            ("Sabah (06:00 - 12:00)", 6, 12),
            ("Öğleden Sonra (12:00 - 18:00)", 12, 18),
            ("Akşam (18:00 - 24:00)", 18, 24),
            ("Gece (00:00 - 06:00)", 0, 6)
        };

        var local = sessions.Select(s => (Hour: LocalTime.FromUtc(s.StartedAt).Hour, s.Status)).ToList();
        var result = new List<CircadianStatItem>();
        foreach (var (label, from, to) in slots)
        {
            var group = local.Where(x => x.Hour >= from && x.Hour < to).ToList();
            var completion = group.Count > 0
                ? group.Count(x => x.Status == SessionStatus.Completed) * 100.0 / group.Count
                : 0;
            result.Add(new CircadianStatItem(label, group.Count, group.Count * 100.0 / local.Count, completion));
        }

        return result;
    }

    public static List<CollectiveInsightItem> BuildCollectiveInsights(
        int sampleSize,
        IReadOnlyList<DurationStatItem> durations,
        IReadOnlyList<CircadianStatItem> circadian,
        IReadOnlyList<GlobalMemoryItem> memories)
    {
        var list = new List<CollectiveInsightItem>();
        if (sampleSize == 0) return list;

        var reliability = sampleSize switch
        {
            < 30 => "Düşük (az veri)",
            < 200 => "Orta",
            _ => "Yüksek"
        };

        var bestDuration = durations.OrderByDescending(d => d.CompletionRate).ThenByDescending(d => d.SessionCount).FirstOrDefault();
        if (bestDuration is not null)
        {
            list.Add(new CollectiveInsightItem(
                "En Yüksek Tamamlanan Seans Süresi",
                $"{bestDuration.Label} diliminde {bestDuration.SessionCount} seansın %{bestDuration.CompletionRate:F1} kadarı tamamlandı.",
                "Seans Mimari",
                reliability));
        }

        var busiestSlot = circadian.OrderByDescending(c => c.SessionCount).FirstOrDefault();
        if (busiestSlot is not null && busiestSlot.SessionCount > 0)
        {
            list.Add(new CollectiveInsightItem(
                "En Yoğun Odak Zamanı",
                $"Seansların %{busiestSlot.Percentage:F1} kadarı {busiestSlot.TimeSlot} diliminde başladı (İstanbul saati).",
                "Sirkadiyen Ritim",
                reliability));
        }

        var abandonedCount = durations.Sum(d => d.AbandonRate * d.SessionCount / 100.0);
        list.Add(new CollectiveInsightItem(
            "Genel Terk Oranı",
            $"Son {sampleSize} odak seansının %{abandonedCount * 100.0 / sampleSize:F1} kadarı yarıda bırakıldı.",
            "Bilişsel Yorgunluk",
            reliability));

        var topCategory = memories.GroupBy(m => m.Category).OrderByDescending(g => g.Count()).FirstOrDefault();
        if (topCategory is not null)
        {
            list.Add(new CollectiveInsightItem(
                "Baskın Hafıza Kategorisi",
                $"Listelenen global hafıza kayıtlarının {topCategory.Count()} tanesi {topCategory.Key} kategorisinde.",
                "Kolektif Hafıza",
                reliability));
        }

        return list;
    }
}
