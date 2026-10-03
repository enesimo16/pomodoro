namespace Focus.Application.Common.Interfaces;

public record GoogleUserInfo(string Subject, string Email, string Name, string? Picture);

public interface IGoogleAuthService
{
    Task<GoogleUserInfo?> ValidateTokenAsync(string idToken, CancellationToken cancellationToken = default);
}
