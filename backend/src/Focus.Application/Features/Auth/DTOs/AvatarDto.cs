using Focus.Domain.Entities;

namespace Focus.Application.Features.Auth.DTOs;

public record AvatarDto(
    Guid Id,
    Guid UserId,
    string SkinTone,
    string HairStyle,
    string HairColor,
    string? TopItemCatalogId,
    string? BottomItemCatalogId,
    string? HatItemCatalogId,
    string? GlassesItemCatalogId,
    string? ShoesItemCatalogId)
{
    public static AvatarDto FromEntity(UserAvatar avatar)
    {
        return new AvatarDto(
            avatar.Id,
            avatar.UserId,
            avatar.SkinTone,
            avatar.HairStyle,
            avatar.HairColor,
            avatar.TopItemCatalogId,
            avatar.BottomItemCatalogId,
            avatar.HatItemCatalogId,
            avatar.GlassesItemCatalogId,
            avatar.ShoesItemCatalogId);
    }
}
