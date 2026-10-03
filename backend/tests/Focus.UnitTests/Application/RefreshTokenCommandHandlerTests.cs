using FluentAssertions;
using Focus.Application.Features.Auth.Commands;
using Focus.Domain.Entities;
using Focus.Infrastructure.Authentication;
using Focus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Focus.UnitTests.Application;

public class RefreshTokenCommandHandlerTests
{
    private ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_ShouldRotateRefreshToken_WhenTokenIsValid()
    {
        // Arrange
        using var context = CreateDbContext();
        var jwtSettings = new JwtSettings
        {
            SigningKey = "test_super_secret_signing_key_at_least_32_bytes_long_123456",
            Issuer = "focus-api-test",
            Audience = "focus-web-test"
        };
        var jwtService = new JwtTokenService(Options.Create(jwtSettings));

        var user = User.CreateGuest("Mert");
        var rawToken = "initial_refresh_token_xyz_123";
        var tokenHash = jwtService.HashToken(rawToken);

        user.AddRefreshToken(tokenHash, DateTime.UtcNow.AddDays(30), "127.0.0.1");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new RefreshTokenCommandHandler(context, jwtService);
        var command = new RefreshTokenCommand(rawToken, "127.0.0.1");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RefreshToken.Should().NotBe(rawToken);

        // Eski token revoke edilmis olmali
        var oldToken = await context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        oldToken.Should().NotBeNull();
        oldToken!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenTokenIsExpired()
    {
        // Arrange
        using var context = CreateDbContext();
        var jwtSettings = new JwtSettings
        {
            SigningKey = "test_super_secret_signing_key_at_least_32_bytes_long_123456",
            Issuer = "focus-api-test",
            Audience = "focus-web-test"
        };
        var jwtService = new JwtTokenService(Options.Create(jwtSettings));

        var user = User.CreateGuest("Ayse");
        var rawToken = "expired_token_123";
        var tokenHash = jwtService.HashToken(rawToken);

        // Gecmiste dolmus token
        user.AddRefreshToken(tokenHash, DateTime.UtcNow.AddMinutes(-10), "127.0.0.1");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new RefreshTokenCommandHandler(context, jwtService);
        var command = new RefreshTokenCommand(rawToken, "127.0.0.1");

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldDetectReplayAttack_AndRevokeAllSessions_WhenTokenAlreadyRevoked()
    {
        // Arrange
        using var context = CreateDbContext();
        var jwtSettings = new JwtSettings
        {
            SigningKey = "test_super_secret_signing_key_at_least_32_bytes_long_123456",
            Issuer = "focus-api-test",
            Audience = "focus-web-test"
        };
        var jwtService = new JwtTokenService(Options.Create(jwtSettings));

        var user = User.CreateGuest("Fatma");
        var compromisedToken = "stolen_token_123";
        var compromisedHash = jwtService.HashToken(compromisedToken);
        var activeToken = "valid_second_device_token_456";
        var activeHash = jwtService.HashToken(activeToken);

        context.Users.Add(user);
        var token1 = new RefreshToken(user.Id, compromisedHash, DateTime.UtcNow.AddDays(10), "127.0.0.1");
        token1.Revoke("127.0.0.1", "rotated");
        var token2 = new RefreshToken(user.Id, activeHash, DateTime.UtcNow.AddDays(10), "127.0.0.1");

        context.RefreshTokens.AddRange(token1, token2);
        await context.SaveChangesAsync();

        var handler = new RefreshTokenCommandHandler(context, jwtService);
        var command = new RefreshTokenCommand(compromisedToken, "192.168.1.100");

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert: Guvenlik ihlali hatasi firlatmali
        var exception = await act.Should().ThrowAsync<UnauthorizedAccessException>();
        exception.WithMessage("*Güvenlik ihlali*");

        // Ve diger aktif oturum da iptal edilmis olmali
        var secondTokenInDb = await context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == activeHash);
        secondTokenInDb.Should().NotBeNull();
        secondTokenInDb!.IsRevoked.Should().BeTrue();
    }
}
