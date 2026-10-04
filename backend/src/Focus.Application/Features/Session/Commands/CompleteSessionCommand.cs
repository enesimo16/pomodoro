using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Session.DTOs;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Session.Commands;

public record CompleteSessionCommand(Guid UserId, Guid? SessionId = null) : IRequest<SessionCompletionResultDto?>;

public class CompleteSessionCommandHandler : IRequestHandler<CompleteSessionCommand, SessionCompletionResultDto?>
{
    private readonly IApplicationDbContext _context;

    public CompleteSessionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
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

        var now = DateTime.UtcNow;
        var totalElapsedSeconds = (int)(now - session.StartedAt).TotalSeconds;
        var currentPauseSeconds = session.TotalPausedSeconds;

        if (session.Status == SessionStatus.Paused && session.PausedAt.HasValue)
        {
            currentPauseSeconds += (int)(now - session.PausedAt.Value).TotalSeconds;
        }

        var netSeconds = Math.Max(0, totalElapsedSeconds - currentPauseSeconds);

        int xpEarned;
        int coinsEarned;
        CoinTransactionReason reason;

        if (session.Kind == SessionKind.Focus)
        {
            var minutes = Math.Max(1, netSeconds / 60);
            xpEarned = minutes * 10;
            coinsEarned = minutes * 1;
            reason = CoinTransactionReason.SessionReward;
        }
        else
        {
            xpEarned = 50;
            coinsEarned = 10;
            reason = CoinTransactionReason.BreakBonus;
        }

        session.Complete(netSeconds, xpEarned, coinsEarned);

        // Kullaniciyi guncelle
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        var oldLevel = user?.Level ?? 1;
        if (user != null)
        {
            user.AddXp(xpEarned);
        }

        // Coin Ledger
        if (coinsEarned > 0)
        {
            var ledgerEntry = new CoinLedgerEntry(request.UserId, reason, coinsEarned, session.Id);
            _context.CoinLedgerEntries.Add(ledgerEntry);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var totalCoins = await _context.CoinLedgerEntries
            .Where(c => c.UserId == request.UserId)
            .SumAsync(c => c.Coins, cancellationToken);

        var (nextKind, nextRound, isCycleFinished) = session.GetNextCycleStep();
        var sessionDto = FocusSessionDto.FromEntity(session);

        return new SessionCompletionResultDto(
            sessionDto,
            xpEarned,
            coinsEarned,
            user?.Level ?? 1,
            user?.CurrentXp ?? 0,
            totalCoins,
            (user?.Level ?? 1) > oldLevel,
            nextKind.ToString(),
            nextRound,
            isCycleFinished);
    }
}
