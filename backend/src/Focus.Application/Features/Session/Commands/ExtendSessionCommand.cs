using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Session.DTOs;
using Focus.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Session.Commands;

public record ExtendSessionCommand(Guid UserId, int ExtraMinutes = 10, Guid? SessionId = null) : IRequest<FocusSessionDto?>;

public class ExtendSessionCommandHandler : IRequestHandler<ExtendSessionCommand, FocusSessionDto?>
{
    private readonly IApplicationDbContext _context;

    public ExtendSessionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FocusSessionDto?> Handle(ExtendSessionCommand request, CancellationToken cancellationToken)
    {
        var query = _context.FocusSessions
            .Where(s => s.UserId == request.UserId && (s.Status == SessionStatus.Running || s.Status == SessionStatus.Paused));

        if (request.SessionId.HasValue)
        {
            query = query.Where(s => s.Id == request.SessionId.Value);
        }

        var session = await query.OrderByDescending(s => s.StartedAt).FirstOrDefaultAsync(cancellationToken);
        if (session == null)
        {
            return null;
        }

        session.Extend(request.ExtraMinutes);
        await _context.SaveChangesAsync(cancellationToken);

        return FocusSessionDto.FromEntity(session);
    }
}
