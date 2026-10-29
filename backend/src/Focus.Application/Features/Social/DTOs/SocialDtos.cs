using System.Text.Json.Serialization;
using Focus.Domain.Entities;

namespace Focus.Application.Features.Social.DTOs;

public record StudyRoomDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Type,
    Guid? OwnerUserId,
    string? OwnerDisplayName,
    int CurrentMemberCount,
    int MaxCapacity,
    bool IsPrivate,
    string? ThemeId,
    string? MusicTrackId,
    int CoWorkingBonusPercent,
    List<RoomMemberDto> Members,
    int FocusingCount = 0)
{
    public static StudyRoomDto FromEntity(StudyRoom room, bool includeMembers = true)
    {
        return new StudyRoomDto(
            room.Id,
            room.Code,
            room.Name,
            room.Description,
            room.Type.ToString(),
            room.OwnerUserId,
            room.OwnerUser?.DisplayName,
            room.Members.Count,
            room.MaxCapacity,
            room.IsPrivate,
            room.ThemeId,
            room.MusicTrackId,
            room.CalculateCoWorkingBonusPercent(),
            includeMembers ? room.Members.Select(RoomMemberDto.FromEntity).ToList() : new List<RoomMemberDto>(),
            room.Members.Count(m => m.IsFocusing));
    }
}

public record RoomMemberDto(
    Guid Id,
    Guid UserId,
    string DisplayName,
    int Level,
    string? AvatarSummary,
    int? SeatIndex,
    bool IsFocusing,
    DateTime JoinedAt)
{
    public static RoomMemberDto FromEntity(RoomMember member)
    {
        return new RoomMemberDto(
            member.Id,
            member.UserId,
            member.User?.DisplayName ?? "Kullanıcı",
            member.User?.Level ?? 1,
            null,
            member.SeatIndex,
            member.IsFocusing,
            member.JoinedAt);
    }
}

public record RoomInvitationResultDto(
    string RoomCode,
    string RoomName,
    string InviteCode,
    string DirectLink,
    string WhatsAppLink,
    string TelegramLink,
    string ShareMessage);

public record LeftRoomDto(string RoomCode, int? VacatedSeat);

public record JoinRoomResultDto(
    bool Success,
    string Message,
    StudyRoomDto? Room)
{
    // Yalnizca sunucu tarafi yayin icin kullanilir, istemciye serilestirilmez
    [JsonIgnore]
    public bool NewlyJoined { get; init; }

    [JsonIgnore]
    public IReadOnlyList<LeftRoomDto> LeftRooms { get; init; } = Array.Empty<LeftRoomDto>();
}

public record SitSeatResultDto(
    bool Success,
    string Message,
    int? SeatIndex)
{
    [JsonIgnore]
    public int? PreviousSeatIndex { get; init; }
}

public record RoomReactionDto(
    Guid RoomId,
    Guid UserId,
    string DisplayName,
    string Reaction,
    DateTime SentAt);
