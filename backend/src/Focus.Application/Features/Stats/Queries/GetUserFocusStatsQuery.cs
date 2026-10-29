using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Stats.DTOs;
using Focus.Domain.Common;
using Focus.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Stats.Queries;

public record GetUserFocusStatsQuery(Guid UserId) : IRequest<UserStatsDto>;

public class GetUserFocusStatsQueryHandler : IRequestHandler<GetUserFocusStatsQuery, UserStatsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly TimeProvider _clock;

    public GetUserFocusStatsQueryHandler(IApplicationDbContext context, TimeProvider clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<UserStatsDto> Handle(GetUserFocusStatsQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        var level = user?.Level ?? 1;
        var currentXp = user?.CurrentXp ?? 0;

        var baseForCurrent = LevelCurve.XpRequiredForLevel(level);
        var baseForNext = LevelCurve.XpRequiredForLevel(level + 1);
        var range = Math.Max(1, baseForNext - baseForCurrent);
        var progress = Math.Clamp((double)(currentXp - baseForCurrent) / range * 100.0, 0.0, 100.0);

        var totalCoins = await _context.CoinLedgerEntries
            .AsNoTracking()
            .Where(c => c.UserId == request.UserId)
            .SumAsync(c => (int?)c.Coins, cancellationToken) ?? 0;

        var streak = await _context.UserStreaks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == request.UserId, cancellationToken);

        var inventoryCount = await _context.UserInventoryItems
            .AsNoTracking()
            .CountAsync(i => i.UserId == request.UserId, cancellationToken);

        var now = _clock.GetUtcNow().UtcDateTime;
        var todayStart = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);
        var startOfWeek = now.DayOfWeek == DayOfWeek.Sunday
            ? todayStart.AddDays(-6)
            : todayStart.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday);
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var last7Start = todayStart.AddDays(-6);

        var completedFocus = _context.FocusSessions
            .AsNoTracking()
            .Where(s => s.UserId == request.UserId && s.Status == SessionStatus.Completed && s.Kind == SessionKind.Focus);

        // Toplamlar veritabaninda hesaplanir; seanslarin tamami bellege cekilmez
        var totals = await completedFocus
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalMinutes = g.Sum(s => s.NetDurationSeconds / 60),
                TotalCount = g.Count(),
                TodayMinutes = g.Where(s => s.EndedAt >= todayStart).Sum(s => s.NetDurationSeconds / 60),
                TodayCount = g.Count(s => s.EndedAt >= todayStart),
                WeekMinutes = g.Where(s => s.EndedAt >= startOfWeek).Sum(s => s.NetDurationSeconds / 60),
                MonthMinutes = g.Where(s => s.EndedAt >= startOfMonth).Sum(s => s.NetDurationSeconds / 60)
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Son 7 gunluk dagilim icin yalnizca ilgili araliktaki satirlar alinir
        var recentSessions = await completedFocus
            .Where(s => s.EndedAt >= last7Start)
            .Select(s => new { s.NetDurationSeconds, s.EndedAt })
            .ToListAsync(cancellationToken);

        var last7Days = new List<DailyFocusStatDto>();
        var culture = new System.Globalization.CultureInfo("tr-TR");
        for (var i = 6; i >= 0; i--)
        {
            var targetDay = todayStart.AddDays(-i);
            var nextDay = targetDay.AddDays(1);

            var daySessions = recentSessions
                .Where(s => s.EndedAt >= targetDay && s.EndedAt < nextDay)
                .ToList();

            last7Days.Add(new DailyFocusStatDto(
                targetDay,
                targetDay.ToString("ddd", culture),
                daySessions.Sum(s => s.NetDurationSeconds / 60),
                daySessions.Count));
        }

        var totalMinutes = totals?.TotalMinutes ?? 0;
        var totalCount = totals?.TotalCount ?? 0;
        var todayMinutes = totals?.TodayMinutes ?? 0;
        var todayCount = totals?.TodayCount ?? 0;
        var thisWeekMinutes = totals?.WeekMinutes ?? 0;
        var thisMonthMinutes = totals?.MonthMinutes ?? 0;

        return new UserStatsDto(
            totalMinutes,
            totalCount,
            todayMinutes,
            todayCount,
            thisWeekMinutes,
            thisMonthMinutes,
            streak?.CurrentStreak ?? 0,
            streak?.LongestStreak ?? 0,
            level,
            currentXp,
            baseForCurrent,
            baseForNext,
            Math.Round(progress, 1),
            totalCoins,
            inventoryCount,
            last7Days);
    }
}
