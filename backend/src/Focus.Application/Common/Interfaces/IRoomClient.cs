using Focus.Application.Features.Social.DTOs;

namespace Focus.Application.Common.Interfaces;

public interface IRoomClient
{
    Task UserJoinedRoom(RoomMemberDto member);
    Task UserLeftRoom(Guid userId);
    Task SeatOccupied(Guid userId, int seatIndex);
    Task SeatVacated(Guid userId, int seatIndex);
    Task DeskLightToggled(Guid userId, bool isLightOn);
    Task ReactionBroadcasted(RoomReactionDto reaction);
    Task CoWorkingBonusUpdated(int bonusPercent);
}
