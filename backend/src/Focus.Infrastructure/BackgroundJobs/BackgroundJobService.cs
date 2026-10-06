using Focus.Application.Common.Interfaces;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Focus.Infrastructure.BackgroundJobs;

public class BackgroundJobService : IBackgroundJobService
{
    private readonly IApplicationDbContext _context;
    private readonly IVectorMemoryService _vectorMemoryService;
    private readonly ILogger<BackgroundJobService> _logger;

    public BackgroundJobService(
        IApplicationDbContext context,
        IVectorMemoryService vectorMemoryService,
        ILogger<BackgroundJobService> logger)
    {
        _context = context;
        _vectorMemoryService = vectorMemoryService;
        _logger = logger;
    }

    public async Task<int> RunDailyStreakMaintenanceAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var streaks = await _context.UserStreaks
            .Include(s => s.User)
            .Where(s => s.CurrentStreak > 0)
            .ToListAsync(cancellationToken);

        int updatedCount = 0;

        foreach (var streak in streaks)
        {
            var (freezeUsed, reset) = streak.ApplyOvernightFreezeOrReset(now);
            if (freezeUsed)
            {
                var notif = Notification.Create(
                    streak.UserId,
                    NotificationType.System,
                    "Seri Dondurucu Devreye Girdi",
                    $"Dün odaklanamadınız, fakat seriniz dondurucu hakkınız ile korundu! Kalan hak: {streak.FreezesAvailable}.");
                _context.Notifications.Add(notif);
                updatedCount++;
            }
            else if (reset)
            {
                var notif = Notification.Create(
                    streak.UserId,
                    NotificationType.System,
                    "Günlük Seri Sıfırlandı",
                    "Dün odaklanma seansı gerçekleştirilemediği için seriniz sıfırlandı. Bugün yeni bir seansla tekrar başlayabilirsiniz!");
                _context.Notifications.Add(notif);
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Daily streak maintenance completed. Evaluated: {Total}, Updated: {Updated}", streaks.Count, updatedCount);
        return updatedCount;
    }

    public async Task<int> RunWeeklyStreakFreezeRefreshAsync(CancellationToken cancellationToken = default)
    {
        var streaks = await _context.UserStreaks
            .Where(s => s.FreezesAvailable < 2)
            .ToListAsync(cancellationToken);

        int refreshedCount = 0;

        foreach (var streak in streaks)
        {
            if (streak.AddFreeze(1))
            {
                var notif = Notification.Create(
                    streak.UserId,
                    NotificationType.System,
                    "Haftalık Seri Dondurucu Yenilendi",
                    $"Yeni hafta başladı! Hesabınıza 1 adet Seri Dondurucu (Freeze) eklendi. Toplam hak: {streak.FreezesAvailable}.");
                _context.Notifications.Add(notif);
                refreshedCount++;
            }
        }

        if (refreshedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Weekly streak freeze refresh completed. Refreshed: {Count}", refreshedCount);
        return refreshedCount;
    }

    public async Task<int> RunAbandonedSessionReconciliationAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddHours(-3);
        var staleSessions = await _context.FocusSessions
            .Where(s => (s.Status == SessionStatus.Running || s.Status == SessionStatus.Paused) && s.StartedAt < cutoff)
            .ToListAsync(cancellationToken);

        if (staleSessions.Count == 0)
        {
            return 0;
        }

        foreach (var session in staleSessions)
        {
            session.Abandon(session.NetDurationSeconds);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Abandoned session reconciliation completed. Reconciled: {Count} sessions", staleSessions.Count);
        return staleSessions.Count;
    }

    public async Task<int> RunGuestAndAuditCleanupAsync(CancellationToken cancellationToken = default)
    {
        var auditCutoff = DateTime.UtcNow.AddDays(-60);
        var staleAuditSessions = await _context.UserDeviceSessions
            .Where(s => s.LastSeenAt < auditCutoff)
            .ToListAsync(cancellationToken);

        int deletedCount = staleAuditSessions.Count;
        if (staleAuditSessions.Count > 0)
        {
            _context.UserDeviceSessions.RemoveRange(staleAuditSessions);
        }

        var guestCutoff = DateTime.UtcNow.AddDays(-30);
        var inactiveGuests = await _context.Users
            .Where(u => u.IsGuest && u.CreatedAt < guestCutoff && !_context.FocusSessions.Any(s => s.UserId == u.Id))
            .Take(100)
            .ToListAsync(cancellationToken);

        if (inactiveGuests.Count > 0)
        {
            _context.Users.RemoveRange(inactiveGuests);
            deletedCount += inactiveGuests.Count;
        }

        if (deletedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Guest and audit cleanup completed. Cleaned {Count} stale records", deletedCount);
        return deletedCount;
    }

    public async Task<int> RunGlobalAgentMemoryAggregationAsync(CancellationToken cancellationToken = default)
    {
        var yesterday = DateTime.UtcNow.AddDays(-1);
        var recentReflections = await _context.SessionReflections
            .Where(r => r.CreatedAt >= yesterday && !string.IsNullOrWhiteSpace(r.DistractionNote))
            .Take(50)
            .ToListAsync(cancellationToken);

        if (recentReflections.Count == 0)
        {
            return 0;
        }

        var lowFatigueHighRating = recentReflections
            .Where(r => r.FocusQuality >= 4)
            .ToList();

        int aggregatedCount = 0;

        if (lowFatigueHighRating.Count >= 3)
        {
            var content = "Kullanıcılar mola ritmine sadık kaldıklarında ve seans öncesi niyet belirlediklerinde odak kalitesi en yüksek seviyeye çıkıyor.";
            var exists = await _context.AgentMemories.AnyAsync(m => m.IsGlobal && m.Content == content, cancellationToken);
            if (!exists)
            {
                var embedding = await _vectorMemoryService.GenerateEmbeddingAsync(content, cancellationToken);
                var memory = AgentMemory.CreateGlobal(MemoryCategory.ProductivityPattern, content, embedding, 4);
                _context.AgentMemories.Add(memory);
                aggregatedCount++;
            }
        }

        var highFatiguePatterns = recentReflections
            .Where(r => r.FocusQuality <= 2)
            .ToList();

        if (highFatiguePatterns.Count >= 3)
        {
            var content = "Kesintisiz 90 dakikayı aşan seanslar yorgunluk indeksini kritik eşiğe çıkarıyor; mola zorunlu tutulmalıdır.";
            var exists = await _context.AgentMemories.AnyAsync(m => m.IsGlobal && m.Content == content, cancellationToken);
            if (!exists)
            {
                var embedding = await _vectorMemoryService.GenerateEmbeddingAsync(content, cancellationToken);
                var memory = AgentMemory.CreateGlobal(MemoryCategory.FatigueRecovery, content, embedding, 5);
                _context.AgentMemories.Add(memory);
                aggregatedCount++;
            }
        }

        if (aggregatedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Global agent memory aggregation completed. Aggregated: {Count} memories", aggregatedCount);
        return aggregatedCount;
    }
}
