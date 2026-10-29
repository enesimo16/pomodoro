using Focus.Application.Common.Interfaces;
using Focus.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Focus.Admin.Pages.Users;

public class IndexModel : PageModel
{
    public const int MaxCoinAdjustment = 100_000;
    public const int MaxReasonLength = 200;
    public const long MaxXpGrant = 1_000_000;

    private readonly IApplicationDbContext _context;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IApplicationDbContext context, ILogger<IndexModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    public int TotalCount { get; set; }
    public int RegisteredCount { get; set; }
    public int GuestCount { get; set; }
    public long TotalCoins { get; set; }

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
        TotalCoins = await _context.CoinLedgerEntries.SumAsync(c => (long?)c.Coins) ?? 0;

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
        var trimmedReason = reason?.Trim();
        if (string.IsNullOrEmpty(trimmedReason) || trimmedReason.Length > MaxReasonLength)
        {
            return Fail($"Gerekçe zorunludur ve en fazla {MaxReasonLength} karakter olabilir.");
        }

        if (amount == 0 || Math.Abs((long)amount) > MaxCoinAdjustment)
        {
            return Fail($"Miktar sıfırdan farklı olmalı ve -{MaxCoinAdjustment:N0} ile {MaxCoinAdjustment:N0} arasında olmalıdır.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return Fail("Kullanıcı bulunamadı.");
        }

        var balance = await _context.CoinLedgerEntries
            .Where(c => c.UserId == userId)
            .SumAsync(c => (long?)c.Coins) ?? 0;
        if (balance + amount < 0)
        {
            return Fail($"Bakiye sıfırın altına düşemez (mevcut bakiye: {balance:N0}).");
        }

        _context.CoinLedgerEntries.Add(new CoinLedgerEntry(userId, CoinTransactionReason.AdminAdjustment, amount));
        user.RegisterWalletChange();
        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin coin adjustment applied: User {UserId}, Amount {Amount}, Reason {Reason}", userId, amount, trimmedReason);
        StatusMessage = $"{user.DisplayName} için {amount:+#,0;-#,0} coin uygulandı.";
        return RedirectToPage(new { Search, Filter });
    }

    public async Task<IActionResult> OnPostAddXpAsync(Guid userId, long xp)
    {
        if (xp < 1 || xp > MaxXpGrant)
        {
            return Fail($"XP miktarı 1 ile {MaxXpGrant:N0} arasında olmalıdır.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return Fail("Kullanıcı bulunamadı.");
        }

        if (user.CurrentXp > long.MaxValue - xp)
        {
            return Fail("Kullanıcının XP değeri bu eklemeyi kaldıramaz.");
        }

        user.AddXp(xp);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin XP grant applied: User {UserId}, Xp {Xp}", userId, xp);
        StatusMessage = $"{user.DisplayName} için {xp:N0} XP eklendi.";
        return RedirectToPage(new { Search, Filter });
    }

    private IActionResult Fail(string message)
    {
        StatusMessage = message;
        return RedirectToPage(new { Search, Filter });
    }
}
