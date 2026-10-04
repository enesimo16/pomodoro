using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Stats.DTOs;
using Focus.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Stats.Queries;

public record GetUserFocusStatsQuery(Guid UserId) : IRequest<UserStatsDto>;

public class GetUserFocusStatsQueryHandler : IRequestHandler<GetUserFocusStatsQuery, UserStatsDto>
{
    private readonly IApplicationDbContext _context;

    public GetUserFocusStatsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserStatsDto> Handle(GetUserFocusStatsQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        var level = user?.Level ?? 1;
        var currentXp = user?.CurrentXp ?? 0;

        var baseForCurrent = (long)Math.Pow(level - 1, 2) * 100;
        var baseForNext = (long)Math.Pow(level, 2) * 100;
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

        // Odaklanma seanslari
        var completedSessions = await _context.FocusSessions
            .AsNoTracking()
            .Where(s => s.UserId == request.UserId && s.Status == SessionStatus.Completed && s.Kind == SessionKind.Focus)
            .Select(s => new { s.NetDurationSeconds, s.EndedAt })
            .ToListAsync(cancellationToken);

        var totalMinutes = completedSessions.Sum(s => s.NetDurationSeconds / 60);
        var totalCount = completedSessions.Count;

        var now = DateTime.UtcNow;
        var todayStart = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);
        var todaySessions = completedSessions.Where(s => s.EndedAt.HasValue && s.EndedAt.Value >= todayStart).ToList();
        var todayMinutes = todaySessions.Sum(s => s.NetDurationSeconds / 60);
        var todayCount = todaySessions.Count;

        var startOfWeek = todayStart.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday);
        if (now.DayOfWeek == DayOfWeek.Sunday)
        {
            startOfWeek = todayStart.AddDays(-6);
        }
        var thisWeekMinutes = completedSessions
            .Where(s => s.EndedAt.HasValue && s.EndedAt.Value >= startOfWeek)
            .Sum(s => s.NetDurationSeconds / 60);

        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var thisMonthMinutes = completedSessions
            .Where(s => s.EndedAt.HasValue && s.EndedAt.Value >= startOfMonth)
            .Sum(s => s.NetDurationSeconds / 60);

        // Son 7 gunluk dagilim
        var last7Days = new List<DailyFocusStatDto>();
        for (var i = 6; i >= 0; i--)
        {
            var targetDay = todayStart.AddDays(-i);
            var nextDay = targetDay.AddDays(1);

            var daySessions = completedSessions
                .Where(s => s.EndedAt.HasValue && s.EndedAt.Value >= targetDay && s.EndedAt.Value < nextDay)
                .ToList();

            var dayName = targetDay.ToString("ddd", new System.Globalization.CultureInfo("tr-TR"));
            last7Days.Add(new DailyFocusStatDto(
                targetDay,
                dayName,
                daySessions.Sum(s => s.NetDurationSeconds / 60),
                daySessions.Count));
        }

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
