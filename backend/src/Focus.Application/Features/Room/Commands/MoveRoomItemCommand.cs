using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Auth.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Room.Commands;

public record MoveRoomItemCommand(
    Guid UserId,
    Guid ItemId,
    int GridX,
    int GridY,
    int Rotation) : IRequest<RoomItemDto?>;

public class MoveRoomItemCommandHandler : IRequestHandler<MoveRoomItemCommand, RoomItemDto?>
{
    private readonly IApplicationDbContext _context;

    public MoveRoomItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RoomItemDto?> Handle(MoveRoomItemCommand request, CancellationToken cancellationToken)
    {
        var room = await _context.PixelRooms
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.OwnerUserId == request.UserId, cancellationToken);

        if (room == null)
        {
            return null;
        }

        var normalizedRotation = (request.Rotation % 360 + 360) % 360;
        var validGridX = Math.Max(0, request.GridX);
        var validGridY = Math.Max(0, request.GridY);

        var success = room.MoveItem(request.ItemId, validGridX, validGridY, normalizedRotation);
        if (!success)
        {
            return null;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var item = room.Items.First(i => i.Id == request.ItemId);
        return new RoomItemDto(item.Id, item.CatalogItemId, item.GridX, item.GridY, item.Rotation);
    }
}
