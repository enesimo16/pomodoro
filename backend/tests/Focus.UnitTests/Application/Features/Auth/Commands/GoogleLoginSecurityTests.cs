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

namespace Focus.UnitTests.Application.Features.Auth.Commands;

public class GoogleLoginSecurityTests
{
    private static readonly JwtSettings JwtConfig = new()
    {
        SigningKey = "test_super_secret_signing_key_at_least_32_bytes_long_123456",
        Issuer = "focus-api-test",
        Audience = "focus-web-test",
        RefreshTokenDays = 7
    };

    private static DbContextOptions<ApplicationDbContext> NewOptions() =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static GoogleLoginCommandHandler CreateHandler(IApplicationDbContext context, GoogleUserInfo? info)
    {
        var google = new Mock<IGoogleAuthService>();
        google.Setup(g => g.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(info);
        return new GoogleLoginCommandHandler(context, google.Object, new JwtTokenService(Options.Create(JwtConfig)));
    }

    [Theory]
    [InlineData("sub", "a@focus.app", false)]
    [InlineData("sub", "", true)]
    [InlineData("sub", "   ", true)]
    [InlineData("", "a@focus.app", true)]
    public async Task Handle_RejectsUnverifiedOrIncompleteIdentity(string subject, string email, bool verified)
    {
        using var context = new ApplicationDbContext(NewOptions());
        var handler = CreateHandler(context, new GoogleUserInfo(subject, email, verified, "Ad", null));

        var act = () => handler.Handle(new GoogleLoginCommand("token"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        (await context.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_FindsUserByGoogleSubject_EvenWhenEmailChanged()
    {
        using var context = new ApplicationDbContext(NewOptions());
        var existing = User.CreateGoogleUser("old@focus.app", "Eski", "sub_1");
        context.Users.Add(existing);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context, new GoogleUserInfo("sub_1", "new@focus.app", true, "Yeni", null));

        var result = await handler.Handle(new GoogleLoginCommand("token"), CancellationToken.None);

        result.User.Id.Should().Be(existing.Id);
        (await context.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_LinksGoogleLoginWhenMatchedByEmail()
    {
        using var context = new ApplicationDbContext(NewOptions());
        var existing = User.CreateGoogleUser("mail@focus.app", "Ad", "sub_old");
        context.Users.Add(existing);
        await context.SaveChangesAsync();
        context.UserExternalLogins.RemoveRange(context.UserExternalLogins);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context, new GoogleUserInfo("sub_new", "Mail@Focus.app", true, "Ad", null));

        var result = await handler.Handle(new GoogleLoginCommand("token"), CancellationToken.None);

        result.User.Id.Should().Be(existing.Id);
        (await context.UserExternalLogins.SingleAsync()).ProviderKey.Should().Be("sub_new");
    }

    [Fact]
    public async Task Handle_RejectsEmailMatchWhenAccountIsLinkedToAnotherGoogleSubject()
    {
        using var context = new ApplicationDbContext(NewOptions());
        context.Users.Add(User.CreateGoogleUser("mail@focus.app", "Ad", "sub_owner"));
        await context.SaveChangesAsync();

        var handler = CreateHandler(context, new GoogleUserInfo("sub_attacker", "mail@focus.app", true, "Ad", null));

        var act = () => handler.Handle(new GoogleLoginCommand("token"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_MergesGuestIntoExistingAccount_WithoutCopyingInitialBonus()
    {
        using var context = new ApplicationDbContext(NewOptions());
        var existing = User.CreateGoogleUser("mail@focus.app", "Ad", "sub_1");
        var guest = User.CreateGuest("Misafir");
        guest.AddXp(250);
        guest.AddRefreshToken("guest_hash", DateTime.UtcNow.AddDays(5), null);
        context.Users.AddRange(existing, guest);
        context.CoinLedgerEntries.AddRange(
            new CoinLedgerEntry(guest.Id, CoinTransactionReason.InitialBonus, 50),
            new CoinLedgerEntry(guest.Id, CoinTransactionReason.SessionReward, 30),
            new CoinLedgerEntry(guest.Id, CoinTransactionReason.ItemPurchase, -20));
        await context.SaveChangesAsync();

        var handler = CreateHandler(context, new GoogleUserInfo("sub_1", "mail@focus.app", true, "Ad", null));

        var result = await handler.Handle(new GoogleLoginCommand("token", guest.Id), CancellationToken.None);

        result.User.Id.Should().Be(existing.Id);
        var transferred = await context.CoinLedgerEntries.Where(c => c.UserId == existing.Id).ToListAsync();
        transferred.Should().ContainSingle(c => c.Reason == CoinTransactionReason.SessionReward && c.Coins == 30);
        transferred.Should().NotContain(c => c.Reason == CoinTransactionReason.InitialBonus);
        transferred.Should().NotContain(c => c.Coins < 0);
        (await context.Users.SingleAsync(u => u.Id == existing.Id)).CurrentXp.Should().Be(250);
        (await context.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == guest.Id)).DeletedAt.Should().NotBeNull();
        (await context.RefreshTokens.SingleAsync(t => t.TokenHash == "guest_hash")).IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_IgnoresClaimTarget_WhenItIsNotAGuest()
    {
        using var context = new ApplicationDbContext(NewOptions());
        var regular = User.CreateGoogleUser("other@focus.app", "Baskasi", "sub_other");
        context.Users.Add(regular);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context, new GoogleUserInfo("sub_new", "new@focus.app", true, "Yeni", null));

        var result = await handler.Handle(new GoogleLoginCommand("token", regular.Id), CancellationToken.None);

        result.User.Id.Should().NotBe(regular.Id);
        (await context.Users.SingleAsync(u => u.Id == regular.Id)).Email.Should().Be("other@focus.app");
    }

    [Fact]
    public async Task Handle_UsesConfiguredRefreshTokenLifetime()
    {
        using var context = new ApplicationDbContext(NewOptions());
        var handler = CreateHandler(context, new GoogleUserInfo("sub_1", "mail@focus.app", true, "Ad", null));

        await handler.Handle(new GoogleLoginCommand("token"), CancellationToken.None);

        var token = await context.RefreshTokens.SingleAsync();
        token.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Handle_RecoversFromConcurrentFirstLogin()
    {
        var options = NewOptions();
        using var competitorContext = new ApplicationDbContext(options);
        using var context = new CollidingDbContext(options, competitorContext);
        var handler = CreateHandler(context, new GoogleUserInfo("sub_race", "race@focus.app", true, "Yaris", null));

        var result = await handler.Handle(new GoogleLoginCommand("token"), CancellationToken.None);

        using var verify = new ApplicationDbContext(options);
        (await verify.Users.CountAsync()).Should().Be(1);
        result.User.Email.Should().Be("race@focus.app");
        (await verify.RefreshTokens.CountAsync(t => t.UserId == result.User.Id)).Should().Be(1);
    }

    // Ilk kayit denemesinde baska bir istegin ayni kullaniciyi olusturmasini ve benzersizlik ihlalini taklit eder
    private sealed class CollidingDbContext : ApplicationDbContext
    {
        private readonly ApplicationDbContext _competitor;
        private bool _collided;

        public CollidingDbContext(DbContextOptions<ApplicationDbContext> options, ApplicationDbContext competitor)
            : base(options)
        {
            _competitor = competitor;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (!_collided)
            {
                _collided = true;
                _competitor.Users.Add(User.CreateGoogleUser("race@focus.app", "Yaris", "sub_race"));
                await _competitor.SaveChangesAsync(cancellationToken);
                throw new DbUpdateException("duplicate key value violates unique constraint");
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
