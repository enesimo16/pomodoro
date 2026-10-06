using Focus.Application.Common.Interfaces;
using Focus.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Focus.Admin.Pages.Sessions;

public class IndexModel : PageModel
{
    private readonly IApplicationDbContext _context;

    public IndexModel(IApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public int TotalSessions { get; set; }
    public int CompletedSessions { get; set; }
    public int AbandonedSessions { get; set; }
    public int ActiveSessions { get; set; }
    public long TotalMinutes { get; set; }

    public List<SessionItem> Sessions { get; set; } = new();

    public record SessionItem(
        Guid Id,
        string UserName,
        int FocusMinutes,
        int NetDurationMinutes,
        SessionStatus Status,
        SessionKind Kind,
        int XpEarned,
        int CoinsEarned,
        string? ThemeId,
        DateTime StartedAt,
        DateTime? EndedAt);

    public async Task OnGetAsync()
    {
        TotalSessions = await _context.FocusSessions.CountAsync();
        CompletedSessions = await _context.FocusSessions.CountAsync(s => s.Status == SessionStatus.Completed);
        AbandonedSessions = await _context.FocusSessions.CountAsync(s => s.Status == SessionStatus.Abandoned);
        ActiveSessions = await _context.FocusSessions.CountAsync(s => s.Status == SessionStatus.Running || s.Status == SessionStatus.Paused);
        TotalMinutes = (await _context.FocusSessions.SumAsync(s => (long)s.NetDurationSeconds)) / 60;

        var query = _context.FocusSessions
            .Include(s => s.User)
            .AsNoTracking();

        if (Filter == "completed")
        {
            query = query.Where(s => s.Status == SessionStatus.Completed);
        }
        else if (Filter == "abandoned")
        {
            query = query.Where(s => s.Status == SessionStatus.Abandoned);
        }
        else if (Filter == "active")
        {
            query = query.Where(s => s.Status == SessionStatus.Running || s.Status == SessionStatus.Paused);
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var pattern = $"%{Search.Trim()}%";
            query = query.Where(s => (s.User != null && EF.Functions.ILike(s.User.DisplayName, pattern)) ||
                                     (s.ThemeId != null && EF.Functions.ILike(s.ThemeId, pattern)));
        }

        var list = await query
            .OrderByDescending(s => s.StartedAt)
            .Take(100)
            .ToListAsync();

        Sessions = list.Select(s => new SessionItem(
            s.Id,
            s.User?.DisplayName ?? "Bilinmeyen",
            s.FocusMinutes,
            s.NetDurationSeconds / 60,
            s.Status,
            s.Kind,
            s.XpEarned,
            s.CoinsEarned,
            s.ThemeId,
            s.StartedAt,
            s.EndedAt)).ToList();
    }
}
