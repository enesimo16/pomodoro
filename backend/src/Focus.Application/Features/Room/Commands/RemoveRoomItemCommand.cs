using Focus.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Room.Commands;

public record RemoveRoomItemCommand(Guid UserId, Guid ItemId) : IRequest<bool>;

public class RemoveRoomItemCommandHandler : IRequestHandler<RemoveRoomItemCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public RemoveRoomItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(RemoveRoomItemCommand request, CancellationToken cancellationToken)
    {
        var room = await _context.PixelRooms
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.OwnerUserId == request.UserId, cancellationToken);

        if (room == null)
        {
            return false;
        }

        var itemToRemove = room.Items.FirstOrDefault(i => i.Id == request.ItemId);
        if (itemToRemove == null)
        {
            return false;
        }

        room.RemoveItem(request.ItemId);
        _context.RoomItems.Remove(itemToRemove);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
