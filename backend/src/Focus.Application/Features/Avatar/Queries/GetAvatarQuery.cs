using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Auth.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Avatar.Queries;

public record GetAvatarQuery(Guid UserId) : IRequest<AvatarDto?>;

public class GetAvatarQueryHandler : IRequestHandler<GetAvatarQuery, AvatarDto?>
{
    private readonly IApplicationDbContext _context;

    public GetAvatarQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AvatarDto?> Handle(GetAvatarQuery request, CancellationToken cancellationToken)
    {
        var avatar = await _context.UserAvatars
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == request.UserId, cancellationToken);

        return avatar != null ? AvatarDto.FromEntity(avatar) : null;
    }
}
