using System.Net;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Social.DTOs;
using Focus.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Social.Commands;

public record CreateRoomInvitationCommand(Guid UserId, string? BaseAppUrl = null) : IRequest<RoomInvitationResultDto>;

public class CreateRoomInvitationCommandHandler : IRequestHandler<CreateRoomInvitationCommand, RoomInvitationResultDto>
{
    private readonly IApplicationDbContext _context;

    public CreateRoomInvitationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RoomInvitationResultDto> Handle(CreateRoomInvitationCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException("Kullanıcı bulunamadı.");
        }

        // Kullanicinin ozel calisma odasini bul veya olustur
        var room = await _context.StudyRooms.FirstOrDefaultAsync(r => r.OwnerUserId == request.UserId, cancellationToken);
        if (room == null)
        {
            var shortCode = "FOC-" + new Random().Next(1000, 9999).ToString(System.Globalization.CultureInfo.InvariantCulture);
            room = StudyRoom.CreatePrivateRoom(user.Id, $"{user.DisplayName}'in Odası", shortCode, user.IsPro);
            var ownerMember = room.AddMember(user.Id);
            _context.StudyRooms.Add(room);
            _context.RoomMembers.Add(ownerMember);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            room.UpdateProCapacity(user.IsPro);
        }

        var inviteCode = "INV-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var invitation = new RoomInvitation(room.Id, user.Id, inviteCode, validHours: 72);
        _context.RoomInvitations.Add(invitation);
        await _context.SaveChangesAsync(cancellationToken);

        var baseUrl = !string.IsNullOrWhiteSpace(request.BaseAppUrl) ? request.BaseAppUrl.TrimEnd('/') : "https://focus.app";
        var directLink = $"{baseUrl}/join/{room.Code}?invite={inviteCode}";
        var shareMessage = $"Selam! Seni piksel çalışma odama davet ediyorum. Beraber odaklanalım ve +%10 odaklanma bonusu kazanalım! Odaya katılmak için tıkla: {directLink}";

        var encodedMsg = WebUtility.UrlEncode(shareMessage);
        var whatsAppLink = $"https://api.whatsapp.com/send?text={encodedMsg}";
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
