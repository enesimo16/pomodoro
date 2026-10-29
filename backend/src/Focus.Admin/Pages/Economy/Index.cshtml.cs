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

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public const int PageSize = 100;
    public const int MaxPageNumber = 1000;

    public bool HasNextPage { get; set; }
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

        PageNumber = Math.Clamp(PageNumber, 1, MaxPageNumber);

        var query = _context.CoinLedgerEntries.AsNoTracking();

        if (Filter == "earned")
        {
            query = query.Where(c => c.Coins > 0);
        }
        else if (Filter == "spent")
        {
            query = query.Where(c => c.Coins < 0);
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search.Trim().ToLowerInvariant();
            var matchingReasons = Enum.GetValues<CoinTransactionReason>()
                .Where(r => r.ToString().Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
            // ToLower().Contains sorguda LOWER ... LIKE olarak cevrilir, StringComparison asiri yuklemesi cevrilemez
#pragma warning disable CA1304, CA1311, CA1862
            query = query.Where(c => c.User.DisplayName.ToLower().Contains(term) || matchingReasons.Contains(c.Reason));
#pragma warning restore CA1304, CA1311, CA1862
        }

        var page = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize + 1)
            .Select(c => new LedgerItem(c.Id, c.User.DisplayName, c.Coins, c.Reason, c.CreatedAt))
            .ToListAsync();

        HasNextPage = page.Count > PageSize;
        Entries = page.Take(PageSize).ToList();
    }
}
