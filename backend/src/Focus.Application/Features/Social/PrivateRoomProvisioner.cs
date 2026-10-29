using Focus.Application.Common.Exceptions;
using Focus.Application.Common.Interfaces;
using Focus.Domain.Common;
using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Social;

public static class PrivateRoomProvisioner
{
    public const string RoomCodePrefix = "FOC-";
    public const string InviteCodePrefix = "INV-";
    private const int MaxAttempts = 5;

    // Kullanicinin ozel odasini dondurur, yoksa olusturur; sahip baska odada degilse uye olarak eklenir
    public static async Task<StudyRoom> EnsureAsync(IApplicationDbContext context, User owner, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var existing = await LoadOwnedRoomAsync(context, owner.Id, cancellationToken);
            if (existing != null)
            {
                existing.UpdateProCapacity(owner.IsPro);
                await AddOwnerMemberIfIdleAsync(context, existing, owner.Id, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                return existing;
            }

            var code = SecureCode.Generate(RoomCodePrefix);
            if (await context.StudyRooms.AnyAsync(r => r.Code == code, cancellationToken)) continue;

            var room = StudyRoom.CreatePrivateRoom(owner.Id, $"{owner.DisplayName}'in Odası", code, owner.IsPro);
            context.StudyRooms.Add(room);
            await AddOwnerMemberIfIdleAsync(context, room, owner.Id, cancellationToken);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
                return room;
            }
            catch (DbUpdateException)
            {
                // Kod cakismasi veya es zamanli ikinci olusturma: kayit atilir ve yeniden denenir
                context.DiscardPendingChanges();
            }
        }

        throw new ConflictException("Oda şu an oluşturulamadı. Lütfen tekrar deneyin.");
    }

    public static async Task<RoomInvitation> CreateInvitationAsync(
        IApplicationDbContext context,
        StudyRoom room,
        Guid inviterId,
        Guid? inviteeUserId,
        string? inviteeUsername,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var code = SecureCode.Generate(InviteCodePrefix, 10);
            if (await context.RoomInvitations.AnyAsync(i => i.InviteCode == code, cancellationToken)) continue;

            var invitation = new RoomInvitation(room.Id, inviterId, code, inviteeUserId, inviteeUsername, validHours: 72);
            context.RoomInvitations.Add(invitation);
            await context.SaveChangesAsync(cancellationToken);
            return invitation;
        }

        throw new ConflictException("Davet kodu şu an üretilemedi. Lütfen tekrar deneyin.");
    }

    private static Task<StudyRoom?> LoadOwnedRoomAsync(IApplicationDbContext context, Guid ownerId, CancellationToken cancellationToken)
    {
        return context.StudyRooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.OwnerUserId == ownerId && r.Type == Domain.Enums.StudyRoomType.PrivateHome, cancellationToken);
    }

    private static async Task AddOwnerMemberIfIdleAsync(IApplicationDbContext context, StudyRoom room, Guid ownerId, CancellationToken cancellationToken)
    {
        if (room.HasMember(ownerId)) return;

        var memberElsewhere = await context.RoomMembers.AnyAsync(m => m.UserId == ownerId, cancellationToken);
        if (memberElsewhere) return;

        context.RoomMembers.Add(room.AddMember(ownerId));
    }
}
