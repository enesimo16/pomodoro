using System.Diagnostics;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.AdminAnalytics.DTOs;
using Focus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Focus.Infrastructure.Services;

public class AdminAnalyticsService : IAdminAnalyticsService
{
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

        var users = await _context.Users
            .AsNoTracking()
            .Select(u => new { u.Id, u.DisplayName, u.Level, u.CurrentXp })
            .ToListAsync(cancellationToken);

        var levelDist = new Dictionary<string, int>
        {
            ["Sv. 1-5"] = users.Count(u => u.Level >= 1 && u.Level <= 5),
            ["Sv. 6-10"] = users.Count(u => u.Level >= 6 && u.Level <= 10),
            ["Sv. 11-20"] = users.Count(u => u.Level >= 11 && u.Level <= 20),
            ["Sv. 21+"] = users.Count(u => u.Level >= 21)
        };

        var streaks = await _context.UserStreaks
            .AsNoTracking()
            .ToDictionaryAsync(s => s.UserId, s => s.CurrentStreak, cancellationToken);

        var coinSums = await _context.CoinLedgerEntries
            .AsNoTracking()
            .GroupBy(c => c.UserId)
            .Select(g => new { UserId = g.Key, Coins = g.Sum(c => c.Coins) })
            .ToDictionaryAsync(x => x.UserId, x => x.Coins, cancellationToken);

        var topByXp = users
            .OrderByDescending(u => u.CurrentXp)
            .Take(10)
            .Select(u => new TopUserSummaryDto(
                u.Id,
                u.DisplayName,
                u.Level,
                u.CurrentXp,
                coinSums.GetValueOrDefault(u.Id, 0),
                streaks.GetValueOrDefault(u.Id, 0)
            ))
            .ToList();

        var topByStreak = users
            .OrderByDescending(u => streaks.GetValueOrDefault(u.Id, 0))
            .ThenByDescending(u => u.CurrentXp)
            .Take(10)
            .Select(u => new TopUserSummaryDto(
                u.Id,
                u.DisplayName,
                u.Level,
                u.CurrentXp,
                coinSums.GetValueOrDefault(u.Id, 0),
                streaks.GetValueOrDefault(u.Id, 0)
            ))
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

        var inventoryItems = await _context.UserInventoryItems
            .AsNoTracking()
            .Select(i => new { i.CatalogItemId })
            .ToListAsync(cancellationToken);

        var catalogDict = await _context.CatalogItems
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var purchasedItems = inventoryItems
            .GroupBy(i => i.CatalogItemId)
            .Where(g => catalogDict.ContainsKey(g.Key))
            .Select(g =>
            {
                var item = catalogDict[g.Key];
                return new TopItemPurchaseDto(
                    item.Id,
                    item.Name,
                    item.Category.ToString(),
                    item.CoinPrice,
                    g.Count()
                );
            })
            .OrderByDescending(x => x.TotalPurchased)
            .Take(10)
            .ToList();

        var categorySpend = purchasedItems
            .GroupBy(x => x.Category)
            .ToDictionary(g => g.Key, g => g.Sum(x => (long)x.Price * x.TotalPurchased));

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
        try
        {
            await _context.Users.Take(1).AnyAsync(cancellationToken);
        }
        catch
        {
            dbStatus = "Degraded";
        }
        sw.Stop();

        var totalMemories = await _context.AgentMemories.CountAsync(cancellationToken);
        var personalMemories = await _context.AgentMemories.CountAsync(m => m.UserId != null, cancellationToken);
        var globalMemories = totalMemories - personalMemories;

        var activeThreshold = DateTime.UtcNow.AddMinutes(-30);
        var totalRooms = await _context.StudyRooms.CountAsync(cancellationToken);
        var activeRooms = await _context.RoomMembers
            .Where(m => m.LastActiveAt >= activeThreshold)
            .Select(m => m.RoomId)
            .Distinct()
            .CountAsync(cancellationToken);

        return new SystemHealthDto(
            dbStatus,
            Math.Round(sw.Elapsed.TotalMilliseconds, 2),
            "Healthy",
            totalMemories,
            personalMemories,
            globalMemories,
            totalRooms,
            activeRooms,
            DateTime.UtcNow
        );
    }
}
