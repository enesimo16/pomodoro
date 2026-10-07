namespace Focus.Application.Features.Atmosphere.Queries;

using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Atmosphere.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

public record GetSavedAtmospheresQuery(Guid UserId) : IRequest<List<SavedAtmosphereDto>>;

public class GetSavedAtmospheresQueryHandler : IRequestHandler<GetSavedAtmospheresQuery, List<SavedAtmosphereDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSavedAtmospheresQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SavedAtmosphereDto>> Handle(GetSavedAtmospheresQuery query, CancellationToken cancellationToken)
    {
        return await _context.SavedAtmospheres
            .Where(a => a.UserId == query.UserId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new SavedAtmosphereDto(
                a.Id,
                a.Name,
                a.Description,
                a.Aesthetic,
                a.WallColor,
                a.FloorColor,
                a.AccentLightColor,
                a.WallPaperId,
                a.FloorId,
                a.WeatherEffect,
                a.WindowVideoQuery,
                a.MusicGenre,
                a.MusicSearchQuery,
                a.MusicVolume,
                a.AmbienceType,
                a.AmbienceVolume,
                a.NoiseType,
                a.NoiseVolume,
                a.CutoffFrequencyHz,
                a.TextureType,
                a.TextureVolume,
                a.RecommendedMinutes,
                a.BreakMinutes,
                a.FlowShieldLevel,
                a.IsAiGenerated,
                a.PromptUsed,
                a.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
