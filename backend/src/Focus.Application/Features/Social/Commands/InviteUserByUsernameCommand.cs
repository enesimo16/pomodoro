using System.Net;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Social.DTOs;
using Focus.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Social.Commands;

public record InviteUserByUsernameCommand(Guid InviterUserId, string TargetUsername, string? BaseAppUrl = null) : IRequest<RoomInvitationResultDto>;

public class InviteUserByUsernameCommandHandler : IRequestHandler<InviteUserByUsernameCommand, RoomInvitationResultDto>
{
    private readonly IApplicationDbContext _context;

    public InviteUserByUsernameCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RoomInvitationResultDto> Handle(InviteUserByUsernameCommand request, CancellationToken cancellationToken)
    {
        var inviter = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.InviterUserId, cancellationToken);
        if (inviter == null)
        {
            throw new InvalidOperationException("Davet eden kullanıcı bulunamadı.");
        }

        var trimmedName = request.TargetUsername.Trim();
        var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.DisplayName == trimmedName, cancellationToken);
        if (targetUser == null)
        {
            throw new InvalidOperationException($"'{trimmedName}' kullanıcı adına sahip bir kullanıcı bulunamadı.");
        }

        if (targetUser.Id == inviter.Id)
        {
            throw new InvalidOperationException("Kendinizi odaya davet edemezsiniz.");
        }

        var room = await _context.StudyRooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.OwnerUserId == request.InviterUserId, cancellationToken);

        if (room == null)
        {
            var shortCode = "FOC-" + new Random().Next(1000, 9999).ToString(System.Globalization.CultureInfo.InvariantCulture);
            room = StudyRoom.CreatePrivateRoom(inviter.Id, $"{inviter.DisplayName}'in Odası", shortCode, inviter.IsPro);
            _context.StudyRooms.Add(room);
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (!room.CanJoin(inviter.IsPro, out var errorMessage))
        {
            throw new InvalidOperationException(errorMessage);
        }

        var inviteCode = "INV-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var invitation = new RoomInvitation(room.Id, inviter.Id, inviteCode, targetUser.Id, targetUser.DisplayName, validHours: 72);
        _context.RoomInvitations.Add(invitation);
        await _context.SaveChangesAsync(cancellationToken);

        var baseUrl = !string.IsNullOrWhiteSpace(request.BaseAppUrl) ? request.BaseAppUrl.TrimEnd('/') : "https://focus.app";
        var directLink = $"{baseUrl}/join/{room.Code}?invite={inviteCode}";
        var shareMessage = $"Selam {targetUser.DisplayName}! Seni piksel çalışma odama davet ediyorum. Beraber odaklanıp +%10 bonus kazanalım: {directLink}";

        var whatsAppLink = $"https://api.whatsapp.com/send?text={WebUtility.UrlEncode(shareMessage)}";
        var telegramLink = $"https://t.me/share/url?url={WebUtility.UrlEncode(directLink)}&text={WebUtility.UrlEncode("Piksel Çalışma Odası Daveti")}";

        return new RoomInvitationResultDto(
            room.Code,
            room.Name,
            inviteCode,
            directLink,
            whatsAppLink,
            telegramLink,
            shareMessage);
    }
}
