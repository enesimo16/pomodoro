using System.Net;
using Focus.Application.Features.Social.DTOs;
using Focus.Domain.Entities;

namespace Focus.Application.Features.Social;

internal static class RoomInvitationResultFactory
{
    private const string DefaultBaseUrl = "https://focus.app";

    public static RoomInvitationResultDto Create(StudyRoom room, RoomInvitation invitation, string? baseAppUrl, string? inviteeDisplayName)
    {
        var baseUrl = !string.IsNullOrWhiteSpace(baseAppUrl) ? baseAppUrl.TrimEnd('/') : DefaultBaseUrl;
        var directLink = $"{baseUrl}/join/{room.Code}?invite={invitation.InviteCode}";
        var greeting = inviteeDisplayName != null ? $"Selam {inviteeDisplayName}!" : "Selam!";
        var shareMessage = $"{greeting} Seni piksel çalışma odama davet ediyorum. Beraber odaklanalım ve +%{StudyRoom.CoWorkingBonusPercent} odaklanma bonusu kazanalım! Odaya katılmak için tıkla: {directLink}";

        var whatsAppLink = $"https://api.whatsapp.com/send?text={WebUtility.UrlEncode(shareMessage)}";
        var telegramLink = $"https://t.me/share/url?url={WebUtility.UrlEncode(directLink)}&text={WebUtility.UrlEncode("Piksel Çalışma Odası Daveti")}";

        return new RoomInvitationResultDto(
            room.Code,
            room.Name,
            invitation.InviteCode,
            directLink,
            whatsAppLink,
            telegramLink,
            shareMessage);
    }
}
