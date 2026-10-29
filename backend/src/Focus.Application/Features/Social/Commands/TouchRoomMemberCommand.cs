using Focus.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Social.Commands;

// Oda varlik sinyali; uye degilse false doner
public record TouchRoomMemberCommand(Guid UserId, string RoomCode) : IRequest<bool>;

public class TouchRoomMemberCommandHandler : IRequestHandler<TouchRoomMemberCommand, bool>
{
    private static readonly TimeSpan MinTouchInterval = TimeSpan.FromMinutes(1);

    private readonly IApplicationDbContext _context;

    public TouchRoomMemberCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(TouchRoomMemberCommand request, CancellationToken cancellationToken)
    {
        var code = request.RoomCode.ToUpperInvariant().Trim();
        var member = await _context.RoomMembers
            .FirstOrDefaultAsync(m => m.UserId == request.UserId && m.Room.Code == code, cancellationToken);

        if (member == null) return false;

        if (member.LastActiveAt == null || DateTime.UtcNow - member.LastActiveAt.Value >= MinTouchInterval)
        {
            member.Touch();
            await _context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }
}
