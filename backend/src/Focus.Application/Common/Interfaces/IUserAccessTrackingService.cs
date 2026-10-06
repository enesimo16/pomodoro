namespace Focus.Application.Common.Interfaces;

public interface IUserAccessTrackingService
{
    Task RecordAccessAsync(
        Guid? userId,
        string ipAddress,
        string userAgent,
        string? path,
        CancellationToken cancellationToken = default);
}
