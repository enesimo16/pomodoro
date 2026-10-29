using Focus.Application.Common.Interfaces;
using Focus.Application.Common.Options;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Focus.Application.Features.Session.Services;

public record SessionSettlement(int NetSeconds, int Xp, int Coins, User? User, bool LevelUp);

// Seans kapanisinda net sure, XP/Coin odulleri, seri kaydi ve birlikte calisma durumu tek yerde hesaplanir (SaveChanges cagiran tarafindadir)
public class SessionSettlementService
{
    private readonly IApplicationDbContext _context;
    private readonly AlgorithmOptions _options;

    public SessionSettlementService(IApplicationDbContext context, IOptions<AlgorithmOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    public async Task<SessionSettlement> CompleteAsync(FocusSession session, DateTime endedAt, CancellationToken cancellationToken)
    {
        var plannedSeconds = session.PlannedMinutes * 60;
        var netSeconds = session.GetNetSeconds(endedAt);
        if (session.Status == SessionStatus.Running && plannedSeconds - netSeconds <= _options.CompletionToleranceSeconds)
        {
            // Istemci ile sunucu saati arasindaki kucuk sapma tam sureyi etkilemesin
            netSeconds = plannedSeconds;
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == session.UserId, cancellationToken);
        var oldLevel = user?.Level ?? 1;
        var xp = 0;
        var coins = 0;
        var streakBonusCoins = 0;

        if (session.Kind == SessionKind.Focus)
        {
            var minutes = netSeconds / 60;
            if (minutes >= _options.MinRewardedFocusMinutes)
            {
                var streak = await GetOrCreateStreakAsync(session.UserId, cancellationToken);
                streakBonusCoins = streak.RecordActivity(endedAt).BonusCoins;

                var coWorkingPercent = await GetCoWorkingBonusPercentAsync(session.UserId, cancellationToken);
                var multiplier = _options.GetStreakMultiplier(streak.CurrentStreak) * (1m + coWorkingPercent / 100m);
                xp = (int)Math.Floor(minutes * _options.XpPerFocusMinute * multiplier);
                coins = (int)Math.Floor(minutes * _options.CoinsPerFocusMinute * multiplier);
            }
        }
        else if (await IsBreakRewardedAsync(session, netSeconds, cancellationToken))
        {
            xp = _options.BreakBonusXp;
            coins = _options.BreakBonusCoins;
        }

        coins = await ApplyDailyCapAsync(session.UserId, coins, endedAt, cancellationToken);
        session.Complete(netSeconds, xp, coins, endedAt);

        var reason = session.Kind == SessionKind.Focus ? CoinTransactionReason.SessionReward : CoinTransactionReason.BreakBonus;
        ApplyRewards(user, session, reason, xp, coins, streakBonusCoins, endedAt);
        await SetFocusingAsync(session.UserId, false, cancellationToken);

        return new SessionSettlement(netSeconds, xp, coins, user, user != null && user.Level > oldLevel);
    }

    // Terk edilen seans yalnizca yeterince uzunsa (docs 3) net dakika kadar odullendirilir; carpan ve seri uygulanmaz
    public async Task<SessionSettlement> AbandonAsync(FocusSession session, DateTime endedAt, CancellationToken cancellationToken)
    {
        var netSeconds = session.GetNetSeconds(endedAt);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == session.UserId, cancellationToken);
        var oldLevel = user?.Level ?? 1;
        var xp = 0;
        var coins = 0;

        var minutes = netSeconds / 60;
        if (session.Kind == SessionKind.Focus && minutes >= _options.AbandonRewardMinMinutes)
        {
            xp = minutes * _options.XpPerFocusMinute;
            coins = await ApplyDailyCapAsync(session.UserId, minutes * _options.CoinsPerFocusMinute, endedAt, cancellationToken);
        }

        session.Abandon(netSeconds, xp, coins, endedAt);
        ApplyRewards(user, session, CoinTransactionReason.SessionReward, xp, coins, 0, endedAt);
        await SetFocusingAsync(session.UserId, false, cancellationToken);

        return new SessionSettlement(netSeconds, xp, coins, user, user != null && user.Level > oldLevel);
    }

