using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Social.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Social.Queries;

public record GetRoomByCodeQuery(string Code) : IRequest<StudyRoomDto?>;

public class GetRoomByCodeQueryHandler : IRequestHandler<GetRoomByCodeQuery, StudyRoomDto?>
{
    private readonly IApplicationDbContext _context;

    public GetRoomByCodeQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StudyRoomDto?> Handle(GetRoomByCodeQuery request, CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant().Trim();

        var room = await _context.StudyRooms
            .Include(r => r.OwnerUser)
            .Include(r => r.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

        if (room == null) return null;

        return new StudyRoomDto(
            room.Id,
            room.Code,
            room.Name,
            room.Description,
            room.Type.ToString(),
            room.OwnerUserId,
            room.OwnerUser?.DisplayName,
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
        );
    }
}
