using Focus.Application.Features.Wrapped.DTOs;

namespace Focus.Application.Common.Interfaces;

public interface IFocusWrappedService
{
    Task<FocusWrappedDto> GetWeeklyWrappedAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<FocusWrappedDto> GetMonthlyWrappedAsync(Guid userId, CancellationToken cancellationToken = default);
}
