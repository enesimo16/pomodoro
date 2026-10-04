using Focus.Domain.Entities;
using Xunit;

namespace Focus.UnitTests.Social;

public class StudyRoomEntityTests
{
    [Fact]
    public void CanJoin_FreeUserPrivateRoom_AllowsMaxTwoMembers()
    {
        var ownerId = Guid.NewGuid();
        var room = StudyRoom.CreatePrivateRoom(ownerId, "Free Room", "FOC-1001", isOwnerPro: false);

        // 1. Sahip eklenir
        room.AddMember(ownerId);
        Assert.True(room.CanJoin(isOwnerPro: false, out var err1));
        Assert.Null(err1);

        // 2. Bir misafir eklenir (toplam 2)
        var guest1 = Guid.NewGuid();
        room.AddMember(guest1);

        // 3. Üçüncü misafir katılmaya çalışır
        var canJoinThird = room.CanJoin(isOwnerPro: false, out var err3);
        Assert.False(canJoinThird);
        Assert.NotNull(err3);
        Assert.Contains("Premium", err3, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CanJoin_ProUserPrivateRoom_AllowsMaxFourMembers()
    {
        var ownerId = Guid.NewGuid();
        var room = StudyRoom.CreatePrivateRoom(ownerId, "Pro Room", "FOC-1002", isOwnerPro: true);

        room.AddMember(ownerId);
        room.AddMember(Guid.NewGuid());
        room.AddMember(Guid.NewGuid());
        room.AddMember(Guid.NewGuid()); // 4 kisiye ulasti

        var canJoinFifth = room.CanJoin(isOwnerPro: true, out var errFifth);
        Assert.False(canJoinFifth);
        Assert.NotNull(errFifth);
        Assert.Contains("4 kişilik", errFifth, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 10)]
    [InlineData(3, 15)]
    [InlineData(4, 20)]
    [InlineData(5, 20)]
    public void CalculateCoWorkingBonusPercent_ShouldScaleCorrectly(int focusingCount, int expectedBonus)
    {
        var room = StudyRoom.CreatePublicLibrary("Test Library", "LIB-TEST", "Description", capacity: 20);

        for (var i = 0; i < focusingCount; i++)
        {
            var member = room.AddMember(Guid.NewGuid(), seatIndex: i + 1);
            member.SetFocusing(true);
        }

        var bonus = room.CalculateCoWorkingBonusPercent();
        Assert.Equal(expectedBonus, bonus);
    }

    [Fact]
    public void AssignSeat_WhenSeatOccupied_ShouldReturnFalse()
    {
        var room = StudyRoom.CreatePublicLibrary("Library", "LIB-1", "Test", capacity: 20);
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        room.AddMember(user1);
        room.AddMember(user2);

        var sat1 = room.AssignSeat(user1, seatIndex: 3);
        Assert.True(sat1);

        // User2 ayni koltuga oturmaya calisir
        var sat2 = room.AssignSeat(user2, seatIndex: 3);
        Assert.False(sat2);

        // Baska koltuga oturabilir
        var sat3 = room.AssignSeat(user2, seatIndex: 4);
        Assert.True(sat3);
    }
}
