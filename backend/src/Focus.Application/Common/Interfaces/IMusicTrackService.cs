using Focus.Application.Features.ExternalMedia.DTOs;

namespace Focus.Application.Common.Interfaces;

public interface IMusicTrackService
{
    Task<IReadOnlyList<MusicTrackDto>> SearchTracksAsync(string query = "lofi", int limit = 12, int page = 1, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MusicTrackDto>> GetLofiTracksAsync(int limit = 12, int page = 1, CancellationToken cancellationToken = default);
}
