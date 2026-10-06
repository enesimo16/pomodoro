using Focus.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Focus.Admin.Pages.Audit;

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
    public int UniqueIps { get; set; }
    public int MobileCount { get; set; }
    public int DesktopCount { get; set; }
    public long TotalActiveMinutes { get; set; }
    public int ActiveNowCount { get; set; }

    public List<AuditItem> Sessions { get; set; } = new();

    public record AuditItem(
        Guid Id,
        string UserName,
        string IpAddress,
        string DeviceType,
        string OperatingSystem,
        string Browser,
        int LoginCount,
        int ActiveMinutes,
        string? LastPath,
        DateTime FirstSeenAt,
        DateTime LastSeenAt,
        bool IsOnlineNow);

    public async Task OnGetAsync()
    {
        var fiveMinAgo = DateTime.UtcNow.AddMinutes(-5);

        TotalSessions = await _context.UserDeviceSessions.CountAsync();
        UniqueIps = await _context.UserDeviceSessions.Select(s => s.IpAddress).Distinct().CountAsync();
        MobileCount = await _context.UserDeviceSessions.CountAsync(s => s.DeviceType == "Mobil");
        DesktopCount = await _context.UserDeviceSessions.CountAsync(s => s.DeviceType == "Masaüstü");
        TotalActiveMinutes = await _context.UserDeviceSessions.SumAsync(s => (long)s.ActiveMinutes);
        ActiveNowCount = await _context.UserDeviceSessions.CountAsync(s => s.LastSeenAt >= fiveMinAgo);

        var query = _context.UserDeviceSessions
            .Include(s => s.User)
            .AsNoTracking();

        if (Filter == "online")
        {
            query = query.Where(s => s.LastSeenAt >= fiveMinAgo);
        }
        else if (Filter == "mobile")
        {
            query = query.Where(s => s.DeviceType == "Mobil" || s.DeviceType == "Tablet");
        }
        else if (Filter == "desktop")
        {
            query = query.Where(s => s.DeviceType == "Masaüstü");
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var pattern = $"%{Search.Trim()}%";
            query = query.Where(s => EF.Functions.ILike(s.IpAddress, pattern) ||
                                     EF.Functions.ILike(s.Browser, pattern) ||
                                     EF.Functions.ILike(s.OperatingSystem, pattern) ||
                                     (s.User != null && EF.Functions.ILike(s.User.DisplayName, pattern)));
        }

        var list = await query
            .OrderByDescending(s => s.LastSeenAt)
            .Take(100)
            .ToListAsync();

        Sessions = list.Select(s => new AuditItem(
            s.Id,
            s.User?.DisplayName ?? (s.UserId.HasValue ? "Kullanıcı" : "Ziyaretçi"),
            s.IpAddress,
            s.DeviceType,
            s.OperatingSystem,
            s.Browser,
            s.LoginCount,
            s.ActiveMinutes,
            s.LastPath,
            s.FirstSeenAt,
            s.LastSeenAt,
            s.LastSeenAt >= fiveMinAgo)).ToList();
    }
}
