using Focus.Application.Common.Exceptions;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Social.DTOs;
using Focus.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Social.Commands;

public record JoinRoomCommand(Guid UserId, string RoomCode, string? InviteCode = null) : IRequest<JoinRoomResultDto>;

public class JoinRoomCommandHandler : IRequestHandler<JoinRoomCommand, JoinRoomResultDto>
{
    public const string RoomUnavailableMessage = "Çalışma odası bulunamadı veya davet geçersiz.";
    private const int MaxAttempts = 3;

    private readonly IApplicationDbContext _context;

    public JoinRoomCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<JoinRoomResultDto> Handle(JoinRoomCommand request, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            try
            {
                return await TryJoinAsync(request, cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Kapasite surumu veya (RoomId, UserId) benzersiz indeksi cakisti: yeniden okunur, mevcut uyelik varsa basari doner
                _context.DiscardPendingChanges();
            }
        }

        throw new ConflictException("Oda şu anda yoğun. Lütfen tekrar deneyin.");
    }

    private async Task<JoinRoomResultDto> TryJoinAsync(JoinRoomCommand request, CancellationToken cancellationToken)
    {
        var code = request.RoomCode.ToUpperInvariant().Trim();
        var room = await _context.StudyRooms
            .Include(r => r.OwnerUser)
            .Include(r => r.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (room == null || user == null)
        {
            return new JoinRoomResultDto(false, RoomUnavailableMessage, null);
        }

        var existingMember = room.Members.FirstOrDefault(m => m.UserId == request.UserId);
        if (existingMember != null)
        {
            existingMember.Touch();
            await _context.SaveChangesAsync(cancellationToken);
            return Success(room, newlyJoined: false, Array.Empty<LeftRoomDto>());
        }

        var isOwner = room.OwnerUserId == request.UserId;
        RoomInvitation? invitation = null;
        if (room.IsPrivate && !isOwner)
        {
            invitation = await FindValidInvitationAsync(room, request, cancellationToken);
            if (invitation == null)
            {
                return new JoinRoomResultDto(false, RoomUnavailableMessage, null);
            }
        }

        var isOwnerPro = room.OwnerUser?.IsPro ?? false;
        if (!isOwner && !room.CanJoin(isOwnerPro, out var errorMessage))
        {
            return new JoinRoomResultDto(false, errorMessage!, null);
        }

        var leftRooms = await LeaveOtherRoomsAsync(request.UserId, room.Id, cancellationToken);

        var member = room.AddMember(request.UserId);
        _context.RoomMembers.Add(member);
        invitation?.MarkAccepted();

        await _context.SaveChangesAsync(cancellationToken);
        return Success(room, newlyJoined: true, leftRooms);
    }

    // Acik (kisiye ozel olmayan) davet suresi dolana kadar tekrar kullanilabilir; kisiye ozel davet tek kullanimliktir
    private async Task<RoomInvitation?> FindValidInvitationAsync(StudyRoom room, JoinRoomCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.InviteCode)) return null;

        var inviteCode = request.InviteCode.Trim().ToUpperInvariant();
        var invitation = await _context.RoomInvitations.FirstOrDefaultAsync(i => i.InviteCode == inviteCode, cancellationToken);
        if (invitation == null || invitation.RoomId != room.Id || invitation.IsExpired) return null;

        if (invitation.InviteeUserId == null) return invitation;
        return invitation.InviteeUserId == request.UserId && !invitation.IsAccepted ? invitation : null;
    }

    private async Task<List<LeftRoomDto>> LeaveOtherRoomsAsync(Guid userId, Guid targetRoomId, CancellationToken cancellationToken)
    {
        var otherRooms = await _context.StudyRooms
            .Include(r => r.Members)
            .Where(r => r.Id != targetRoomId && r.Members.Any(m => m.UserId == userId))
            .ToListAsync(cancellationToken);

        var left = new List<LeftRoomDto>();
        foreach (var other in otherRooms)
        {
            var removed = other.RemoveMember(userId);
            if (removed == null) continue;

            _context.RoomMembers.Remove(removed);
            left.Add(new LeftRoomDto(other.Code, removed.SeatIndex));
        }

        return left;
    }

    private static JoinRoomResultDto Success(StudyRoom room, bool newlyJoined, IReadOnlyList<LeftRoomDto> leftRooms)
    {
        return new JoinRoomResultDto(true, $"'{room.Name}' odasına başarıyla katıldınız!", StudyRoomDto.FromEntity(room))
        {
            NewlyJoined = newlyJoined,
            LeftRooms = leftRooms
        };
    }
}
