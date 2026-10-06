using Focus.Application.Common.Interfaces;
using Hangfire;

namespace Focus.Infrastructure.BackgroundJobs;

public record RecurringJobDefinition(
    string Id,
    string Title,
    string Description,
    string CronExpression,
    string ScheduleDescription);

public static class RecurringJobsConfigurator
{
    public static readonly IReadOnlyList<RecurringJobDefinition> RegisteredJobs = new List<RecurringJobDefinition>
    {
        new(
            "daily-streak-maintenance",
            "Günlük Seri & Dondurucu Denetimi",
            "Dün seans tamamlamayan kullanıcıların serilerini kontrol eder; dondurucu (freeze) hakkı varsa düşer, yoksa seriyi sıfırlar.",
            "5 0 * * *",
            "Her gece 00:05 UTC"),
        new(
            "weekly-freeze-refresh",
            "Haftalık Dondurucu (Freeze) Yenileme",
            "Tüm kullanıcıların haftalık dondurucu haklarını (Free: 1, Pro: 2) yeniler ve bilgilendirme bildirimi gönderir.",
            "0 0 * * 1",
            "Her Pazartesi 00:00 UTC"),
        new(
            "abandoned-session-reconciliation",
            "Askıda Kalan Seans Uzlaşması",
            "3 saati aşkın süredir güncellenmeyen ve açık unutulmuş aktif veya duraklatılmış seansları terk edildi olarak kapatır.",
            "0 * * * *",
            "Her saat başı"),
        new(
            "guest-audit-cleanup",
            "Eski Misafir & Erişim Logu Temizliği",
            "30 günden eski seanssız misafir hesaplarını ve 60 günden eski IP/cihaz denetim kayıtlarını temizler.",
            "0 3 * * 0",
            "Her Pazar 03:00 UTC"),
        new(
            "global-memory-aggregation",
            "Kolektif Global Yapay Zeka Bellek Sentezi",
            "Son 24 saatteki seans geri bildirimlerini anonimleştirerek ortak AI koç modelinin global pgvector hafızasına aktarır.",
            "0 1 * * *",
            "Her gece 01:00 UTC")
    };

    public static void ConfigureRecurringJobs(IRecurringJobManager recurringJobManager)
    {
        recurringJobManager.AddOrUpdate<IBackgroundJobService>(
            "daily-streak-maintenance",
            service => service.RunDailyStreakMaintenanceAsync(CancellationToken.None),
            "5 0 * * *");

        recurringJobManager.AddOrUpdate<IBackgroundJobService>(
            "weekly-freeze-refresh",
            service => service.RunWeeklyStreakFreezeRefreshAsync(CancellationToken.None),
            "0 0 * * 1");

        recurringJobManager.AddOrUpdate<IBackgroundJobService>(
            "abandoned-session-reconciliation",
            service => service.RunAbandonedSessionReconciliationAsync(CancellationToken.None),
            "0 * * * *");

        recurringJobManager.AddOrUpdate<IBackgroundJobService>(
            "guest-audit-cleanup",
            service => service.RunGuestAndAuditCleanupAsync(CancellationToken.None),
            "0 3 * * 0");

        recurringJobManager.AddOrUpdate<IBackgroundJobService>(
            "global-memory-aggregation",
            service => service.RunGlobalAgentMemoryAggregationAsync(CancellationToken.None),
            "0 1 * * *");
    }
}
