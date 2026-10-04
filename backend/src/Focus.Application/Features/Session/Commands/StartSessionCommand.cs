using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Session.DTOs;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Session.Commands;

public record StartSessionCommand(
    Guid UserId,
    SessionKind Kind,
    int FocusMinutes,
    int BreakMinutes,
    int CurrentRound = 1,
    int TargetRounds = 4,
    string? ThemeId = null,
    string? MixPresetId = null) : IRequest<FocusSessionDto>;

public class StartSessionCommandHandler : IRequestHandler<StartSessionCommand, FocusSessionDto>
{
    private readonly IApplicationDbContext _context;

    public StartSessionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FocusSessionDto> Handle(StartSessionCommand request, CancellationToken cancellationToken)
    {
        // 1. Varsa eski aktif seansi sonlandir
        var activeSessions = await _context.FocusSessions
            .Where(s => s.UserId == request.UserId && (s.Status == SessionStatus.Running || s.Status == SessionStatus.Paused))
            .ToListAsync(cancellationToken);

        foreach (var oldSession in activeSessions)
        {
            var diff = (int)(DateTime.UtcNow - oldSession.StartedAt).TotalSeconds;
            oldSession.Abandon(Math.Max(0, diff - oldSession.TotalPausedSeconds));
        }

        // 2. Kullanicinin odasini bul
        var room = await _context.PixelRooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.OwnerUserId == request.UserId, cancellationToken);

        // 3. Yeni seansi baslat
        var session = FocusSession.Start(
            request.UserId,
            room?.Id,
            request.Kind,
            request.FocusMinutes,
            request.BreakMinutes,
            request.CurrentRound,
            request.TargetRounds,
            request.ThemeId,
            request.MixPresetId);

        _context.FocusSessions.Add(session);

        // 4. Kullanicinin tercihlerine secilen sureleri varsayilan olarak kaydet
        var preferences = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken);

        if (preferences != null)
        {
            preferences.UpdatePreferences(
                request.FocusMinutes,
                request.BreakMinutes,
                preferences.LongBreakMinutes,
                preferences.FlowShieldEnabled,
                preferences.WeatherSyncEnabled,
                preferences.CityKey,
                preferences.AgentEnabled,
                request.TargetRounds);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return FocusSessionDto.FromEntity(session);
    }
}