    // Odak seansi suresince kullanicinin en son aktif oda uyeligi "odaklaniyor" isaretlenir
    public async Task SetFocusingAsync(Guid userId, bool focusing, CancellationToken cancellationToken)
    {
        var memberships = await _context.RoomMembers
            .Where(m => m.UserId == userId)
            .ToListAsync(cancellationToken);

        if (memberships.Count == 0) return;

        if (!focusing)
        {
            foreach (var membership in memberships.Where(m => m.IsFocusing))
            {
                membership.SetFocusing(false);
            }

            return;
        }

        var primary = memberships
            .OrderByDescending(m => m.LastActiveAt ?? m.JoinedAt)
            .ThenByDescending(m => m.JoinedAt)
            .ThenBy(m => m.Id)
            .First();

        foreach (var membership in memberships)
        {
            if (membership == primary)
            {
                membership.SetFocusing(true);
            }
            else if (membership.IsFocusing)
            {
                membership.SetFocusing(false);
            }
        }
    }

    private void ApplyRewards(
        User? user,
        FocusSession session,
        CoinTransactionReason reason,
        int xp,
        int coins,
        int streakBonusCoins,
        DateTime at)
    {
        if (coins > 0)
        {
            _context.CoinLedgerEntries.Add(new CoinLedgerEntry(session.UserId, reason, coins, session.Id, at));
        }

        if (streakBonusCoins > 0)
        {
            _context.CoinLedgerEntries.Add(new CoinLedgerEntry(session.UserId, CoinTransactionReason.StreakReward, streakBonusCoins, session.Id, at));
        }

        if (user == null) return;

        user.AddXp(xp);
        if (coins > 0 || streakBonusCoins > 0)
        {
            user.RegisterWalletChange();
        }
    }

    private async Task<UserStreak> GetOrCreateStreakAsync(Guid userId, CancellationToken cancellationToken)
    {
        var streak = await _context.UserStreaks.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (streak != null) return streak;

        streak = UserStreak.CreateDefault(userId);
        _context.UserStreaks.Add(streak);
        return streak;
    }

    private async Task<int> GetCoWorkingBonusPercentAsync(Guid userId, CancellationToken cancellationToken)
    {
        var primary = await _context.RoomMembers
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.LastActiveAt ?? m.JoinedAt)
            .ThenByDescending(m => m.JoinedAt)
            .ThenBy(m => m.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (primary == null) return 0;

        var othersFocusing = await _context.RoomMembers
            .CountAsync(m => m.RoomId == primary.RoomId && m.UserId != userId && m.IsFocusing, cancellationToken);

        return StudyRoom.CoWorkingBonusPercentFor(othersFocusing + 1);
    }

    // Mola odulu: planlanan surenin en az %90 i tamamlanmali ve hemen oncesinde odul almis bir odak seansi bitmis olmali
    private async Task<bool> IsBreakRewardedAsync(FocusSession breakSession, int netSeconds, CancellationToken cancellationToken)
    {
        var requiredSeconds = (int)Math.Ceiling(breakSession.PlannedMinutes * 60 * _options.BreakMinCompletionRatio);
        if (netSeconds < requiredSeconds) return false;

        var previousFocus = await _context.FocusSessions
            .AsNoTracking()
            .Where(s => s.UserId == breakSession.UserId
                && s.Kind == SessionKind.Focus
                && s.Status == SessionStatus.Completed
                && s.XpEarned > 0
                && s.EndedAt != null
                && s.EndedAt <= breakSession.StartedAt)
            .OrderByDescending(s => s.EndedAt)
            .Select(s => new { s.EndedAt })
            .FirstOrDefaultAsync(cancellationToken);

        if (previousFocus?.EndedAt == null) return false;

        var gap = breakSession.StartedAt - previousFocus.EndedAt.Value;
        if (gap > TimeSpan.FromMinutes(_options.BreakMaxGapMinutes)) return false;

        var focusEndedAt = previousFocus.EndedAt.Value;
        var alreadyRewarded = await _context.FocusSessions
            .AnyAsync(s => s.UserId == breakSession.UserId
                && s.Id != breakSession.Id
                && s.Kind == SessionKind.Break
                && s.Status == SessionStatus.Completed
                && s.XpEarned > 0
                && s.StartedAt >= focusEndedAt, cancellationToken);

        return !alreadyRewarded;
    }

    private async Task<int> ApplyDailyCapAsync(Guid userId, int coins, DateTime at, CancellationToken cancellationToken)
    {
        if (coins <= 0) return 0;

        var dayStart = at.Date;
        var dayEnd = dayStart.AddDays(1);
        var earnedToday = await _context.CoinLedgerEntries
            .Where(c => c.UserId == userId
                && (c.Reason == CoinTransactionReason.SessionReward || c.Reason == CoinTransactionReason.BreakBonus)
                && c.CreatedAt >= dayStart
                && c.CreatedAt < dayEnd)
            .SumAsync(c => (int?)c.Coins, cancellationToken) ?? 0;

        return Math.Clamp(_options.DailyCoinCap - earnedToday, 0, coins);
    }
}
