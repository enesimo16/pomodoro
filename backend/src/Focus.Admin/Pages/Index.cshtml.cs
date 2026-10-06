using Focus.Application.Common.Interfaces;
using Focus.Application.Features.AdminAnalytics.DTOs;
using Focus.Domain.Enums;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Focus.Admin.Pages;

public class IndexModel : PageModel
{
    private readonly IApplicationDbContext _context;
    private readonly IAdminAnalyticsService _analyticsService;

    public IndexModel(IApplicationDbContext context, IAdminAnalyticsService analyticsService)
    {
        _context = context;
        _analyticsService = analyticsService;
    }

    public PlatformOverviewDto Overview { get; set; } = null!;
    public SystemHealthDto Health { get; set; } = null!;
    public List<RecentUserItem> RecentUsers { get; set; } = new();
    public List<RecentSessionItem> RecentSessions { get; set; } = new();

    public record RecentUserItem(
        Guid Id,
        string DisplayName,
        string? Email,
        bool IsGuest,
        int Level,
        long CurrentXp,
        int Coins,
        DateTime CreatedAt);

    public record RecentSessionItem(
        Guid Id,
        string UserName,
        int FocusMinutes,
        int NetDurationMinutes,
        SessionStatus Status,
        string? ThemeId,
        DateTime StartedAt);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Overview = await _analyticsService.GetOverviewAsync(cancellationToken);
        Health = await _analyticsService.GetSystemHealthAsync(cancellationToken);

        var users = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(8)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.Id).ToList();
        var coinBalances = await _context.CoinLedgerEntries
            .Where(c => userIds.Contains(c.UserId))
            .GroupBy(c => c.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(c => c.Coins) })
            .ToDictionaryAsync(x => x.UserId, x => x.Total, cancellationToken);

        RecentUsers = users.Select(u => new RecentUserItem(
            u.Id,
            u.DisplayName,
            u.Email,
            u.IsGuest,
            u.Level,
            u.CurrentXp,
            coinBalances.GetValueOrDefault(u.Id, 0),
            u.CreatedAt)).ToList();

        var sessions = await _context.FocusSessions
            .OrderByDescending(s => s.StartedAt)
            .Take(8)
            .ToListAsync(cancellationToken);

        var sessionUserIds = sessions.Select(s => s.UserId).Distinct().ToList();
        var userNames = await _context.Users
            .Where(u => sessionUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);

        RecentSessions = sessions.Select(s => new RecentSessionItem(
            s.Id,
            userNames.GetValueOrDefault(s.UserId, "Bilinmeyen"),
            s.FocusMinutes,
            s.NetDurationSeconds / 60,
            s.Status,
            s.ThemeId,
            s.StartedAt)).ToList();
    }
}
