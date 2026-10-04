using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Auth.DTOs;
using Focus.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Room.Commands;

public record UpdateRoomDetailsCommand(
    Guid UserId,
    string Name,
    bool IsPublic,
    int MaxVisitors = 5) : IRequest<PixelRoomDto?>;

public class UpdateRoomDetailsCommandHandler : IRequestHandler<UpdateRoomDetailsCommand, PixelRoomDto?>
{
    private readonly IApplicationDbContext _context;

    public UpdateRoomDetailsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PixelRoomDto?> Handle(UpdateRoomDetailsCommand request, CancellationToken cancellationToken)
    {
        var room = await _context.PixelRooms
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.OwnerUserId == request.UserId, cancellationToken);

        if (room == null)
        {
            room = PixelRoom.CreateDefault(request.UserId);
            _context.PixelRooms.Add(room);
        }

        room.UpdateDetails(request.Name, request.IsPublic, request.MaxVisitors);
        await _context.SaveChangesAsync(cancellationToken);

        return PixelRoomDto.FromEntity(room);
    }
}
