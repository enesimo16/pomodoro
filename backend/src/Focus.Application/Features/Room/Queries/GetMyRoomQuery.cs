using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Auth.DTOs;
using Focus.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Room.Queries;

public record GetMyRoomQuery(Guid UserId) : IRequest<PixelRoomDto?>;

public class GetMyRoomQueryHandler : IRequestHandler<GetMyRoomQuery, PixelRoomDto?>
{
    private readonly IApplicationDbContext _context;

    public GetMyRoomQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PixelRoomDto?> Handle(GetMyRoomQuery request, CancellationToken cancellationToken)
    {
        var room = await _context.PixelRooms
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.OwnerUserId == request.UserId, cancellationToken);

        if (room == null)
        {
            room = PixelRoom.CreateDefault(request.UserId);
            _context.PixelRooms.Add(room);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return PixelRoomDto.FromEntity(room);
    }
}
