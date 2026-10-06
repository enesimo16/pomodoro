using Focus.Application.Common.Interfaces;
using Focus.Domain.Entities;
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

    public int TotalCount { get; set; }
    public int RegisteredCount { get; set; }
    public int GuestCount { get; set; }
    public int TotalCoins { get; set; }

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
        TotalCount = await _context.Users.CountAsync();
        GuestCount = await _context.Users.CountAsync(u => u.IsGuest);
        RegisteredCount = TotalCount - GuestCount;
        TotalCoins = await _context.CoinLedgerEntries.SumAsync(c => (int?)c.Coins) ?? 0;

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

    public async Task<IActionResult> OnPostGrantCoinsAsync(Guid userId, int amount, string? reason)
    {
        if (amount != 0)
        {
            var entry = new CoinLedgerEntry(userId, CoinTransactionReason.InitialBonus, amount);
            _context.CoinLedgerEntries.Add(entry);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage(new { Search, Filter });
    }

    public async Task<IActionResult> OnPostAddXpAsync(Guid userId, long xp)
    {
        if (xp > 0)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                user.AddXp(xp);
                await _context.SaveChangesAsync();
            }
        }

        return RedirectToPage(new { Search, Filter });
    }
}
