using Focus.Application.Common.Extensions;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Social.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Social.Commands;

// Oda sahibi degilse, hedef uye degilse veya hedef sahibin kendisiyse null doner
public record KickRoomMemberCommand(Guid OwnerUserId, string RoomCode, Guid TargetUserId) : IRequest<LeftRoomDto?>;

public class KickRoomMemberCommandHandler : IRequestHandler<KickRoomMemberCommand, LeftRoomDto?>
{
    private readonly IApplicationDbContext _context;

    public KickRoomMemberCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LeftRoomDto?> Handle(KickRoomMemberCommand request, CancellationToken cancellationToken)
    {
        var code = request.RoomCode.ToUpperInvariant().Trim();
        var room = await _context.StudyRooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

        if (room == null || room.OwnerUserId != request.OwnerUserId || request.TargetUserId == request.OwnerUserId)
        {
            return null;
        }

        var removed = room.RemoveMember(request.TargetUserId);
        if (removed == null) return null;

        _context.RoomMembers.Remove(removed);
        await _context.SaveChangesOrConflictAsync(cancellationToken);
        return new LeftRoomDto(room.Code, removed.SeatIndex);
    }
}
