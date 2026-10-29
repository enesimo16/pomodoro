using Focus.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Social.Queries;

public record IsRoomMemberQuery(Guid UserId, string RoomCode) : IRequest<bool>;

public class IsRoomMemberQueryHandler : IRequestHandler<IsRoomMemberQuery, bool>
{
    private readonly IApplicationDbContext _context;

    public IsRoomMemberQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public Task<bool> Handle(IsRoomMemberQuery request, CancellationToken cancellationToken)
    {
        var code = request.RoomCode.ToUpperInvariant().Trim();
        return _context.RoomMembers
            .AsNoTracking()
            .AnyAsync(m => m.UserId == request.UserId && m.Room.Code == code, cancellationToken);
    }
}
