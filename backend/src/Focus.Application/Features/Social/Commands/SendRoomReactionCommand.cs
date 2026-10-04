using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Social.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Social.Commands;

public record SendRoomReactionCommand(Guid UserId, string RoomCode, string Reaction) : IRequest<RoomReactionDto?>;

public class SendRoomReactionCommandHandler : IRequestHandler<SendRoomReactionCommand, RoomReactionDto?>
{
    private readonly IApplicationDbContext _context;

    public SendRoomReactionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RoomReactionDto?> Handle(SendRoomReactionCommand request, CancellationToken cancellationToken)
    {
        var code = request.RoomCode.ToUpperInvariant().Trim();
        var room = await _context.StudyRooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

        if (room == null || !room.Members.Any(m => m.UserId == request.UserId))
        {
            return null;
        }

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        var displayName = user?.DisplayName ?? "Kullanıcı";

        return new RoomReactionDto(
            room.Id,
            request.UserId,
            displayName,
            request.Reaction,
            DateTime.UtcNow);
    }
}
