using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Auth.DTOs;
using Focus.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Room.Commands;

public record UpdateRoomThemeCommand(
    Guid UserId,
    string? WallpaperCatalogId,
    string? FloorCatalogId) : IRequest<PixelRoomDto?>;

public class UpdateRoomThemeCommandHandler : IRequestHandler<UpdateRoomThemeCommand, PixelRoomDto?>
{
    private readonly IApplicationDbContext _context;

    public UpdateRoomThemeCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PixelRoomDto?> Handle(UpdateRoomThemeCommand request, CancellationToken cancellationToken)
    {
        var room = await _context.PixelRooms
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.OwnerUserId == request.UserId, cancellationToken);

        if (room == null)
        {
            room = PixelRoom.CreateDefault(request.UserId);
            _context.PixelRooms.Add(room);
        }

        room.UpdateTheme(request.WallpaperCatalogId, request.FloorCatalogId);
        await _context.SaveChangesAsync(cancellationToken);

        return PixelRoomDto.FromEntity(room);
    }
}
