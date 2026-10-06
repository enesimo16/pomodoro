using Focus.Application.Common.Interfaces;
using Focus.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Focus.Admin.Pages.StudyRooms;

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

    public int TotalRooms { get; set; }
    public int PrivateRooms { get; set; }
    public int LibraryRooms { get; set; }
    public int TotalActiveMembers { get; set; }

    public List<RoomItem> Rooms { get; set; } = new();

    public record RoomItem(
        Guid Id,
        string Code,
        string Name,
        string? Description,
        StudyRoomType Type,
        int MaxCapacity,
        int CurrentMembers,
        string OwnerName,
        DateTime CreatedAt);

    public async Task OnGetAsync()
    {
        TotalRooms = await _context.StudyRooms.CountAsync();
        PrivateRooms = await _context.StudyRooms.CountAsync(r => r.Type == StudyRoomType.PrivateHome);
        LibraryRooms = await _context.StudyRooms.CountAsync(r => r.Type == StudyRoomType.PublicLibrary);
        TotalActiveMembers = await _context.RoomMembers.CountAsync();

        var query = _context.StudyRooms
            .Include(r => r.Members)
            .AsNoTracking();

        if (Filter == "private")
        {
            query = query.Where(r => r.Type == StudyRoomType.PrivateHome);
        }
        else if (Filter == "library")
        {
            query = query.Where(r => r.Type == StudyRoomType.PublicLibrary);
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var pattern = $"%{Search.Trim()}%";
            query = query.Where(r => EF.Functions.ILike(r.Code, pattern) ||
                                     EF.Functions.ILike(r.Name, pattern));
        }

        var list = await query
            .OrderByDescending(r => r.CreatedAt)
            .Take(100)
            .ToListAsync();

        var ownerIds = list.Where(r => r.OwnerUserId.HasValue).Select(r => r.OwnerUserId!.Value).Distinct().ToList();
        var ownerNames = await _context.Users
            .Where(u => ownerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName);

        Rooms = list.Select(r => new RoomItem(
            r.Id,
            r.Code,
            r.Name,
            r.Description,
            r.Type,
            r.MaxCapacity,
            r.Members.Count,
            r.OwnerUserId.HasValue ? ownerNames.GetValueOrDefault(r.OwnerUserId.Value, "Kullanıcı") : "Genel Sistem",
            r.CreatedAt)).ToList();
    }
}
