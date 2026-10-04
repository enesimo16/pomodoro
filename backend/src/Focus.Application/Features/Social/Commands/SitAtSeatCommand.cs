using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Social.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Social.Commands;

public record SitAtSeatCommand(Guid UserId, string RoomCode, int SeatIndex) : IRequest<SitSeatResultDto>;

public class SitAtSeatCommandHandler : IRequestHandler<SitAtSeatCommand, SitSeatResultDto>
{
    private readonly IApplicationDbContext _context;

    public SitAtSeatCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SitSeatResultDto> Handle(SitAtSeatCommand request, CancellationToken cancellationToken)
    {
        var code = request.RoomCode.ToUpperInvariant().Trim();
        var room = await _context.StudyRooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

        if (room == null)
        {
            return new SitSeatResultDto(false, "Oda bulunamadı.", null);
        }

        var member = room.Members.FirstOrDefault(m => m.UserId == request.UserId);
        if (member == null)
        {
            return new SitSeatResultDto(false, "Önce bu odaya katılmalısınız.", null);
        }

        var isSeatTaken = room.Members.Any(m => m.UserId != request.UserId && m.SeatIndex == request.SeatIndex);
        if (isSeatTaken)
        {
            return new SitSeatResultDto(false, "Bu çalışma masası/koltuğu dolu. Lütfen başka bir masa seçiniz.", null);
        }

        member.SitAtSeat(request.SeatIndex);
        await _context.SaveChangesAsync(cancellationToken);

        return new SitSeatResultDto(true, $"Masa {request.SeatIndex}'e başarıyla oturdunuz.", request.SeatIndex);
    }
}
