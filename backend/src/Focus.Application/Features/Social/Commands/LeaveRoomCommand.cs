using Focus.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Social.Commands;

public record LeaveRoomCommand(Guid UserId, string RoomCode) : IRequest<bool>;

public class LeaveRoomCommandHandler : IRequestHandler<LeaveRoomCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public LeaveRoomCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(LeaveRoomCommand request, CancellationToken cancellationToken)
    {
        var code = request.RoomCode.ToUpperInvariant().Trim();
        var room = await _context.StudyRooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

        if (room == null) return false;

        room.RemoveMember(request.UserId);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
