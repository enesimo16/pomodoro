using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Coach.DTOs;
using Focus.Domain.Entities;
using Focus.Domain.Enums;

namespace Focus.Infrastructure.Services;

public class FocusCoachEngine : IFocusCoachEngine
{
    private const double WeightContinuous = 0.35;
    private const double WeightSkippedBreaks = 0.25;
    private const double WeightTotalFocus = 0.25;
    private const double WeightNightWork = 0.15;

    public CoachAnomalyStatusDto EvaluateAnomalies(
        User user,
        List<FocusSession> recentSessions,
        List<SessionCheckIn> recentCheckIns,
        List<SessionReflection> recentReflections,
        UserStreak? streak)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        var todaySessions = recentSessions
            .Where(s => s.StartedAt.Date == today)
            .ToList();

        var dailyFatigue = CalculateDailyFatigueScore(todaySessions);
        var weeklyMinutes = CalculateWeeklyTotalMinutes(recentSessions);

        var lastSession = recentSessions.OrderByDescending(s => s.StartedAt).FirstOrDefault();
        var daysSinceLastSession = lastSession == null
            ? 99
            : (int)(now - lastSession.StartedAt).TotalDays;

        // 1. Yokluk / Geri Donus Anomalisi (3+ gun seans yapilmadiysa)
        if (daysSinceLastSession >= 3 && lastSession != null)
        {
            return new CoachAnomalyStatusDto(
                HasAnomaly: true,
                AnomalyType: AnomalyType.AbsenceReturn,
                DailyFatigueScore: dailyFatigue,
                WeeklyFocusMinutes: weeklyMinutes,
                DaysSinceLastSession: daysSinceLastSession,
                StatusSummary: "Geri donus algilandi. Masan ve odan seni bekliyordu.",
                RecommendedAction: "Zihinsel surtunmeyi kirmak ve isinmak icin 15-20 dakikalik hafif bir seans yapin.");
        }

        // 2. Haftalik Asiri Yuklenme & Tukenmislik Kalkanı (Haftalik 35+ saat / 2100 dk)
        if (weeklyMinutes >= 2100)
        {
            return new CoachAnomalyStatusDto(
                HasAnomaly: true,
                AnomalyType: AnomalyType.WeeklyExcessiveLoad,
                DailyFatigueScore: dailyFatigue,
                WeeklyFocusMinutes: weeklyMinutes,
                DaysSinceLastSession: daysSinceLastSession,
                StatusSummary: $"Haftalik yuksek disiplin ({weeklyMinutes / 60} saat). Aktif toparlanma fazina gecis onerilir.",
                RecommendedAction: "Aktif toparlanma saglamak ve zihnin ogrenilenleri kalici hafizaya islemesi icin arkadas odalarinda ortak calisin, piksel carsiyi gezin veya acik havada yuruyun.");
        }

        // 3. Gunluk Yuksek Yorgunluk Skoru (>= 70)
        if (dailyFatigue >= 70)
        {
            return new CoachAnomalyStatusDto(
                HasAnomaly: true,
                AnomalyType: AnomalyType.HighDailyFatigue,
                DailyFatigueScore: dailyFatigue,
                WeeklyFocusMinutes: weeklyMinutes,
                DaysSinceLastSession: daysSinceLastSession,
                StatusSummary: $"Gunluk zihinsel yorgunluk esigi asildi (Skor: {dailyFatigue:F0}/100).",
                RecommendedAction: "15 dakikalik su, esneme ve goz dinlendirme molasi verin. Ekran parlakligini kisabilirsiniz.");
        }

        // 4. Duygusal Stres / Surekli Dusuk Odak Kalitesi
        var recentBadReflections = recentReflections.Take(3).Count(r => r.FocusQuality <= 2 || r.MoodAfter == SessionMoodAfter.Exhausted || r.MoodAfter == SessionMoodAfter.Frustrated);
        var recentStressedCheckIns = recentCheckIns.Take(3).Count(c => c.Mood == SessionMood.Stressed || c.Mood == SessionMood.Tired);

        if (recentBadReflections >= 2 || recentStressedCheckIns >= 2)
        {
            return new CoachAnomalyStatusDto(
                HasAnomaly: true,
                AnomalyType: AnomalyType.EmotionalStress,
                DailyFatigueScore: dailyFatigue,
                WeeklyFocusMinutes: weeklyMinutes,
                DaysSinceLastSession: daysSinceLastSession,
                StatusSummary: "Zihinsel stres ve odak dalgalanmasi gozlemlendi.",
                RecommendedAction: "Gorevi mikro parcalara bolun. Derin nefes egzersizi veya Lo-Fi yagmur ambiyansiyla baslayin.");
        }

        // 5. Dengeli Durum
        return new CoachAnomalyStatusDto(
            HasAnomaly: false,
            AnomalyType: AnomalyType.None,
            DailyFatigueScore: dailyFatigue,
            WeeklyFocusMinutes: weeklyMinutes,
            DaysSinceLastSession: daysSinceLastSession,
            StatusSummary: "Odak ritmi dengeli ve surdurulebilir.",
            RecommendedAction: "Mevcut calisma ve mola dongunuzu koruyarak devam edebilirsiniz.");
    }

    public double CalculateDailyFatigueScore(List<FocusSession> todaySessions)
    {
        if (todaySessions == null || todaySessions.Count == 0) return 0;

        double maxContinuous = 0;
        double totalFocusMinutes = 0;
        int skippedBreaks = 0;
        double nightMinutes = 0;

        foreach (var s in todaySessions)
        {
            var durationMin = s.NetDurationSeconds > 0 ? s.NetDurationSeconds / 60.0 : s.FocusMinutes;
            totalFocusMinutes += durationMin;

            if (durationMin > maxContinuous)
            {
                maxContinuous = durationMin;
            }

            // Gece 00:00 - 05:00 arasi seans orani
            if (s.StartedAt.Hour >= 0 && s.StartedAt.Hour < 5)
            {
                nightMinutes += durationMin;
            }

            if (s.Status == SessionStatus.Abandoned)
            {
                skippedBreaks++;
            }
        }

        var continuousRatio = Math.Min(maxContinuous / 120.0, 1.0);
        var skippedRatio = Math.Min(skippedBreaks / 4.0, 1.0);
        var totalRatio = Math.Min(totalFocusMinutes / 360.0, 1.0);
        var nightRatio = totalFocusMinutes > 0 ? Math.Min(nightMinutes / totalFocusMinutes, 1.0) : 0;

        var rawScore = (WeightContinuous * continuousRatio) +
                       (WeightSkippedBreaks * skippedRatio) +
                       (WeightTotalFocus * totalRatio) +
                       (WeightNightWork * nightRatio);

        return Math.Round(Math.Clamp(rawScore * 100.0, 0.0, 100.0), 1);
    }

    public int CalculateWeeklyTotalMinutes(List<FocusSession> past7DaysSessions)
    {
        if (past7DaysSessions == null || past7DaysSessions.Count == 0) return 0;

        var weekAgo = DateTime.UtcNow.AddDays(-7);
        return (int)past7DaysSessions
            .Where(s => s.StartedAt >= weekAgo && s.Status == SessionStatus.Completed)
            .Sum(s => s.NetDurationSeconds > 0 ? s.NetDurationSeconds / 60.0 : s.FocusMinutes);
    }
}
