using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Focus.Domain.Entities;
using Focus.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Xunit;

namespace Focus.UnitTests.Auth;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _service;
    private readonly JwtSettings _settings;

    public JwtTokenServiceTests()
    {
        _settings = new JwtSettings
        {
            SigningKey = "test_super_secret_signing_key_at_least_32_bytes_long_123456",
            Issuer = "focus-api-test",
            Audience = "focus-web-test",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 30
        };

        _service = new JwtTokenService(Options.Create(_settings));
    }

    [Fact]
    public void GenerateTokens_ShouldReturnValidTokens_WithCorrectClaims()
    {
        // Arrange
        var user = User.CreateGuest("DenemeKullanici");

        // Act
        var (accessToken, refreshToken, expiresAt) = _service.GenerateTokens(user);

        // Assert
        accessToken.Should().NotBeNullOrWhiteSpace();
        refreshToken.Should().NotBeNullOrWhiteSpace();
        expiresAt.Should().BeAfter(DateTime.UtcNow);

        // Token cozumleme ve claim kontrolu
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(accessToken);

        jwtToken.Issuer.Should().Be(_settings.Issuer);
        jwtToken.Audiences.Should().Contain(_settings.Audience);

        var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        subClaim.Should().NotBeNull();
        subClaim!.Value.Should().Be(user.Id.ToString());

        var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Name);
        nameClaim.Should().NotBeNull();
        nameClaim!.Value.Should().Be("DenemeKullanici");

        var isGuestClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "is_guest");
        isGuestClaim.Should().NotBeNull();
        isGuestClaim!.Value.Should().Be("true");
    }

    [Fact]
    public void HashToken_ShouldProduceConsistentSha256HexString()
    {
        // Arrange
        var token = "sample_raw_refresh_token_string_abc_123";

        // Act
        var hash1 = _service.HashToken(token);
        var hash2 = _service.HashToken(token);

        // Assert
        hash1.Should().NotBeNullOrWhiteSpace();
        hash1.Length.Should().Be(64); // SHA-256 hex uzunlugu
        hash1.Should().Be(hash2);
    }
}
