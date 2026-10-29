using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Auth.DTOs;
using FluentValidation;
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
        var rotation = RoomPlacement.RequireRotation(request.Rotation);

        var room = await _context.PixelRooms
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.OwnerUserId == request.UserId, cancellationToken);

        var existing = room?.Items.FirstOrDefault(i => i.Id == request.ItemId);
        if (room == null || existing == null)
        {
            return null;
        }

        var catalogItem = await _context.CatalogItems.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == existing.CatalogItemId, cancellationToken)
            ?? throw new ValidationException("Geçersiz eşya seçimi.");

        await RoomPlacement.EnsureFitsAsync(_context, room, catalogItem, request.GridX, request.GridY, rotation, existing.Id, cancellationToken);

        room.MoveItem(request.ItemId, request.GridX, request.GridY, rotation);
        await _context.SaveChangesAsync(cancellationToken);

        return new RoomItemDto(existing.Id, existing.CatalogItemId, existing.GridX, existing.GridY, existing.Rotation);
    }
}
