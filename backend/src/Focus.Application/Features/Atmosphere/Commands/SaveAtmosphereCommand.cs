namespace Focus.Application.Features.Atmosphere.Commands;

using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Atmosphere.DTOs;
using Focus.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

public record SaveAtmosphereCommand(
    Guid UserId,
    GeneratedAtmosphereDto Atmosphere,
    string? CustomName = null
) : IRequest<SavedAtmosphereDto>;

public class SaveAtmosphereCommandHandler : IRequestHandler<SaveAtmosphereCommand, SavedAtmosphereDto>
{
    private readonly IApplicationDbContext _context;

    public SaveAtmosphereCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SavedAtmosphereDto> Handle(SaveAtmosphereCommand command, CancellationToken cancellationToken)
    {
        var atm = command.Atmosphere;
        var name = !string.IsNullOrWhiteSpace(command.CustomName) ? command.CustomName.Trim() : atm.ThemeName;

        var entity = SavedAtmosphere.Create(
            userId: command.UserId,
            name: name,
            description: atm.Description,
            aesthetic: atm.VisualTheme.Aesthetic,
            wallColor: atm.VisualTheme.WallColor,
            floorColor: atm.VisualTheme.FloorColor,
            accentLightColor: atm.VisualTheme.AccentLightColor,
            wallPaperId: atm.VisualTheme.WallPaperId,
            floorId: atm.VisualTheme.FloorId,
            weatherEffect: atm.VisualTheme.WeatherEffect,
            windowVideoQuery: atm.VisualTheme.WindowVideoQuery,
            musicGenre: atm.SoundscapeMixer.Layer1Melody.Genre,
            musicSearchQuery: atm.SoundscapeMixer.Layer1Melody.SearchQuery,
            musicVolume: atm.SoundscapeMixer.Layer1Melody.Volume,
            ambienceType: atm.SoundscapeMixer.Layer2Ambience.AmbienceType,
            ambienceVolume: atm.SoundscapeMixer.Layer2Ambience.Volume,
            noiseType: atm.SoundscapeMixer.Layer3Noise.NoiseType,
            noiseVolume: atm.SoundscapeMixer.Layer3Noise.Volume,
            cutoffFrequencyHz: atm.SoundscapeMixer.Layer3Noise.CutoffFrequencyHz,
            textureType: atm.SoundscapeMixer.Layer4Texture.TextureType,
            textureVolume: atm.SoundscapeMixer.Layer4Texture.Volume,
            recommendedMinutes: atm.RecommendedSession.DurationMinutes,
            breakMinutes: atm.RecommendedSession.BreakMinutes,
            flowShieldLevel: atm.RecommendedSession.FlowShieldLevel,
            isAiGenerated: true,
            promptUsed: atm.Description);

        _context.SavedAtmospheres.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return new SavedAtmosphereDto(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.Aesthetic,
            entity.WallColor,
            entity.FloorColor,
            entity.AccentLightColor,
            entity.WallPaperId,
            entity.FloorId,
            entity.WeatherEffect,
            entity.WindowVideoQuery,
            entity.MusicGenre,
            entity.MusicSearchQuery,
            entity.MusicVolume,
            entity.AmbienceType,
            entity.AmbienceVolume,
            entity.NoiseType,
            entity.NoiseVolume,
            entity.CutoffFrequencyHz,
            entity.TextureType,
            entity.TextureVolume,
            entity.RecommendedMinutes,
            entity.BreakMinutes,
            entity.FlowShieldLevel,
            entity.IsAiGenerated,
            entity.PromptUsed,
            entity.CreatedAt);
    }
}
