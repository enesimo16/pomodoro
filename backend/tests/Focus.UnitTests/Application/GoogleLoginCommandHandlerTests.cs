using FluentAssertions;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Auth.Commands;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Focus.Infrastructure.Authentication;
using Focus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Focus.UnitTests.Application;

public class GoogleLoginCommandHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_ShouldCreateGoogleUser_WhenUserDoesNotExist()
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

        var googleServiceMock = new Mock<IGoogleAuthService>();
        googleServiceMock
            .Setup(g => g.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleUserInfo("sub_123", "developer@focus.app", "Enes Developer", "avatar.png"));

        var handler = new GoogleLoginCommandHandler(context, googleServiceMock.Object, jwtService);
        var command = new GoogleLoginCommand("valid_google_token");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.User.Email.Should().Be("developer@focus.app");
        result.User.DisplayName.Should().Be("Enes Developer");
        result.User.IsGuest.Should().BeFalse();
        result.AccessToken.Should().NotBeNullOrWhiteSpace();

        var savedUser = await context.Users
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.Email == "developer@focus.app");

        savedUser.Should().NotBeNull();
        savedUser!.ExternalLogins.Should().ContainSingle(l => l.Provider == AuthProvider.Google && l.ProviderKey == "sub_123");
    }

    [Fact]
    public async Task Handle_ShouldClaimGuestUser_WhenGuestUserIdIsProvided()
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

        var guestUser = User.CreateGuest("GeciciMisafir");
        context.Users.Add(guestUser);
        await context.SaveChangesAsync();

        var googleServiceMock = new Mock<IGoogleAuthService>();
        googleServiceMock
            .Setup(g => g.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleUserInfo("sub_456", "claimed@focus.app", "Enes Kalici", null));

        var handler = new GoogleLoginCommandHandler(context, googleServiceMock.Object, jwtService);
        var command = new GoogleLoginCommand("valid_google_token", guestUser.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.User.Id.Should().Be(guestUser.Id); // Ayni ID korunmali!
        result.User.Email.Should().Be("claimed@focus.app");
        result.User.IsGuest.Should().BeFalse();

        var claimedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == guestUser.Id);
        claimedUser.Should().NotBeNull();
        claimedUser!.IsGuest.Should().BeFalse();
        claimedUser.Email.Should().Be("claimed@focus.app");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenTokenIsInvalid()
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

        var googleServiceMock = new Mock<IGoogleAuthService>();
        googleServiceMock
            .Setup(g => g.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoogleUserInfo?)null);

        var handler = new GoogleLoginCommandHandler(context, googleServiceMock.Object, jwtService);
        var command = new GoogleLoginCommand("invalid_google_token");

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
