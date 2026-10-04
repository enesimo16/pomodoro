using Focus.Application.Features.Social.Commands;
using Focus.Domain.Entities;
using Focus.UnitTests.Common;
using Xunit;

namespace Focus.UnitTests.Social;

public class RoomInvitationCommandHandlerTests
{
    [Fact]
    public async Task CreateRoomInvitation_ShouldGenerateValidMobileLinks()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("OdaSahibi");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new CreateRoomInvitationCommandHandler(context);
        var result = await handler.Handle(new CreateRoomInvitationCommand(user.Id, "https://focus.app"), CancellationToken.None);

        Assert.NotNull(result.RoomCode);
        Assert.StartsWith("FOC-", result.RoomCode, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("INV-", result.InviteCode, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("https://focus.app/join/", result.DirectLink, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("https://api.whatsapp.com/send?text=", result.WhatsAppLink, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("https://t.me/share/url", result.TelegramLink, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InviteUserByUsername_WhenUserExists_ShouldCreateTargetedInvite()
    {
        using var context = TestDbContextFactory.Create();
        var inviter = User.CreateGuest("Ali");
        var friend = User.CreateGuest("Veli");
        context.Users.AddRange(inviter, friend);
        await context.SaveChangesAsync();

        var handler = new InviteUserByUsernameCommandHandler(context);
        var result = await handler.Handle(new InviteUserByUsernameCommand(inviter.Id, "Veli", "https://focus.app"), CancellationToken.None);

        Assert.NotNull(result.InviteCode);
        Assert.Contains("Veli", result.ShareMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InviteUserByUsername_WhenUserNotFound_ShouldThrow()
    {
        using var context = TestDbContextFactory.Create();
        var inviter = User.CreateGuest("Ali");
        context.Users.Add(inviter);
        await context.SaveChangesAsync();

        var handler = new InviteUserByUsernameCommandHandler(context);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new InviteUserByUsernameCommand(inviter.Id, "OlmayanKullanici"), CancellationToken.None));
    }

    [Fact]
    public async Task InviteUserByUsername_WhenInvitingSelf_ShouldThrow()
    {
        using var context = TestDbContextFactory.Create();
        var inviter = User.CreateGuest("Ali");
        context.Users.Add(inviter);
        await context.SaveChangesAsync();

        var handler = new InviteUserByUsernameCommandHandler(context);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new InviteUserByUsernameCommand(inviter.Id, "Ali"), CancellationToken.None));
    }
}
