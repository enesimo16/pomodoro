using Focus.Application.Features.ExternalMedia.DTOs;

namespace Focus.Application.Common.Interfaces;

public interface IThemeVideoService
{
    Task<IReadOnlyList<ThemeVideoDto>> SearchVideosAsync(string query = "rain window", int perPage = 12, int page = 1, CancellationToken cancellationToken = default);
}
