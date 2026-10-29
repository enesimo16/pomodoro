using Focus.Application.Common.Extensions;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Social.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Social.Commands;

// Uye degilse null doner
public record LeaveRoomCommand(Guid UserId, string RoomCode) : IRequest<LeftRoomDto?>;

public class LeaveRoomCommandHandler : IRequestHandler<LeaveRoomCommand, LeftRoomDto?>
{
    private readonly IApplicationDbContext _context;

    public LeaveRoomCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LeftRoomDto?> Handle(LeaveRoomCommand request, CancellationToken cancellationToken)
    {
        var code = request.RoomCode.ToUpperInvariant().Trim();
        var room = await _context.StudyRooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

        var removed = room?.RemoveMember(request.UserId);
        if (room == null || removed == null) return null;

        _context.RoomMembers.Remove(removed);
        await _context.SaveChangesOrConflictAsync(cancellationToken);
        return new LeftRoomDto(room.Code, removed.SeatIndex);
    }
}
