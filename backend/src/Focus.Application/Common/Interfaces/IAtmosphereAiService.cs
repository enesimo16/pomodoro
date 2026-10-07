namespace Focus.Application.Common.Interfaces;

using Focus.Application.Features.Atmosphere.DTOs;

public interface IAtmosphereAiService
{
    Task<GeneratedAtmosphereDto> GenerateAtmosphereAsync(
        Guid userId,
        GenerateAtmosphereRequestDto request,
        CancellationToken cancellationToken = default);

    Task<GeneratedAtmosphereDto> GenerateAutoAtmosphereAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> ContributeSessionInsightAsync(
        Guid userId,
        Guid? sessionId = null,
        int? plannedDurationMinutes = null,
        int? focusQuality = null,
        string? notes = null,
        CancellationToken cancellationToken = default);
}
