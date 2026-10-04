using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Session.DTOs;
using Focus.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Session.Commands;

public record AbandonSessionCommand(Guid UserId, Guid? SessionId = null) : IRequest<FocusSessionDto?>;

public class AbandonSessionCommandHandler : IRequestHandler<AbandonSessionCommand, FocusSessionDto?>
{
    private readonly IApplicationDbContext _context;

    public AbandonSessionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FocusSessionDto?> Handle(AbandonSessionCommand request, CancellationToken cancellationToken)
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

        var now = DateTime.UtcNow;
        var totalElapsedSeconds = (int)(now - session.StartedAt).TotalSeconds;
        var currentPauseSeconds = session.TotalPausedSeconds;

        if (session.Status == SessionStatus.Paused && session.PausedAt.HasValue)
        {
            currentPauseSeconds += (int)(now - session.PausedAt.Value).TotalSeconds;
        }

        var netSeconds = Math.Max(0, totalElapsedSeconds - currentPauseSeconds);

        session.Abandon(netSeconds, xp: 0, coins: 0);
        await _context.SaveChangesAsync(cancellationToken);

        return FocusSessionDto.FromEntity(session);
    }
}
