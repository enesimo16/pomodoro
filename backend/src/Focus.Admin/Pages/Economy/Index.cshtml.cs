using Focus.Application.Common.Interfaces;
using Focus.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Focus.Admin.Pages.Economy;

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

    public long CirculatingCoins { get; set; }
    public long TotalEarned { get; set; }
    public long TotalSpent { get; set; }
    public int TotalTransactions { get; set; }

    public List<LedgerItem> Entries { get; set; } = new();

    public record LedgerItem(
        Guid Id,
        string UserName,
        int Coins,
        CoinTransactionReason Reason,
        DateTime CreatedAt);

    public async Task OnGetAsync()
    {
        CirculatingCoins = await _context.CoinLedgerEntries.SumAsync(c => (long)c.Coins);
        TotalEarned = await _context.CoinLedgerEntries.Where(c => c.Coins > 0).SumAsync(c => (long)c.Coins);
        TotalSpent = Math.Abs(await _context.CoinLedgerEntries.Where(c => c.Coins < 0).SumAsync(c => (long)c.Coins));
        TotalTransactions = await _context.CoinLedgerEntries.CountAsync();

        var query = _context.CoinLedgerEntries.AsNoTracking();

        if (Filter == "earned")
        {
            query = query.Where(c => c.Coins > 0);
        }
        else if (Filter == "spent")
        {
            query = query.Where(c => c.Coins < 0);
        }

        var list = await query
            .OrderByDescending(c => c.CreatedAt)
            .Take(100)
            .ToListAsync();

        var userIds = list.Select(c => c.UserId).Distinct().ToList();
        var userNames = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName);

        var result = list.Select(c => new LedgerItem(
            c.Id,
            userNames.GetValueOrDefault(c.UserId, "Bilinmeyen"),
            c.Coins,
            c.Reason,
            c.CreatedAt)).ToList();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search.Trim();
            result = result.Where(r => r.UserName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                                       r.Reason.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        Entries = result;
    }
}
