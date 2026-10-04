using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Social.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Social.Commands;

public record JoinRoomCommand(Guid UserId, string RoomCode, string? InviteCode = null) : IRequest<JoinRoomResultDto>;

public class JoinRoomCommandHandler : IRequestHandler<JoinRoomCommand, JoinRoomResultDto>
{
    private readonly IApplicationDbContext _context;

    public JoinRoomCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<JoinRoomResultDto> Handle(JoinRoomCommand request, CancellationToken cancellationToken)
    {
        var code = request.RoomCode.ToUpperInvariant().Trim();
        var room = await _context.StudyRooms
            .Include(r => r.OwnerUser)
            .Include(r => r.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

        if (room == null)
        {
            return new JoinRoomResultDto(false, "Çalışma odası bulunamadı.", null);
        }

        var isOwnerPro = room.OwnerUser?.IsPro ?? false;
        var isAlreadyMember = room.Members.Any(m => m.UserId == request.UserId);

        if (!isAlreadyMember && !room.CanJoin(isOwnerPro, out var errorMessage))
        {
            return new JoinRoomResultDto(false, errorMessage!, null);
        }

        if (!isAlreadyMember)
        {
            var member = room.AddMember(request.UserId);
            _context.RoomMembers.Add(member);
        }
        else
        {
            room.AddMember(request.UserId);
        }

        if (!string.IsNullOrWhiteSpace(request.InviteCode))
        {
            var invite = await _context.RoomInvitations.FirstOrDefaultAsync(i => i.InviteCode == request.InviteCode.Trim(), cancellationToken);
            if (invite != null)
            {
                invite.MarkAccepted();
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Odayı DTO olarak döndür
        var membersDto = room.Members.Select(m => new RoomMemberDto(
            m.Id,
            m.UserId,
            m.User?.DisplayName ?? "Kullanıcı",
            m.User?.Level ?? 1,
            null,
            m.SeatIndex,
            m.IsFocusing,
            m.JoinedAt
        )).ToList();

        var roomDto = new StudyRoomDto(
            room.Id,
            room.Code,
            room.Name,
            room.Description,
            room.Type.ToString(),
            room.OwnerUserId,
            room.OwnerUser?.DisplayName,
            room.Members.Count,
            room.MaxCapacity,
            room.IsPrivate,
            room.ThemeId,
            room.MusicTrackId,
            room.CalculateCoWorkingBonusPercent(),
            membersDto
        );

        return new JoinRoomResultDto(true, $"'{room.Name}' odasına başarıyla katıldınız!", roomDto);
    }
}
