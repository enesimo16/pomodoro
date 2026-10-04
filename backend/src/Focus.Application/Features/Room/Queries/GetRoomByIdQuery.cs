using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Auth.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Room.Queries;

public record GetRoomByIdQuery(Guid RoomId, Guid? RequestingUserId = null) : IRequest<PixelRoomDto?>;

public class GetRoomByIdQueryHandler : IRequestHandler<GetRoomByIdQuery, PixelRoomDto?>
{
    private readonly IApplicationDbContext _context;

    public GetRoomByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PixelRoomDto?> Handle(GetRoomByIdQuery request, CancellationToken cancellationToken)
    {
        var room = await _context.PixelRooms
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == request.RoomId, cancellationToken);

        if (room == null)
        {
            return null;
        }

        // Gizli odalar sadece oda sahibi tarafindan goruntulenebilir
        if (!room.IsPublic && request.RequestingUserId != room.OwnerUserId)
        {
            return null;
        }

        return PixelRoomDto.FromEntity(room);
    }
}
