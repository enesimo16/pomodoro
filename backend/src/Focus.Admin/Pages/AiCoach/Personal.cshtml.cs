using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Coach.DTOs;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Focus.Admin.Pages.AiCoach;

public class PersonalModel : PageModel
{
    private readonly IApplicationDbContext _context;
    private readonly IFocusCoachEngine _coachEngine;

    public PersonalModel(IApplicationDbContext context, IFocusCoachEngine coachEngine)
    {
        _context = context;
        _coachEngine = coachEngine;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? SelectedUserId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    // Kullanıcı Listesi
    public List<UserOption> UserOptions { get; set; } = new();
    public User? CurrentUser { get; set; }

    // KPI Metrikleri
    public int TotalPersonalMemories { get; set; }
    public double AverageFocusQuality { get; set; }
    public int TotalReflections { get; set; }
    public int TotalCheckIns { get; set; }

    // AI Koçluk ve Anomali Durumu
    public CoachAnomalyStatusDto? AnomalyStatus { get; set; }

    // Kişisel Sentez Çıkarımları
    public List<PersonalInsightItem> SynthesizedInsights { get; set; } = new();

    // Vektör Bellek Listesi
    public List<PersonalMemoryItem> Memories { get; set; } = new();

    // Seans Sonu Yansımaları
    public List<ReflectionItem> RecentReflections { get; set; } = new();

    public record UserOption(Guid Id, string DisplayName, string Email, int Level, int MemoryCount);

    public record PersonalInsightItem(string Title, string Description, string Tag, string Importance);

    public record PersonalMemoryItem(
        Guid Id,
        MemoryCategory Category,
        string Content,
        int Importance,
        int UsageCount,
        DateTime CreatedAt);

    public record ReflectionItem(
        Guid SessionId,
        DateTime Date,
        int Quality,
        SessionMoodAfter MoodAfter,
        string? DistractionNote);

    public async Task OnGetAsync()
    {
        // 1. Kullanıcı Listesini ve Kişisel Hafıza Sayılarını Getir
        var users = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(50)
            .ToListAsync();

        var memoryCounts = await _context.AgentMemories
            .Where(m => m.UserId != null)
            .GroupBy(m => m.UserId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.UserId, g => g.Count);

        UserOptions = users.Select(u => new UserOption(
            u.Id,
            u.DisplayName,
            u.Email ?? "Misafir",
            u.Level,
            memoryCounts.GetValueOrDefault(u.Id, 0))).ToList();

        if (users.Count == 0) return;

        // Seçili kullanıcıyı belirle (seçilmemişse en çok hafızası olan veya ilk kullanıcı)
        if (!SelectedUserId.HasValue || !users.Any(u => u.Id == SelectedUserId.Value))
        {
            var bestUser = UserOptions.OrderByDescending(u => u.MemoryCount).FirstOrDefault();
            SelectedUserId = bestUser?.Id ?? users[0].Id;
        }

        CurrentUser = users.FirstOrDefault(u => u.Id == SelectedUserId.Value);
        if (CurrentUser == null) return;

        var uid = CurrentUser.Id;

        // 2. Kişisel Vektör Bellekleri
        var memQuery = _context.AgentMemories
            .Where(m => m.UserId == uid)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var pattern = $"%{Search.Trim()}%";
            memQuery = memQuery.Where(m => EF.Functions.ILike(m.Content, pattern));
        }

        Memories = await memQuery
            .OrderByDescending(m => m.Importance)
            .ThenByDescending(m => m.CreatedAt)
            .Take(50)
            .Select(m => new PersonalMemoryItem(
                m.Id,
                m.Category,
                m.Content,
                m.Importance,
                m.UsageCount,
                m.CreatedAt))
            .ToListAsync();

        TotalPersonalMemories = await _context.AgentMemories.CountAsync(m => m.UserId == uid);

        // 3. Kullanıcı Seansları, Check-In ve Reflection Verileri
        var recentSessions = await _context.FocusSessions
            .Where(s => s.UserId == uid)
            .OrderByDescending(s => s.StartedAt)
            .Take(30)
            .ToListAsync();

        var recentCheckIns = await _context.SessionCheckIns
            .Where(c => c.UserId == uid)
            .OrderByDescending(c => c.CreatedAt)
            .Take(30)
            .ToListAsync();

        var reflections = await _context.SessionReflections
            .Where(r => r.UserId == uid)
            .OrderByDescending(r => r.CreatedAt)
            .Take(30)
            .ToListAsync();

        var streak = await _context.UserStreaks
            .FirstOrDefaultAsync(s => s.UserId == uid);

        TotalReflections = reflections.Count;
        TotalCheckIns = recentCheckIns.Count;
        AverageFocusQuality = reflections.Count > 0
            ? reflections.Average(r => r.FocusQuality)
            : 0;

        RecentReflections = reflections.Take(10).Select(r => new ReflectionItem(
            r.SessionId,
            r.CreatedAt,
            r.FocusQuality,
            r.MoodAfter,
            r.DistractionNote)).ToList();

        // 4. Kural Motoruyla Anomali & Yorgunluk Tanısı
        AnomalyStatus = _coachEngine.EvaluateAnomalies(CurrentUser, recentSessions, recentCheckIns, reflections, streak);

        // 5. Kişisel Derin Sentez Çıkarımları (Synthesis Engine)
        SynthesizedInsights = GeneratePersonalInsights(CurrentUser, recentSessions, recentCheckIns, reflections, streak, Memories);
    }

