using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Social.DTOs;
using Focus.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Social.Queries;

public record GetPublicRoomsQuery : IRequest<List<StudyRoomDto>>;

public class GetPublicRoomsQueryHandler : IRequestHandler<GetPublicRoomsQuery, List<StudyRoomDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPublicRoomsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<StudyRoomDto>> Handle(GetPublicRoomsQuery request, CancellationToken cancellationToken)
    {
        var rooms = await _context.StudyRooms
            .Include(r => r.Members)
                .ThenInclude(m => m.User)
            .Where(r => r.Type == StudyRoomType.PublicLibrary || r.Type == StudyRoomType.ShopMarket)
            .OrderBy(r => r.Type)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return rooms.Select(room => new StudyRoomDto(
            room.Id,
            room.Code,
            room.Name,
            room.Description,
            room.Type.ToString(),
            room.OwnerUserId,
            null,
            room.Members.Count,
            room.MaxCapacity,
            room.IsPrivate,
            room.ThemeId,
            room.MusicTrackId,
            room.CalculateCoWorkingBonusPercent(),
            room.Members.Select(m => new RoomMemberDto(
                m.Id,
                m.UserId,
                m.User?.DisplayName ?? "Kullanıcı",
                m.User?.Level ?? 1,
                null,
                m.SeatIndex,
                m.IsFocusing,
                m.JoinedAt
            )).ToList()
        )).ToList();
    }
}
