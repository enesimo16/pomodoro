using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Social.DTOs;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Social.Commands;

public record InviteUserByUsernameCommand(
    Guid InviterUserId,
    string TargetUsername,
    string? BaseAppUrl = null,
    Guid? TargetUserId = null) : IRequest<RoomInvitationResultDto>;

public class InviteUserByUsernameCommandHandler : IRequestHandler<InviteUserByUsernameCommand, RoomInvitationResultDto>
{
    public const string InviteFailedMessage = "Davet gönderilemedi. Kullanıcı bilgisini kontrol edip tekrar deneyin.";

    private readonly IApplicationDbContext _context;

    public InviteUserByUsernameCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RoomInvitationResultDto> Handle(InviteUserByUsernameCommand request, CancellationToken cancellationToken)
    {
        var inviter = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.InviterUserId, cancellationToken)
            ?? throw new InvalidOperationException(InviteFailedMessage);

        var targetUser = await ResolveTargetAsync(request, cancellationToken);

        if (targetUser.Id == inviter.Id)
        {
            throw new InvalidOperationException("Kendinizi odaya davet edemezsiniz.");
        }

        var room = await PrivateRoomProvisioner.EnsureAsync(_context, inviter, cancellationToken);

        if (!room.CanJoin(inviter.IsPro, out var errorMessage))
        {
            throw new InvalidOperationException(errorMessage);
        }

        var invitation = await PrivateRoomProvisioner.CreateInvitationAsync(
            _context, room, inviter.Id, targetUser.Id, targetUser.DisplayName, cancellationToken);

        _context.Notifications.Add(Notification.Create(
            targetUser.Id,
            NotificationType.RoomInvite,
            "Oda Daveti",
            $"{inviter.DisplayName} seni piksel çalışma odasına davet etti.",
            $"/room?room={room.Code}&invite={invitation.InviteCode}"));
        await _context.SaveChangesAsync(cancellationToken);

        return RoomInvitationResultFactory.Create(room, invitation, request.BaseAppUrl, targetUser.DisplayName);
    }

    private async Task<User> ResolveTargetAsync(InviteUserByUsernameCommand request, CancellationToken cancellationToken)
    {
        if (request.TargetUserId.HasValue)
        {
            return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.TargetUserId.Value, cancellationToken)
                ?? throw new InvalidOperationException(InviteFailedMessage);
        }

        // DisplayName benzersiz degildir: tam eslesme aranir, birden fazla sonuc varsa kimlik istenir
        var name = request.TargetUsername.Trim();
        var matches = await _context.Users.AsNoTracking()
            .Where(u => u.DisplayName == name)
            .Take(2)
            .ToListAsync(cancellationToken);

        return matches.Count switch
        {
            1 => matches[0],
            0 => throw new InvalidOperationException(InviteFailedMessage),
            _ => throw new InvalidOperationException("Bu ada sahip birden fazla kullanıcı var. Lütfen kullanıcı kimliğiyle davet edin.")
        };
    }
}
