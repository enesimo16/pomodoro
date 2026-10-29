using Focus.Application.Common.Extensions;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Session.DTOs;
using Focus.Application.Features.Session.Services;
using Focus.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Session.Commands;

public record ResumeSessionCommand(Guid UserId, Guid? SessionId = null) : IRequest<FocusSessionDto?>;

public class ResumeSessionCommandHandler : IRequestHandler<ResumeSessionCommand, FocusSessionDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly SessionSettlementService _settlement;
    private readonly TimeProvider _clock;

    public ResumeSessionCommandHandler(IApplicationDbContext context, SessionSettlementService settlement, TimeProvider clock)
    {
        _context = context;
        _settlement = settlement;
        _clock = clock;
    }

    public async Task<FocusSessionDto?> Handle(ResumeSessionCommand request, CancellationToken cancellationToken)
    {
        var query = _context.FocusSessions
            .Where(s => s.UserId == request.UserId && s.Status == SessionStatus.Paused);

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
        session.Resume(now);

        if (session.Kind == SessionKind.Focus)
        {
            await _settlement.SetFocusingAsync(request.UserId, true, cancellationToken);
        }

        await _context.SaveChangesOrConflictAsync(cancellationToken);

        return FocusSessionDto.FromEntity(session, now);
    }
}
