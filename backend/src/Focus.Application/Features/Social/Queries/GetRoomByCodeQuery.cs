using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Social.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Social.Queries;

public record GetRoomByCodeQuery(string Code, Guid RequesterUserId) : IRequest<StudyRoomDto?>;

public class GetRoomByCodeQueryHandler : IRequestHandler<GetRoomByCodeQuery, StudyRoomDto?>
{
    private readonly IApplicationDbContext _context;

    public GetRoomByCodeQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StudyRoomDto?> Handle(GetRoomByCodeQuery request, CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant().Trim();

        var room = await _context.StudyRooms
            .AsNoTracking()
            .Include(r => r.OwnerUser)
            .Include(r => r.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

        if (room == null) return null;

        // Ozel odalar yalnizca sahibi ve uyelerine gorunur
        var canView = !room.IsPrivate
            || room.OwnerUserId == request.RequesterUserId
            || room.HasMember(request.RequesterUserId);

        return canView ? StudyRoomDto.FromEntity(room) : null;
    }
}
