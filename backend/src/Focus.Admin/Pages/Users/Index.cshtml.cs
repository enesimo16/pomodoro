using Focus.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Focus.Admin.Pages.Users;

public class IndexModel : PageModel
{
    private readonly IApplicationDbContext _context;

    public IndexModel(IApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    public List<UserViewModel> Users { get; set; } = new();

    public record UserViewModel(
        Guid Id,
        string DisplayName,
        string? Email,
        bool IsGuest,
        int Level,
        long Xp,
        int Coins,
        string? RoomName,
        string? AvatarTop,
        DateTime CreatedAt);

    public async Task OnGetAsync()
    {
        var query = _context.Users
            .Include(u => u.Avatar)
            .Include(u => u.Room)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var pattern = $"%{Search.Trim()}%";
            query = query.Where(u => EF.Functions.ILike(u.DisplayName, pattern) ||
                                     (u.Email != null && EF.Functions.ILike(u.Email, pattern)));
        }

        if (Filter == "guest")
        {
            query = query.Where(u => u.IsGuest);
        }
        else if (Filter == "registered")
        {
            query = query.Where(u => !u.IsGuest);
        }

        var usersList = await query
            .OrderByDescending(u => u.CreatedAt)
            .Take(100)
            .ToListAsync();

        var userIds = usersList.Select(u => u.Id).ToList();
        var coinBalances = await _context.CoinLedgerEntries
            .Where(c => userIds.Contains(c.UserId))
            .GroupBy(c => c.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(c => c.Coins) })
            .ToDictionaryAsync(x => x.UserId, x => x.Total);

        Users = usersList.Select(u => new UserViewModel(
            u.Id,
            u.DisplayName,
            u.Email,
            u.IsGuest,
            u.Level,
            u.CurrentXp,
            coinBalances.GetValueOrDefault(u.Id, 0),
            u.Room?.Name,
            u.Avatar?.TopItemCatalogId,
            u.CreatedAt)).ToList();
    }
}
