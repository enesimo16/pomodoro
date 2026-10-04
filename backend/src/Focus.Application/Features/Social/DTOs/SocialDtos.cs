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
    List<RoomMemberDto> Members);

public record RoomMemberDto(
    Guid Id,
    Guid UserId,
    string DisplayName,
    int Level,
    string? AvatarSummary,
    int? SeatIndex,
    bool IsFocusing,
    DateTime JoinedAt);

public record RoomInvitationResultDto(
    string RoomCode,
    string RoomName,
    string InviteCode,
    string DirectLink,
    string WhatsAppLink,
    string TelegramLink,
    string ShareMessage);

public record JoinRoomResultDto(
    bool Success,
    string Message,
    StudyRoomDto? Room);

public record SitSeatResultDto(
    bool Success,
    string Message,
    int? SeatIndex);

public record RoomReactionDto(
    Guid RoomId,
    Guid UserId,
    string DisplayName,
    string Reaction,
    DateTime SentAt);
