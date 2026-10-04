using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Session.DTOs;
using Focus.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Session.Commands;

public record PauseSessionCommand(Guid UserId, Guid? SessionId = null) : IRequest<FocusSessionDto?>;

public class PauseSessionCommandHandler : IRequestHandler<PauseSessionCommand, FocusSessionDto?>
{
    private readonly IApplicationDbContext _context;

    public PauseSessionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FocusSessionDto?> Handle(PauseSessionCommand request, CancellationToken cancellationToken)
    {
        var query = _context.FocusSessions
            .Where(s => s.UserId == request.UserId && s.Status == SessionStatus.Running);

        if (request.SessionId.HasValue)
        {
            query = query.Where(s => s.Id == request.SessionId.Value);
        }

        var session = await query.OrderByDescending(s => s.StartedAt).FirstOrDefaultAsync(cancellationToken);
        if (session == null)
        {
            return null;
        }

        session.Pause();
        await _context.SaveChangesAsync(cancellationToken);

        return FocusSessionDto.FromEntity(session);
    }
}
