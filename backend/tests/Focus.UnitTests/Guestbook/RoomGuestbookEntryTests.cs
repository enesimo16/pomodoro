using FluentAssertions;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Xunit;

namespace Focus.UnitTests.Guestbook;

public class RoomGuestbookEntryTests
{
    [Fact]
    public void Create_WithValidParameters_SetsPropertiesCorrectly()
    {
        var roomId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var displayName = "PikselGezgini";
        var message = "Harika bir seansti, tebrikler!";
        var gift = GiftType.Coffee;

        var entry = RoomGuestbookEntry.Create(roomId, senderId, displayName, message, gift);

        entry.Id.Should().NotBeEmpty();
        entry.StudyRoomId.Should().Be(roomId);
        entry.SenderUserId.Should().Be(senderId);
        entry.SenderDisplayName.Should().Be(displayName);
        entry.Message.Should().Be(message);
        entry.GiftType.Should().Be(gift);
        entry.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithEmptyMessage_ThrowsArgumentException()
    {
        var act = () => RoomGuestbookEntry.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Gezgin",
            "   ",
            GiftType.None);

        act.Should().Throw<ArgumentException>();
    }
}
