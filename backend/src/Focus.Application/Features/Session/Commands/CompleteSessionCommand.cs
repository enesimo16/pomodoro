using Focus.Application.Common.Extensions;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Session.DTOs;
using Focus.Application.Features.Session.Services;
using Focus.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Session.Commands;

public record CompleteSessionCommand(Guid UserId, Guid? SessionId = null) : IRequest<SessionCompletionResultDto?>;

public class CompleteSessionCommandHandler : IRequestHandler<CompleteSessionCommand, SessionCompletionResultDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly SessionSettlementService _settlement;
    private readonly TimeProvider _clock;

    public CompleteSessionCommandHandler(IApplicationDbContext context, SessionSettlementService settlement, TimeProvider clock)
    {
        _context = context;
        _settlement = settlement;
        _clock = clock;
    }

    public async Task<SessionCompletionResultDto?> Handle(CompleteSessionCommand request, CancellationToken cancellationToken)
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
        var settlement = await _settlement.CompleteAsync(session, now, cancellationToken);

        await _context.SaveChangesOrConflictAsync(
            cancellationToken,
            "Seans başka bir istek tarafından zaten tamamlandı.",
            treatConstraintViolationAsConflict: true);

        var totalCoins = await _context.CoinLedgerEntries
            .Where(c => c.UserId == request.UserId)
            .SumAsync(c => (int?)c.Coins, cancellationToken) ?? 0;

        var (nextKind, nextRound, isCycleFinished) = session.GetNextCycleStep();

        return new SessionCompletionResultDto(
            FocusSessionDto.FromEntity(session, now),
            settlement.Xp,
            settlement.Coins,
            settlement.User?.Level ?? 1,
            settlement.User?.CurrentXp ?? 0,
            totalCoins,
            settlement.LevelUp,
            nextKind.ToString(),
            nextRound,
            isCycleFinished);
    }
}
