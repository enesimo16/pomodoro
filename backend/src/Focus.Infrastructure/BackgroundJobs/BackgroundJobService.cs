using Focus.Application.Common.Exceptions;
using Focus.Application.Common.Extensions;
using Focus.Application.Common.Interfaces;
using Focus.Application.Common.Options;
using Focus.Application.Features.Session.Services;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Focus.Infrastructure.BackgroundJobs;

public class BackgroundJobService : IBackgroundJobService
{
    private readonly IApplicationDbContext _context;
    private readonly IVectorMemoryService _vectorMemoryService;
    private readonly ILogger<BackgroundJobService> _logger;
    private readonly SessionSettlementService _settlement;
    private readonly AlgorithmOptions _options;
    private readonly TimeProvider _clock;

    public BackgroundJobService(
        IApplicationDbContext context,
        IVectorMemoryService vectorMemoryService,
        SessionSettlementService settlement,
        IOptions<AlgorithmOptions> options,
        TimeProvider clock,
        ILogger<BackgroundJobService> logger)
    {
        _context = context;
        _vectorMemoryService = vectorMemoryService;
        _settlement = settlement;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    public async Task<int> RunDailyStreakMaintenanceAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
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

    // Suresi planlanandan (tolerans) uzun gecmis calisan seanslar planlanan sureyle tamamlanir, uzun suredir duraklatilmislar net sureyle terk edilir
    public async Task<int> RunAbandonedSessionReconciliationAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var runningCutoff = now.AddMinutes(-_options.StaleRunningGraceMinutes);
        var pausedCutoff = now.AddHours(-_options.StalePausedHours);

        var staleSessionIds = await _context.FocusSessions
            .AsNoTracking()
            .Where(s => (s.Status == SessionStatus.Running && s.PlannedEndAt < runningCutoff)
                || (s.Status == SessionStatus.Paused && s.PausedAt != null && s.PausedAt < pausedCutoff))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var reconciled = 0;
        foreach (var sessionId in staleSessionIds)
        {
            var session = await _context.FocusSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
            if (session == null || !session.IsActive) continue;

            try
            {
                if (session.Status == SessionStatus.Running)
                {
                    await _settlement.CompleteAsync(session, session.PlannedEndAt, cancellationToken);
                }
                else
                {
                    await _settlement.AbandonAsync(session, session.PausedAt!.Value, cancellationToken);
                }

                await _context.SaveChangesOrConflictAsync(cancellationToken, treatConstraintViolationAsConflict: true);
                reconciled++;
            }
            catch (ConflictException)
            {
                // Kullanici seansi ayni anda kapatti; bir sonraki calismada tekrar degerlendirilir
                _logger.LogInformation("Stale session {SessionId} was changed concurrently; skipping", sessionId);
            }
        }

        _logger.LogInformation("Stale session reconciliation completed. Reconciled: {Count} of {Total} sessions", reconciled, staleSessionIds.Count);
        return reconciled;
    }

    public async Task<int> RunStaleRoomMemberCleanupAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = _clock.GetUtcNow().UtcDateTime.AddMinutes(-_options.RoomMemberStaleMinutes);
        var rooms = await _context.StudyRooms
            .Include(r => r.Members)
            .Where(r => r.Members.Any(m => m.LastActiveAt != null && m.LastActiveAt < cutoff))
            .ToListAsync(cancellationToken);

        var removed = 0;
        foreach (var room in rooms)
        {
            var staleUserIds = room.Members
                .Where(m => m.LastActiveAt != null && m.LastActiveAt < cutoff)
                .Select(m => m.UserId)
                .ToList();

            foreach (var userId in staleUserIds)
            {
                var member = room.RemoveMember(userId);
                if (member == null) continue;

                _context.RoomMembers.Remove(member);
                removed++;
            }
        }

        if (removed == 0) return 0;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Uyeler ayni anda islem yapti veya ayrildi; bir sonraki calismada tekrar degerlendirilir
            _context.DiscardPendingChanges();
            _logger.LogInformation("Stale room member cleanup hit a concurrent change; will retry on the next run");
            return 0;
        }

        _logger.LogInformation("Stale room member cleanup completed. Removed: {Count} members", removed);
        return removed;
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
