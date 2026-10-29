namespace Focus.Application.Features.Atmosphere.Commands;

using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Atmosphere.DTOs;
using Focus.Application.Features.Room;
using MediatR;
using Microsoft.EntityFrameworkCore;

public record ApplyAtmosphereCommand(
    Guid UserId,
    ApplyAtmosphereRequestDto Request
) : IRequest<bool>;

public class ApplyAtmosphereCommandHandler : IRequestHandler<ApplyAtmosphereCommand, bool>
{
    private const int MinFocusMinutes = 5;
    private const int MaxFocusMinutes = 180;
    private const int MinBreakMinutes = 1;
    private const int MaxBreakMinutes = 60;

    private readonly IApplicationDbContext _context;

    public ApplyAtmosphereCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(ApplyAtmosphereCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        var applied = false;
        var wallPaperId = !string.IsNullOrWhiteSpace(req.WallPaperId)
            ? req.WallPaperId
            : req.Atmosphere?.VisualTheme?.WallPaperId;
        var floorId = !string.IsNullOrWhiteSpace(req.FloorId)
            ? req.FloorId
            : req.Atmosphere?.VisualTheme?.FloorId;
        var focusMinutes = ClampOrNull(req.FocusMinutes ?? req.Atmosphere?.RecommendedSession?.DurationMinutes, MinFocusMinutes, MaxFocusMinutes);
        var breakMinutes = ClampOrNull(req.BreakMinutes ?? req.Atmosphere?.RecommendedSession?.BreakMinutes, MinBreakMinutes, MaxBreakMinutes);
        var flowShield = req.FlowShieldEnabled ?? (req.Atmosphere?.RecommendedSession != null ? true : null);

        // 1. Odaya Duvar/Zemin temasini uygula
        var room = req.RoomId.HasValue
            ? await _context.PixelRooms.FirstOrDefaultAsync(r => r.Id == req.RoomId.Value && r.OwnerUserId == command.UserId, cancellationToken)
            : await _context.PixelRooms.FirstOrDefaultAsync(r => r.OwnerUserId == command.UserId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(wallPaperId))
        {
            await CatalogOwnership.RequireOwnedAsync(_context, command.UserId, wallPaperId, CatalogOwnership.WallpaperCategories, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(floorId))
        {
            await CatalogOwnership.RequireOwnedAsync(_context, command.UserId, floorId, CatalogOwnership.FloorCategories, cancellationToken);
        }

        if (room != null && (!string.IsNullOrWhiteSpace(wallPaperId) || !string.IsNullOrWhiteSpace(floorId)))
        {
            room.UpdateTheme(wallPaperId, floorId);
            applied = true;
        }

        // 2. Kullanici tercihlerine seans suresi ve flow shield'i uygula
        var preferences = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == command.UserId, cancellationToken);

        if (preferences != null)
        {
            var newFocus = focusMinutes ?? preferences.DefaultFocusMinutes;
            var newBreak = breakMinutes ?? preferences.ShortBreakMinutes;
            var newShield = flowShield ?? preferences.FlowShieldEnabled;

            preferences.UpdatePreferences(
                defaultFocusMinutes: newFocus,
                shortBreakMinutes: newBreak,
                longBreakMinutes: preferences.LongBreakMinutes,
                flowShieldEnabled: newShield,
                weatherSyncEnabled: preferences.WeatherSyncEnabled,
                cityKey: preferences.CityKey,
                agentEnabled: preferences.AgentEnabled,
                targetRounds: preferences.TargetRounds);
            applied = true;
        }

        if (!applied)
        {
            return false;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static int? ClampOrNull(int? value, int min, int max) =>
        value.HasValue ? Math.Clamp(value.Value, min, max) : null;
}
