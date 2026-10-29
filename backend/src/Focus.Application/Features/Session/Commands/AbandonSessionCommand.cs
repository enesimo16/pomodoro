using Focus.Application.Common.Extensions;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Session.DTOs;
using Focus.Application.Features.Session.Services;
using Focus.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Session.Commands;

public record AbandonSessionCommand(Guid UserId, Guid? SessionId = null) : IRequest<FocusSessionDto?>;

public class AbandonSessionCommandHandler : IRequestHandler<AbandonSessionCommand, FocusSessionDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly SessionSettlementService _settlement;
    private readonly TimeProvider _clock;

    public AbandonSessionCommandHandler(IApplicationDbContext context, SessionSettlementService settlement, TimeProvider clock)
    {
        _context = context;
        _settlement = settlement;
        _clock = clock;
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

        var now = _clock.GetUtcNow().UtcDateTime;
        await _settlement.AbandonAsync(session, now, cancellationToken);

        await _context.SaveChangesOrConflictAsync(
            cancellationToken,
            "Seans başka bir istek tarafından zaten sonlandırıldı.",
            treatConstraintViolationAsConflict: true);

        return FocusSessionDto.FromEntity(session, now);
    }
}
