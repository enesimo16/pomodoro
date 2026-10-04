using Focus.Application.Features.ExternalMedia.DTOs;

namespace Focus.Application.Common.Interfaces;

public interface IMusicTrackService
{
    Task<IReadOnlyList<MusicTrackDto>> GetLofiTracksAsync(int limit = 10, CancellationToken cancellationToken = default);
}
