using Focus.Application.Features.ExternalMedia.DTOs;

namespace Focus.Application.Common.Interfaces;

public interface IThemeVideoService
{
    Task<IReadOnlyList<ThemeVideoDto>> SearchVideosAsync(string query = "rain window", int perPage = 6, CancellationToken cancellationToken = default);
}
