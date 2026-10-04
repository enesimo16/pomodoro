using Focus.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Focus.Admin.Pages;

public class IndexModel : PageModel
{
    private readonly IApplicationDbContext _context;

    public IndexModel(IApplicationDbContext context)
    {
        _context = context;
    }

    public int TotalUsers { get; set; }
    public int GuestUsers { get; set; }
    public int RegisteredUsers { get; set; }
    public int TotalCoinsInCirculation { get; set; }
    public List<RecentUserDto> RecentUsers { get; set; } = new();

    public record RecentUserDto(
        Guid Id,
        string DisplayName,
        string? Email,
        bool IsGuest,
        int Level,
        long Xp,
        DateTime CreatedAt);

    public async Task OnGetAsync()
    {
        TotalUsers = await _context.Users.CountAsync();
        GuestUsers = await _context.Users.CountAsync(u => u.IsGuest);
        RegisteredUsers = TotalUsers - GuestUsers;

        TotalCoinsInCirculation = await _context.CoinLedgerEntries.SumAsync(c => (int?)c.Coins) ?? 0;

        RecentUsers = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .Select(u => new RecentUserDto(
                u.Id,
                u.DisplayName,
                u.Email,
                u.IsGuest,
                u.Level,
                u.CurrentXp,
                u.CreatedAt))
            .ToListAsync();
    }
}
