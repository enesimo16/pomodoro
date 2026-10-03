using Focus.Domain.Entities;

namespace Focus.Application.Common.Interfaces;

public interface IJwtTokenService
{
    (string AccessToken, string RefreshToken, DateTime ExpiresAt) GenerateTokens(User user);
    string HashToken(string token);
}