    private static List<PersonalInsightItem> GeneratePersonalInsights(
        User user,
        List<FocusSession> sessions,
        List<SessionCheckIn> checkIns,
        List<SessionReflection> reflections,
        UserStreak? streak,
        List<PersonalMemoryItem> memories)
    {
        var insights = new List<PersonalInsightItem>();

        // Çıkarım 1: En Verimli Çalışma Saatleri
        if (sessions.Count > 0)
        {
            var completed = sessions.Where(s => s.Status == SessionStatus.Completed).ToList();
            if (completed.Count > 0)
            {
                var hourGroups = completed
                    .GroupBy(s => s.StartedAt.Hour)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();

                if (hourGroups != null)
                {
                    var hour = hourGroups.Key;
                    insights.Add(new PersonalInsightItem(
                        Title: "Zirve Odaklanma Penceresi",
                        Description: $"Kullanıcı en yüksek seans tamamlama başarısını saat {hour:00}:00 - {(hour + 2):00}:00 aralığında göstermektedir.",
                        Tag: "Sirkadiyen Ritim",
                        Importance: "Yüksek"));
                }
            }
        }

        // Çıkarım 2: Seans Süresi Dayanıklılığı
        if (sessions.Count >= 3)
        {
            var avgMinutes = sessions.Average(s => s.FocusMinutes);
            var abandonedCount = sessions.Count(s => s.Status == SessionStatus.Abandoned);
            var abandonRatio = abandonedCount / (double)sessions.Count;

            if (abandonRatio > 0.3)
            {
                insights.Add(new PersonalInsightItem(
                    Title: "Mikro Seans Önerisi",
                    Description: $"Terk edilen seans oranı %{(abandonRatio * 100):F0}. 45+ dk yerine 20-25 dakikalık seanslar zihinsel sürtünmeyi düşürecektir.",
                    Tag: "Bilişsel Yük",
                    Importance: "Kritik"));
            }
            else
            {
                insights.Add(new PersonalInsightItem(
                    Title: "Derin Odak İstikrarı",
                    Description: $"Ortalama seans süresi {avgMinutes:F0} dk ve tamamlama oranı güçlü. Akış kalkanı (Flow Shield) koruması yüksek.",
                    Tag: "Akış Durumu",
                    Importance: "Olumlu"));
            }
        }

        // Çıkarım 3: Duygusal Dönüşüm (Mood Shift)
        if (checkIns.Count > 0 && reflections.Count > 0)
        {
            var refreshedCount = reflections.Count(r => r.MoodAfter == SessionMoodAfter.Accomplished || r.MoodAfter == SessionMoodAfter.Refreshed);
            var ratio = refreshedCount / (double)reflections.Count;

            if (ratio >= 0.5)
            {
                insights.Add(new PersonalInsightItem(
                    Title: "Pozitif Bilişsel Ödül Döngüsü",
                    Description: $"Seansların %{(ratio * 100):F0}'i 'Başarmış' ve 'Tazelenmiş' ruh haliyle tamamlanıyor. Dopamin döngüsü sağlıklı.",
                    Tag: "Ruh Hali",
                    Importance: "Normal"));
            }
        }

        // Çıkarım 4: Streak Disiplini
        if (streak != null && streak.CurrentStreak > 0)
        {
            insights.Add(new PersonalInsightItem(
                Title: $"Kesintisiz Odak Serisi: {streak.CurrentStreak} Gün",
                Description: $"En uzun seri {streak.LongestStreak} gün. Kalan dondurucu token adedi: {streak.FreezesAvailable}.",
                Tag: "Alışkanlık",
                Importance: "Yüksek"));
        }

        if (insights.Count == 0)
        {
            insights.Add(new PersonalInsightItem(
                Title: "Gelişmekte Olan AI Profili",
                Description: "Kullanıcı daha fazla odak seansı tamamladıkça ve AI koç ile etkileşime girdikçe derin örüntüler otomatik sentezlenecektir.",
                Tag: "Öğrenme",
                Importance: "Bilgi"));
        }

        return insights;
    }
}
