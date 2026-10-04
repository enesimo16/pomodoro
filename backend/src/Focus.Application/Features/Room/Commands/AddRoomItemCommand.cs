using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Auth.DTOs;
using Focus.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Room.Commands;

public record AddRoomItemCommand(
    Guid UserId,
    string CatalogItemId,
    int GridX,
    int GridY,
    int Rotation) : IRequest<RoomItemDto>;

public class AddRoomItemCommandHandler : IRequestHandler<AddRoomItemCommand, RoomItemDto>
{
    private readonly IApplicationDbContext _context;

    public AddRoomItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RoomItemDto> Handle(AddRoomItemCommand request, CancellationToken cancellationToken)
    {
        var room = await _context.PixelRooms
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.OwnerUserId == request.UserId, cancellationToken);

        if (room == null)
        {
            room = PixelRoom.CreateDefault(request.UserId);
            _context.PixelRooms.Add(room);
        }

        var normalizedRotation = (request.Rotation % 360 + 360) % 360;
        var validGridX = Math.Max(0, request.GridX);
        var validGridY = Math.Max(0, request.GridY);

        var item = room.AddItem(request.CatalogItemId, validGridX, validGridY, normalizedRotation);
        _context.RoomItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        return new RoomItemDto(item.Id, item.CatalogItemId, item.GridX, item.GridY, item.Rotation);
    }
}
