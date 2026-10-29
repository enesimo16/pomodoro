using System.Data.Common;
using System.Diagnostics;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.AdminAnalytics.DTOs;
using Focus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Focus.Infrastructure.Services;

public class AdminAnalyticsService : IAdminAnalyticsService
{
    private const int TopListSize = 10;
    private const string RedisNotConfigured = "NotConfigured";

    private readonly IApplicationDbContext _context;

    public AdminAnalyticsService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PlatformOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekAgo = now.AddDays(-7);
        var monthAgo = now.AddDays(-30);

        var totalUsers = await _context.Users.CountAsync(cancellationToken);
        var guestUsers = await _context.Users.CountAsync(u => u.IsGuest, cancellationToken);
        var registeredUsers = totalUsers - guestUsers;

        var dau = await _context.FocusSessions
            .Where(s => s.StartedAt >= today)
            .Select(s => s.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        var wau = await _context.FocusSessions
            .Where(s => s.StartedAt >= weekAgo)
            .Select(s => s.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        var mau = await _context.FocusSessions
            .Where(s => s.StartedAt >= monthAgo)
            .Select(s => s.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        var totalSessions = await _context.FocusSessions.CountAsync(cancellationToken);
        var completedSessions = await _context.FocusSessions
            .CountAsync(s => s.Status == SessionStatus.Completed, cancellationToken);
        var abandonedSessions = await _context.FocusSessions
            .CountAsync(s => s.Status == SessionStatus.Abandoned, cancellationToken);

        var totalSeconds = await _context.FocusSessions
            .Where(s => s.Status == SessionStatus.Completed)
            .SumAsync(s => (long)s.NetDurationSeconds, cancellationToken);

        var totalFocusMinutes = totalSeconds / 60;
        var completionRate = totalSessions > 0
            ? Math.Round((double)completedSessions / totalSessions * 100, 1)
            : 0.0;

        return new PlatformOverviewDto(
            totalUsers,
            guestUsers,
            registeredUsers,
            dau,
            wau,
            mau,
            totalFocusMinutes,
            totalSessions,
            completedSessions,
            abandonedSessions,
            completionRate
        );
    }

    public async Task<UserAnalyticsDto> GetUserAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var totalAvatars = await _context.UserAvatars.CountAsync(cancellationToken);

        var levelDist = new Dictionary<string, int>
        {
            ["Sv. 1-5"] = await _context.Users.CountAsync(u => u.Level >= 1 && u.Level <= 5, cancellationToken),
            ["Sv. 6-10"] = await _context.Users.CountAsync(u => u.Level >= 6 && u.Level <= 10, cancellationToken),
            ["Sv. 11-20"] = await _context.Users.CountAsync(u => u.Level >= 11 && u.Level <= 20, cancellationToken),
            ["Sv. 21+"] = await _context.Users.CountAsync(u => u.Level >= 21, cancellationToken)
        };

        var topXpRows = await _context.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CurrentXp)
            .Take(TopListSize)
            .Select(u => new { u.Id, u.DisplayName, u.Level, u.CurrentXp })
            .ToListAsync(cancellationToken);

        var topStreakRows = await (
                from s in _context.UserStreaks.AsNoTracking()
                join u in _context.Users.AsNoTracking() on s.UserId equals u.Id
                orderby s.CurrentStreak descending, u.CurrentXp descending
                select new { u.Id, u.DisplayName, u.Level, u.CurrentXp, s.CurrentStreak })
            .Take(TopListSize)
            .ToListAsync(cancellationToken);

        var listedIds = topXpRows.Select(u => u.Id).Concat(topStreakRows.Select(u => u.Id)).Distinct().ToList();

        var streaks = await _context.UserStreaks
            .AsNoTracking()
            .Where(s => listedIds.Contains(s.UserId))
            .ToDictionaryAsync(s => s.UserId, s => s.CurrentStreak, cancellationToken);

        var coinSums = await _context.CoinLedgerEntries
            .AsNoTracking()
            .Where(c => listedIds.Contains(c.UserId))
            .GroupBy(c => c.UserId)
            .Select(g => new { UserId = g.Key, Coins = g.Sum(c => c.Coins) })
            .ToDictionaryAsync(x => x.UserId, x => x.Coins, cancellationToken);

        var topByXp = topXpRows
            .Select(u => new TopUserSummaryDto(u.Id, u.DisplayName, u.Level, u.CurrentXp, coinSums.GetValueOrDefault(u.Id, 0), streaks.GetValueOrDefault(u.Id, 0)))
            .ToList();

        var topByStreak = topStreakRows
            .Select(u => new TopUserSummaryDto(u.Id, u.DisplayName, u.Level, u.CurrentXp, coinSums.GetValueOrDefault(u.Id, 0), u.CurrentStreak))
            .ToList();

        return new UserAnalyticsDto(
            totalAvatars,
            levelDist,
            topByXp,
            topByStreak
        );
    }

