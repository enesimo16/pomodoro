using Focus.Application.Common.Options;
using Focus.Application.Features.Session.Services;
using Focus.Application.Features.Social;
using Focus.Application.Features.Social.Commands;
using Focus.Application.Common.Interfaces;
using Focus.Domain.Entities;
using Focus.Infrastructure.BackgroundJobs;
using Focus.Infrastructure.Persistence;
using Focus.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Focus.UnitTests.Social;

public class RoomMembershipTests
{
    private static async Task<(ApplicationDbContext Context, User Owner, User Guest, StudyRoom Room)> CreateRoomWithGuestAsync()
    {
        var context = TestDbContextFactory.Create();
        var owner = User.CreateGuest("Sahip");
        var guest = User.CreateGuest("Misafir");
        context.Users.AddRange(owner, guest);
        await context.SaveChangesAsync();

        var room = await PrivateRoomProvisioner.EnsureAsync(context, owner, CancellationToken.None);
        var invite = await PrivateRoomProvisioner.CreateInvitationAsync(context, room, owner.Id, null, null, CancellationToken.None);
        await new JoinRoomCommandHandler(context).Handle(new JoinRoomCommand(guest.Id, room.Code, invite.InviteCode), CancellationToken.None);
        return (context, owner, guest, room);
    }

    [Fact]
    public async Task Kick_ByOwner_ShouldRemoveMemberAndReportSeat()
    {
        var (context, owner, guest, room) = await CreateRoomWithGuestAsync();
        using var db = context;
        await new SitAtSeatCommandHandler(context).Handle(new SitAtSeatCommand(guest.Id, room.Code, 1), CancellationToken.None);

        var left = await new KickRoomMemberCommandHandler(context)
            .Handle(new KickRoomMemberCommand(owner.Id, room.Code, guest.Id), CancellationToken.None);

        Assert.NotNull(left);
        Assert.Equal(1, left.VacatedSeat);
        Assert.False(await context.RoomMembers.AnyAsync(m => m.UserId == guest.Id));
    }

    [Fact]
    public async Task Kick_ByNonOwnerOrSelf_ShouldBeRejected()
    {
        var (context, owner, guest, room) = await CreateRoomWithGuestAsync();
        using var db = context;
        var handler = new KickRoomMemberCommandHandler(context);

        var byGuest = await handler.Handle(new KickRoomMemberCommand(guest.Id, room.Code, owner.Id), CancellationToken.None);
        var self = await handler.Handle(new KickRoomMemberCommand(owner.Id, room.Code, owner.Id), CancellationToken.None);

        Assert.Null(byGuest);
        Assert.Null(self);
        Assert.Equal(2, await context.RoomMembers.CountAsync());
    }

    [Fact]
    public async Task Leave_WhenNotMember_ShouldReturnNull()
    {
        var (context, _, _, room) = await CreateRoomWithGuestAsync();
        using var db = context;
        var stranger = User.CreateGuest("Yabanci");
        context.Users.Add(stranger);
        await context.SaveChangesAsync();

        var left = await new LeaveRoomCommandHandler(context).Handle(new LeaveRoomCommand(stranger.Id, room.Code), CancellationToken.None);

        Assert.Null(left);
    }

    [Fact]
    public async Task SitAtSeat_ShouldValidateRangeAndReportPreviousSeat()
    {
        var (context, _, guest, room) = await CreateRoomWithGuestAsync();
        using var db = context;
        var handler = new SitAtSeatCommandHandler(context);

        var negative = await handler.Handle(new SitAtSeatCommand(guest.Id, room.Code, -1), CancellationToken.None);
        var tooHigh = await handler.Handle(new SitAtSeatCommand(guest.Id, room.Code, room.MaxCapacity), CancellationToken.None);
        var first = await handler.Handle(new SitAtSeatCommand(guest.Id, room.Code, 0), CancellationToken.None);
        var moved = await handler.Handle(new SitAtSeatCommand(guest.Id, room.Code, 1), CancellationToken.None);

        Assert.False(negative.Success);
        Assert.False(tooHigh.Success);
        Assert.True(first.Success);
        Assert.Null(first.PreviousSeatIndex);
        Assert.True(moved.Success);
        Assert.Equal(0, moved.PreviousSeatIndex);
    }

    [Fact]
    public async Task StaleMemberCleanup_ShouldRemoveOnlyInactiveMembers()
    {
        var (context, owner, guest, room) = await CreateRoomWithGuestAsync();
        using var db = context;
        var clock = new TestClock(DateTime.UtcNow);
        var options = Microsoft.Extensions.Options.Options.Create(new AlgorithmOptions());
        var service = new BackgroundJobService(
            context,
            new Mock<IVectorMemoryService>().Object,
            new SessionSettlementService(context, options),
            options,
            clock,
            NullLogger<BackgroundJobService>.Instance);

        Assert.Equal(0, await service.RunStaleRoomMemberCleanupAsync());

        clock.Advance(TimeSpan.FromMinutes(options.Value.RoomMemberStaleMinutes + 1));
        var removed = await service.RunStaleRoomMemberCleanupAsync();

        Assert.Equal(2, removed);
        Assert.Empty(await context.RoomMembers.ToListAsync());
        Assert.NotEqual(Guid.Empty, owner.Id);
        Assert.NotEqual(Guid.Empty, guest.Id);
    }

    [Fact]
    public async Task TouchMember_ShouldReturnWhetherUserIsMember()
    {
        var (context, owner, _, room) = await CreateRoomWithGuestAsync();
        using var db = context;
        var handler = new TouchRoomMemberCommandHandler(context);

        Assert.True(await handler.Handle(new TouchRoomMemberCommand(owner.Id, room.Code), CancellationToken.None));
        Assert.False(await handler.Handle(new TouchRoomMemberCommand(Guid.NewGuid(), room.Code), CancellationToken.None));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("coffee", true)]
    [InlineData("12345678901234567", false)]
    [InlineData("a\nb", false)]
    public void ReactionValidator_ShouldEnforceFormat(string reaction, bool expectedValid)
    {
        var validator = new SendRoomReactionCommandValidator();

        var result = validator.Validate(new SendRoomReactionCommand(Guid.NewGuid(), "FOC-ABCD2345", reaction));

        Assert.Equal(expectedValid, result.IsValid);
    }
}
