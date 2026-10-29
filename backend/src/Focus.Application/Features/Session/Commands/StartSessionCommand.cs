using Focus.Application.Common.Extensions;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Session.DTOs;
using Focus.Application.Features.Session.Services;
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
    private const string ActiveSessionConflictMessage = "Zaten aktif bir seansın var.";

    private readonly IApplicationDbContext _context;
    private readonly SessionSettlementService _settlement;
    private readonly TimeProvider _clock;

    public StartSessionCommandHandler(IApplicationDbContext context, SessionSettlementService settlement, TimeProvider clock)
    {
        _context = context;
        _settlement = settlement;
        _clock = clock;
    }

    public async Task<FocusSessionDto> Handle(StartSessionCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        // 1. Varsa eski aktif seansi gercek net sureyle sonlandir (benzersiz aktif seans indeksi icin ayri kayit)
        var activeSessions = await _context.FocusSessions
            .Where(s => s.UserId == request.UserId && (s.Status == SessionStatus.Running || s.Status == SessionStatus.Paused))
            .ToListAsync(cancellationToken);

        if (activeSessions.Count > 0)
        {
            foreach (var oldSession in activeSessions)
            {
                await _settlement.AbandonAsync(oldSession, now, cancellationToken);
            }

            await _context.SaveChangesOrConflictAsync(cancellationToken, ActiveSessionConflictMessage, treatConstraintViolationAsConflict: true);
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
            request.MixPresetId,
            now);

        _context.FocusSessions.Add(session);

        if (request.Kind == SessionKind.Focus)
        {
            await _settlement.SetFocusingAsync(request.UserId, true, cancellationToken);
        }

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

        await _context.SaveChangesOrConflictAsync(cancellationToken, ActiveSessionConflictMessage, treatConstraintViolationAsConflict: true);

        return FocusSessionDto.FromEntity(session, now);
    }
}