    public async Task<EconomyAnalyticsDto> GetEconomyAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var totalCirculating = await _context.CoinLedgerEntries
            .SumAsync(e => (long)e.Coins, cancellationToken);

        var earned = await _context.CoinLedgerEntries
            .Where(e => e.Coins > 0)
            .SumAsync(e => (long)e.Coins, cancellationToken);

        var spent = await _context.CoinLedgerEntries
            .Where(e => e.Coins < 0)
            .SumAsync(e => (long)-e.Coins, cancellationToken);

        var purchaseCounts = await _context.UserInventoryItems
            .AsNoTracking()
            .GroupBy(i => i.CatalogItemId)
            .Select(g => new { CatalogItemId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var catalogDict = await _context.CatalogItems
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var allPurchases = purchaseCounts
            .Where(p => catalogDict.ContainsKey(p.CatalogItemId))
            .Select(p =>
            {
                var item = catalogDict[p.CatalogItemId];
                return new TopItemPurchaseDto(item.Id, item.Name, item.Category.ToString(), item.CoinPrice, p.Count);
            })
            .ToList();

        // Kategori harcamasi sinirlanmamis tum satin alimlardan hesaplanir
        var categorySpend = allPurchases
            .GroupBy(x => x.Category)
            .ToDictionary(g => g.Key, g => g.Sum(x => (long)x.Price * x.TotalPurchased));

        var purchasedItems = allPurchases
            .OrderByDescending(x => x.TotalPurchased)
            .Take(TopListSize)
            .ToList();

        return new EconomyAnalyticsDto(
            totalCirculating,
            earned,
            spent,
            purchasedItems,
            categorySpend
        );
    }

    public async Task<SystemHealthDto> GetSystemHealthAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var dbStatus = "Healthy";
        var totalMemories = 0;
        var personalMemories = 0;
        var totalRooms = 0;
        var activeRooms = 0;

        try
        {
            await _context.Users.Take(1).AnyAsync(cancellationToken);
            sw.Stop();

            totalMemories = await _context.AgentMemories.CountAsync(cancellationToken);
            personalMemories = await _context.AgentMemories.CountAsync(m => m.UserId != null, cancellationToken);

            var activeThreshold = DateTime.UtcNow.AddMinutes(-30);
            totalRooms = await _context.StudyRooms.CountAsync(cancellationToken);
            activeRooms = await _context.RoomMembers
                .Where(m => m.LastActiveAt >= activeThreshold)
                .Select(m => m.RoomId)
                .Distinct()
                .CountAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is DbException or InvalidOperationException or DbUpdateException)
        {
            sw.Stop();
            dbStatus = "Degraded";
        }

        return new SystemHealthDto(
            dbStatus,
            Math.Round(sw.Elapsed.TotalMilliseconds, 2),
            RedisNotConfigured,
            totalMemories,
            personalMemories,
            totalMemories - personalMemories,
            totalRooms,
            activeRooms,
            DateTime.UtcNow
        );
    }
}
