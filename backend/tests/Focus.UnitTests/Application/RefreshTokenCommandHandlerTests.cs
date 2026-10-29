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
        exception.WithMessage("Oturum geçersiz.");

        // Ve diger aktif oturum da iptal edilmis olmali
        var secondTokenInDb = await context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == activeHash);
        secondTokenInDb.Should().NotBeNull();
        secondTokenInDb!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldRejectLoggedOutToken_WithoutRevokingOtherSessions()
    {
        using var context = CreateDbContext();
        var jwtService = CreateJwtService();

        var user = User.CreateGuest("Selin");
        var loggedOutHash = jwtService.HashToken("logged_out_token");
        var activeHash = jwtService.HashToken("other_device_token");

        context.Users.Add(user);
        var loggedOut = new RefreshToken(user.Id, loggedOutHash, DateTime.UtcNow.AddDays(10), "127.0.0.1");
        loggedOut.Revoke("127.0.0.1");
        var active = new RefreshToken(user.Id, activeHash, DateTime.UtcNow.AddDays(10), "127.0.0.1");
        context.RefreshTokens.AddRange(loggedOut, active);
        await context.SaveChangesAsync();

        var handler = new RefreshTokenCommandHandler(context, jwtService);

        var act = async () => await handler.Handle(new RefreshTokenCommand("logged_out_token", "10.0.0.1"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Oturum geçersiz.");
        var stillActive = await context.RefreshTokens.SingleAsync(t => t.TokenHash == activeHash);
        stillActive.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldRevokeAllSessionsWithoutReplacementHash_WhenRotatedTokenIsReplayed()
    {
        using var context = CreateDbContext();
        var jwtService = CreateJwtService();

        var user = User.CreateGuest("Kaan");
        var rotatedHash = jwtService.HashToken("rotated_token");
        var activeHash = jwtService.HashToken("active_token");

        context.Users.Add(user);
        var rotated = new RefreshToken(user.Id, rotatedHash, DateTime.UtcNow.AddDays(10), "127.0.0.1");
        rotated.Revoke("127.0.0.1", "next_hash");
        var active = new RefreshToken(user.Id, activeHash, DateTime.UtcNow.AddDays(10), "127.0.0.1");
        context.RefreshTokens.AddRange(rotated, active);
        await context.SaveChangesAsync();

        var handler = new RefreshTokenCommandHandler(context, jwtService);

        var act = async () => await handler.Handle(new RefreshTokenCommand("rotated_token", "10.0.0.1"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        var revokedActive = await context.RefreshTokens.SingleAsync(t => t.TokenHash == activeHash);
        revokedActive.IsRevoked.Should().BeTrue();
        revokedActive.ReplacedByTokenHash.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldAllowOnlyOneRequest_WhenSameTokenIsRefreshedConcurrently()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        var jwtService = CreateJwtService();

        var user = User.CreateGuest("Deniz");
        var tokenHash = jwtService.HashToken("shared_token");
        user.AddRefreshToken(tokenHash, DateTime.UtcNow.AddDays(10), "127.0.0.1");

        using var seedContext = new ApplicationDbContext(options);
        seedContext.Users.Add(user);
        await seedContext.SaveChangesAsync();

        using var firstContext = new ApplicationDbContext(options);
        using var secondContext = new ApplicationDbContext(options);

        // Her iki istek de belirteci iptal edilmemis olarak okudu
        await firstContext.RefreshTokens.ToListAsync();
        await secondContext.RefreshTokens.ToListAsync();

        var first = new RefreshTokenCommandHandler(firstContext, jwtService);
        var second = new RefreshTokenCommandHandler(secondContext, jwtService);

        var firstResult = await first.Handle(new RefreshTokenCommand("shared_token"), CancellationToken.None);
        var secondAct = async () => await second.Handle(new RefreshTokenCommand("shared_token"), CancellationToken.None);

        firstResult.RefreshToken.Should().NotBeNullOrWhiteSpace();
        await secondAct.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Oturum geçersiz.");

        using var verifyContext = new ApplicationDbContext(options);
        // InMemory saglayici islem desteklemez; kazanan istegin rotasyonu eski belirtece yazilmis olmali
        var original = await verifyContext.RefreshTokens.SingleAsync(t => t.TokenHash == tokenHash);
        original.ReplacedByTokenHash.Should().Be(jwtService.HashToken(firstResult.RefreshToken));
    }

    [Fact]
    public async Task Handle_ShouldUseConfiguredRefreshTokenLifetime()
    {
        using var context = CreateDbContext();
        var jwtService = new JwtTokenService(Options.Create(new JwtSettings
        {
            SigningKey = "test_super_secret_signing_key_at_least_32_bytes_long_123456",
            RefreshTokenDays = 7
        }));

        var user = User.CreateGuest("Ece");
        user.AddRefreshToken(jwtService.HashToken("lifetime_token"), DateTime.UtcNow.AddDays(10), "127.0.0.1");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new RefreshTokenCommandHandler(context, jwtService);
        var result = await handler.Handle(new RefreshTokenCommand("lifetime_token"), CancellationToken.None);

        var newHash = jwtService.HashToken(result.RefreshToken);
        var created = await context.RefreshTokens.SingleAsync(t => t.TokenHash == newHash);
        created.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
    }

    private static JwtTokenService CreateJwtService()
    {
        return new JwtTokenService(Options.Create(new JwtSettings
        {
            SigningKey = "test_super_secret_signing_key_at_least_32_bytes_long_123456",
            Issuer = "focus-api-test",
            Audience = "focus-web-test"
        }));
    }
}
