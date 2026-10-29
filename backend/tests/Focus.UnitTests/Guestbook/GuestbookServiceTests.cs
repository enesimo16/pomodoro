using FluentAssertions;
using Focus.Application.Features.Social;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Focus.Infrastructure.Persistence;
using Focus.Infrastructure.Services;
using Focus.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Focus.UnitTests.Guestbook;

public class GuestbookServiceTests
{
    private static async Task<(ApplicationDbContext Context, User Owner, StudyRoom Room)> CreatePrivateRoomAsync()
    {
        var context = TestDbContextFactory.Create();
        var owner = User.CreateGuest("Sahip");
        context.Users.Add(owner);
        await context.SaveChangesAsync();
        var room = await PrivateRoomProvisioner.EnsureAsync(context, owner, CancellationToken.None);
        return (context, owner, room);
    }

    private static async Task<User> AddUserAsync(ApplicationDbContext context, string name)
    {
        var user = User.CreateGuest(name);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task AddEntry_PrivateRoomByNonMember_ShouldFail()
    {
        var (context, _, room) = await CreatePrivateRoomAsync();
        using var db = context;
        var stranger = await AddUserAsync(context, "Yabanci");

        var act = () => new GuestbookService(context).AddEntryAsync(room.Code, stranger.Id, "merhaba");

        await act.Should().ThrowAsync<InvalidOperationException>();
        (await context.RoomGuestbookEntries.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AddEntry_ByMemberWithLowercaseCode_ShouldStoreEntryAndNotifyOwnerOnce()
    {
        var (context, owner, room) = await CreatePrivateRoomAsync();
        using var db = context;
        var visitor = await AddUserAsync(context, "Ziyaretci");
        context.RoomMembers.Add(room.AddMember(visitor.Id));
        await context.SaveChangesAsync();

        var dto = await new GuestbookService(context).AddEntryAsync(" " + room.Code.ToLowerInvariant() + " ", visitor.Id, "selam", GiftType.Coffee);

        dto.StudyRoomCode.Should().Be(room.Code);
        (await context.Notifications.CountAsync(n => n.UserId == owner.Id && n.Type == NotificationType.GuestbookNote)).Should().Be(1);
    }

    [Fact]
    public async Task AddEntry_PublicRoomByNonMember_ShouldSucceed()
    {
        using var context = TestDbContextFactory.Create();
        context.StudyRooms.Add(StudyRoom.CreatePublicLibrary("Kutuphane", "LIB-GB", "test"));
        var user = await AddUserAsync(context, "Gezgin");

        var dto = await new GuestbookService(context).AddEntryAsync("LIB-GB", user.Id, "merhaba");

        dto.Message.Should().Be("merhaba");
    }

    [Fact]
    public void Create_MessageLongerThanLimit_ShouldThrow()
    {
        var tooLong = new string('a', RoomGuestbookEntry.MaxMessageLength + 1);

        var act = () => RoomGuestbookEntry.Create(Guid.NewGuid(), Guid.NewGuid(), "Gezgin", tooLong);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task DeleteEntry_ShouldAllowOwnerAndSenderOnly()
    {
        var (context, owner, room) = await CreatePrivateRoomAsync();
        using var db = context;
        var sender = await AddUserAsync(context, "Yazar");
        var other = await AddUserAsync(context, "Baskasi");
        context.RoomMembers.Add(room.AddMember(sender.Id));
        await context.SaveChangesAsync();
        var service = new GuestbookService(context);
        var first = await service.AddEntryAsync(room.Code, sender.Id, "bir");
        var second = await service.AddEntryAsync(room.Code, sender.Id, "iki");

        (await service.DeleteEntryAsync(room.Code, first.Id, other.Id)).Should().BeFalse();
        (await service.DeleteEntryAsync(room.Code, first.Id, owner.Id)).Should().BeTrue();
        (await service.DeleteEntryAsync(room.Code, second.Id, sender.Id)).Should().BeTrue();
        (await context.RoomGuestbookEntries.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task GetEntries_ShouldClampLimit()
    {
        var (context, owner, room) = await CreatePrivateRoomAsync();
        using var db = context;
        var service = new GuestbookService(context);
        for (var i = 0; i < 3; i++)
        {
            await service.AddEntryAsync(room.Code, owner.Id, $"not {i}");
        }

        (await service.GetEntriesByRoomCodeAsync(room.Code, -5)).Should().HaveCount(1);
        (await service.GetEntriesByRoomCodeAsync(room.Code, 100000)).Should().HaveCount(3);
    }
}
