using FluentAssertions;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Auth.Commands;
using Focus.Domain.Entities;
using Focus.Infrastructure.Authentication;
using Focus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Focus.UnitTests.Application;

public class CreateGuestCommandHandlerTests
{
    private ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_ShouldCreateGuestUser_AndReturnAuthResponse()
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

        var handler = new CreateGuestCommandHandler(context, jwtService);
        var command = new CreateGuestCommand("Ahmet", "tr-TR", "Europe/Istanbul", "127.0.0.1");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.User.DisplayName.Should().Be("Ahmet");
        result.User.IsGuest.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();

        // Veritabaninda olusma kontrolu
        var savedUser = await context.Users
            .Include(u => u.Avatar)
            .Include(u => u.Room)
                .ThenInclude(r => r!.Items)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == result.User.Id);

        savedUser.Should().NotBeNull();
        savedUser!.DisplayName.Should().Be("Ahmet");
        savedUser.Room.Should().NotBeNull();
        savedUser.Room!.Items.Should().HaveCount(3);
        savedUser.RefreshTokens.Should().ContainSingle();

        // 50 coin baslangic hediyesi kontrolu
        var coinEntry = await context.CoinLedgerEntries.FirstOrDefaultAsync(c => c.UserId == savedUser.Id);
        coinEntry.Should().NotBeNull();
        coinEntry!.Coins.Should().Be(50);
    }
}
