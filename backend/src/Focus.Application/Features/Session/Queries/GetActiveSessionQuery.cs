using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Session.DTOs;
using Focus.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Session.Queries;

public record GetActiveSessionQuery(Guid UserId) : IRequest<FocusSessionDto?>;

public class GetActiveSessionQueryHandler : IRequestHandler<GetActiveSessionQuery, FocusSessionDto?>
{
    private readonly IApplicationDbContext _context;

    public GetActiveSessionQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FocusSessionDto?> Handle(GetActiveSessionQuery request, CancellationToken cancellationToken)
    {
        var session = await _context.FocusSessions
            .AsNoTracking()
            .Where(s => s.UserId == request.UserId && (s.Status == SessionStatus.Running || s.Status == SessionStatus.Paused))
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return session != null ? FocusSessionDto.FromEntity(session) : null;
    }
}
