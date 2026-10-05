using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Coach.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Coach.Queries;

public record GetCoachAnomalyStatusQuery(Guid UserId) : IRequest<CoachAnomalyStatusDto>;

public class GetCoachAnomalyStatusQueryHandler : IRequestHandler<GetCoachAnomalyStatusQuery, CoachAnomalyStatusDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IFocusCoachEngine _coachEngine;

    public GetCoachAnomalyStatusQueryHandler(
        IApplicationDbContext context,
        IFocusCoachEngine coachEngine)
    {
        _context = context;
        _coachEngine = coachEngine;
    }

    public async Task<CoachAnomalyStatusDto> Handle(GetCoachAnomalyStatusQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException("Kullanici bulunamadi.");
        }

        var sinceDate = DateTime.UtcNow.AddDays(-14);

        var recentSessions = await _context.FocusSessions
            .Where(s => s.UserId == request.UserId && s.StartedAt >= sinceDate)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(cancellationToken);

        var recentCheckIns = await _context.SessionCheckIns
            .Where(c => c.UserId == request.UserId && c.CreatedAt >= sinceDate)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var recentReflections = await _context.SessionReflections
            .Where(r => r.UserId == request.UserId && r.CreatedAt >= sinceDate)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        var streak = await _context.UserStreaks
            .FirstOrDefaultAsync(s => s.UserId == request.UserId, cancellationToken);

        return _coachEngine.EvaluateAnomalies(user, recentSessions, recentCheckIns, recentReflections, streak);
    }
}
