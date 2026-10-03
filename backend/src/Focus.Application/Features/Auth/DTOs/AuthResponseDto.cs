namespace Focus.Application.Features.Auth.DTOs;

public record AuthResponseDto(
    UserDto User,
    AvatarDto? Avatar,
    PixelRoomDto? Room,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);
