using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Auth.DTOs;
using Focus.Domain.Entities;
using FluentValidation;
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
        var rotation = RoomPlacement.RequireRotation(request.Rotation);
        var catalogItem = await CatalogOwnership.RequireOwnedAsync(
            _context, request.UserId, request.CatalogItemId, CatalogOwnership.FurnitureCategories, cancellationToken);

        var room = await _context.PixelRooms
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.OwnerUserId == request.UserId, cancellationToken);

        if (room == null)
        {
            room = PixelRoom.CreateDefault(request.UserId);
            _context.PixelRooms.Add(room);
        }

        if (room.Items.Count >= PixelRoom.MaxItems)
        {
            throw new ValidationException($"Odada en fazla {PixelRoom.MaxItems} eşya bulunabilir.");
        }

        // Her eşya tek adet sahiplenilir (envanterde (UserId, CatalogItemId) benzersizdir)
        if (room.Items.Any(i => i.CatalogItemId == catalogItem.Id))
        {
            throw new ValidationException("Bu eşya zaten odaya yerleştirilmiş.");
        }

        await RoomPlacement.EnsureFitsAsync(_context, room, catalogItem, request.GridX, request.GridY, rotation, null, cancellationToken);

        var item = room.AddItem(catalogItem.Id, request.GridX, request.GridY, rotation);
        _context.RoomItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        return new RoomItemDto(item.Id, item.CatalogItemId, item.GridX, item.GridY, item.Rotation);
    }
}
