using Focus.Application.Common.Extensions;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Session.DTOs;
using Focus.Application.Features.Session.Services;
using Focus.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Session.Commands;

public record PauseSessionCommand(Guid UserId, Guid? SessionId = null) : IRequest<FocusSessionDto?>;

public class PauseSessionCommandHandler : IRequestHandler<PauseSessionCommand, FocusSessionDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly SessionSettlementService _settlement;
    private readonly TimeProvider _clock;

    public PauseSessionCommandHandler(IApplicationDbContext context, SessionSettlementService settlement, TimeProvider clock)
    {
        _context = context;
        _settlement = settlement;
        _clock = clock;
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

        var now = _clock.GetUtcNow().UtcDateTime;
        session.Pause(now);
        await _settlement.SetFocusingAsync(request.UserId, false, cancellationToken);
        await _context.SaveChangesOrConflictAsync(cancellationToken);

        return FocusSessionDto.FromEntity(session, now);
    }
}
