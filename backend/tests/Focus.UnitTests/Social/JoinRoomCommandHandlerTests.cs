using Focus.Application.Features.Social;
using Focus.Application.Features.Social.Commands;
using Focus.Application.Features.Social.Queries;
using Focus.Domain.Entities;
using Focus.Infrastructure.Persistence;
using Focus.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Focus.UnitTests.Social;

public class JoinRoomCommandHandlerTests
{
    private static async Task<(ApplicationDbContext Context, User Owner, StudyRoom Room)> CreateOwnedRoomAsync()
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
    public async Task Join_PrivateRoomWithoutInvite_ShouldBeRejected()
    {
        var (context, _, room) = await CreateOwnedRoomAsync();
        using var db = context;
        var stranger = await AddUserAsync(context, "Yabanci");

        var result = await new JoinRoomCommandHandler(context)
            .Handle(new JoinRoomCommand(stranger.Id, room.Code), CancellationToken.None);

        Assert.False(result.Success);
        Assert.False(await context.RoomMembers.AnyAsync(m => m.UserId == stranger.Id));
    }

    [Fact]
    public async Task Join_PrivateRoomWithInviteOfAnotherRoom_ShouldBeRejected()
    {
        var (context, _, room) = await CreateOwnedRoomAsync();
        using var db = context;
        var otherOwner = await AddUserAsync(context, "Baska");
        var otherRoom = await PrivateRoomProvisioner.EnsureAsync(context, otherOwner, CancellationToken.None);
        var foreignInvite = await PrivateRoomProvisioner.CreateInvitationAsync(context, otherRoom, otherOwner.Id, null, null, CancellationToken.None);
        var stranger = await AddUserAsync(context, "Yabanci");

        var result = await new JoinRoomCommandHandler(context)
            .Handle(new JoinRoomCommand(stranger.Id, room.Code, foreignInvite.InviteCode), CancellationToken.None);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Join_PrivateRoomWithExpiredInvite_ShouldBeRejected()
    {
        var (context, owner, room) = await CreateOwnedRoomAsync();
        using var db = context;
        context.RoomInvitations.Add(new RoomInvitation(room.Id, owner.Id, "INV-EXPIRED1", validHours: -1));
        await context.SaveChangesAsync();
        var guest = await AddUserAsync(context, "Misafir");

        var result = await new JoinRoomCommandHandler(context)
            .Handle(new JoinRoomCommand(guest.Id, room.Code, "INV-EXPIRED1"), CancellationToken.None);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Join_PrivateRoomWithValidInvite_ShouldJoinAndBeIdempotent()
    {
        var (context, owner, room) = await CreateOwnedRoomAsync();
        using var db = context;
        var invite = await PrivateRoomProvisioner.CreateInvitationAsync(context, room, owner.Id, null, null, CancellationToken.None);
        var guest = await AddUserAsync(context, "Misafir");
        var handler = new JoinRoomCommandHandler(context);

        var first = await handler.Handle(new JoinRoomCommand(guest.Id, room.Code, invite.InviteCode.ToLowerInvariant()), CancellationToken.None);
        var second = await handler.Handle(new JoinRoomCommand(guest.Id, room.Code, invite.InviteCode), CancellationToken.None);

        Assert.True(first.Success);
        Assert.True(first.NewlyJoined);
        Assert.True(second.Success);
        Assert.False(second.NewlyJoined);
        Assert.Equal(1, await context.RoomMembers.CountAsync(m => m.UserId == guest.Id));
    }

    [Fact]
    public async Task Join_TargetedInviteForSomeoneElse_ShouldBeRejected()
    {
        var (context, owner, room) = await CreateOwnedRoomAsync();
        using var db = context;
        var friend = await AddUserAsync(context, "Arkadas");
        var intruder = await AddUserAsync(context, "Davetsiz");
        var invite = await PrivateRoomProvisioner.CreateInvitationAsync(context, room, owner.Id, friend.Id, friend.DisplayName, CancellationToken.None);
        var handler = new JoinRoomCommandHandler(context);

        var intruderResult = await handler.Handle(new JoinRoomCommand(intruder.Id, room.Code, invite.InviteCode), CancellationToken.None);
        var friendResult = await handler.Handle(new JoinRoomCommand(friend.Id, room.Code, invite.InviteCode), CancellationToken.None);

        Assert.False(intruderResult.Success);
        Assert.True(friendResult.Success);
        Assert.True((await context.RoomInvitations.AsNoTracking().FirstAsync(i => i.Id == invite.Id)).IsAccepted);
    }

    [Fact]
    public async Task Join_AnotherRoom_ShouldLeaveThePreviousOne()
    {
        var (context, owner, room) = await CreateOwnedRoomAsync();
        using var db = context;
        context.StudyRooms.Add(StudyRoom.CreatePublicLibrary("Kutuphane", "LIB-TEST", "test", capacity: 10));
        await context.SaveChangesAsync();
        var user = await AddUserAsync(context, "Gezgin");
        var handler = new JoinRoomCommandHandler(context);

        await handler.Handle(new JoinRoomCommand(user.Id, "LIB-TEST"), CancellationToken.None);
        var invite = await PrivateRoomProvisioner.CreateInvitationAsync(context, room, owner.Id, null, null, CancellationToken.None);
        var moved = await handler.Handle(new JoinRoomCommand(user.Id, room.Code, invite.InviteCode), CancellationToken.None);

        Assert.True(moved.Success);
        Assert.Contains(moved.LeftRooms, l => l.RoomCode == "LIB-TEST");
        var memberships = await context.RoomMembers.Where(m => m.UserId == user.Id).ToListAsync();
        Assert.Single(memberships);
        Assert.Equal(room.Id, memberships[0].RoomId);
    }

    [Fact]
    public async Task Join_OwnerOfFullPrivateRoom_ShouldBypassCapacity()
    {
        var (context, owner, room) = await CreateOwnedRoomAsync();
        using var db = context;
        var invite = await PrivateRoomProvisioner.CreateInvitationAsync(context, room, owner.Id, null, null, CancellationToken.None);
        var guest = await AddUserAsync(context, "Misafir");
        var handler = new JoinRoomCommandHandler(context);
        await handler.Handle(new JoinRoomCommand(guest.Id, room.Code, invite.InviteCode), CancellationToken.None);
        await new LeaveRoomCommandHandler(context).Handle(new LeaveRoomCommand(owner.Id, room.Code), CancellationToken.None);
        var other = await AddUserAsync(context, "Diger");
        await handler.Handle(new JoinRoomCommand(other.Id, room.Code, invite.InviteCode), CancellationToken.None);

        var ownerJoin = await handler.Handle(new JoinRoomCommand(owner.Id, room.Code), CancellationToken.None);

        Assert.True(ownerJoin.Success);
    }

    [Fact]
    public async Task IsRoomMember_ShouldOnlyAcceptMembers()
    {
        var (context, owner, room) = await CreateOwnedRoomAsync();
        using var db = context;
        var stranger = await AddUserAsync(context, "Yabanci");
        var handler = new IsRoomMemberQueryHandler(context);

        Assert.True(await handler.Handle(new IsRoomMemberQuery(owner.Id, room.Code.ToLowerInvariant()), CancellationToken.None));
        Assert.False(await handler.Handle(new IsRoomMemberQuery(stranger.Id, room.Code), CancellationToken.None));
    }

    [Fact]
    public async Task GetRoomByCode_PrivateRoomForNonMember_ShouldReturnNull()
    {
        var (context, owner, room) = await CreateOwnedRoomAsync();
        using var db = context;
        var stranger = await AddUserAsync(context, "Yabanci");
        var handler = new GetRoomByCodeQueryHandler(context);

        Assert.Null(await handler.Handle(new GetRoomByCodeQuery(room.Code, stranger.Id), CancellationToken.None));
        Assert.NotNull(await handler.Handle(new GetRoomByCodeQuery(room.Code, owner.Id), CancellationToken.None));
    }
}
