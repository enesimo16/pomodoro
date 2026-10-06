using Focus.Application.Common.Interfaces;
using Focus.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Focus.Admin.Pages.AiCoach;

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

    public int TotalMemories { get; set; }
    public int GlobalMemories { get; set; }
    public int PrivateMemories { get; set; }
    public int TotalReflections { get; set; }

    public List<MemoryItem> Memories { get; set; } = new();

    public record MemoryItem(
        Guid Id,
        string OwnerName,
        bool IsGlobal,
        MemoryCategory Category,
        string Content,
        int Importance,
        int UsageCount,
        DateTime CreatedAt);

    public async Task OnGetAsync()
    {
        TotalMemories = await _context.AgentMemories.CountAsync();
        GlobalMemories = await _context.AgentMemories.CountAsync(m => m.UserId == null);
        PrivateMemories = await _context.AgentMemories.CountAsync(m => m.UserId != null);
        TotalReflections = await _context.SessionReflections.CountAsync();

        var query = _context.AgentMemories.AsNoTracking();

        if (Filter == "global")
        {
            query = query.Where(m => m.UserId == null);
        }
        else if (Filter == "private")
        {
            query = query.Where(m => m.UserId != null);
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var pattern = $"%{Search.Trim()}%";
            query = query.Where(m => EF.Functions.ILike(m.Content, pattern));
        }

        var list = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(100)
            .ToListAsync();

        var userIds = list.Where(m => m.UserId.HasValue).Select(m => m.UserId!.Value).Distinct().ToList();
        var userNames = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName);

        Memories = list.Select(m => new MemoryItem(
            m.Id,
            m.UserId.HasValue ? userNames.GetValueOrDefault(m.UserId.Value, "Kullanıcı") : "Global Anonim Havuz",
            m.IsGlobal,
            m.Category,
            m.Content,
            m.Importance,
            m.UsageCount,
            m.CreatedAt)).ToList();
    }
}
