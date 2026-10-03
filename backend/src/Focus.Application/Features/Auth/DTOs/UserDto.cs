using Focus.Domain.Entities;

namespace Focus.Application.Features.Auth.DTOs;

public record UserDto(
    Guid Id,
    string DisplayName,
    string? Email,
    bool IsGuest,
    int Level,
    long CurrentXp,
    string Locale,
    string TimeZoneId,
    DateTime CreatedAt)
{
    public static UserDto FromEntity(User user)
    {
        return new UserDto(
            user.Id,
            user.DisplayName,
            user.Email,
            user.IsGuest,
            user.Level,
            user.CurrentXp,
            user.Locale,
            user.TimeZoneId,
            user.CreatedAt);
    }
}
