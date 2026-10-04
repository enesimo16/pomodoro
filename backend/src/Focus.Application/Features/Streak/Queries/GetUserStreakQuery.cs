using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Streak.DTOs;
using Focus.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Streak.Queries;

public record GetUserStreakQuery(Guid UserId) : IRequest<UserStreakDto>;

public class GetUserStreakQueryHandler : IRequestHandler<GetUserStreakQuery, UserStreakDto>
{
    private readonly IApplicationDbContext _context;

    public GetUserStreakQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserStreakDto> Handle(GetUserStreakQuery request, CancellationToken cancellationToken)
    {
        var streak = await _context.UserStreaks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == request.UserId, cancellationToken);

        if (streak == null)
        {
            return new UserStreakDto(
                CurrentStreak: 0,
                LongestStreak: 0,
                LastActivityDate: null,
                FreezesAvailable: 1,
                LastFreezeUsedDate: null,
                IsActiveToday: false,
                IsStreakAtRisk: false,
                NextMilestoneDays: 3,
                NextMilestoneBonus: 15);
        }

        var today = DateTime.UtcNow.Date;
        var isActiveToday = streak.LastActivityDate.HasValue && streak.LastActivityDate.Value.Date == today;
        var isAtRisk = streak.CurrentStreak > 0 && !isActiveToday &&
                       streak.LastActivityDate.HasValue && streak.LastActivityDate.Value.Date == today.AddDays(-1);

        var (nextMilestone, nextBonus) = GetNextMilestone(streak.CurrentStreak);

        return new UserStreakDto(
            streak.CurrentStreak,
            streak.LongestStreak,
            streak.LastActivityDate,
            streak.FreezesAvailable,
            streak.LastFreezeUsedDate,
            isActiveToday,
            isAtRisk,
            nextMilestone,
            nextBonus);
    }

    private static (int Days, int Bonus) GetNextMilestone(int currentStreak)
    {
        if (currentStreak < 3) return (3, 15);
        if (currentStreak < 7) return (7, 50);
        if (currentStreak < 14) return (14, 120);
        if (currentStreak < 30) return (30, 300);
        var next30 = ((currentStreak / 30) + 1) * 30;
        return (next30, 300);
    }
}
