using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Auth.DTOs;
using Focus.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Avatar.Commands;

public record UpdateAvatarAppearanceCommand(
    Guid UserId,
    string? SkinTone,
    string? HairStyle,
    string? HairColor,
    string? TopItemId,
    string? BottomItemId,
    string? HatItemId,
    string? GlassesItemId,
    string? ShoesItemId,
    bool ClearHat = false,
    bool ClearGlasses = false) : IRequest<AvatarDto>;

public class UpdateAvatarAppearanceCommandHandler : IRequestHandler<UpdateAvatarAppearanceCommand, AvatarDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateAvatarAppearanceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AvatarDto> Handle(UpdateAvatarAppearanceCommand request, CancellationToken cancellationToken)
    {
        var avatar = await _context.UserAvatars
            .FirstOrDefaultAsync(a => a.UserId == request.UserId, cancellationToken);

        if (avatar == null)
        {
            avatar = UserAvatar.CreateDefault(request.UserId);
            _context.UserAvatars.Add(avatar);
        }

        avatar.UpdateAppearance(
            request.SkinTone,
            request.HairStyle,
            request.HairColor,
            request.TopItemId,
            request.BottomItemId,
            request.HatItemId,
            request.GlassesItemId,
            request.ShoesItemId,
            request.ClearHat,
            request.ClearGlasses);

        await _context.SaveChangesAsync(cancellationToken);

        return AvatarDto.FromEntity(avatar);
    }
}
