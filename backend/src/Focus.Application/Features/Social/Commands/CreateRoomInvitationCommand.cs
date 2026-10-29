using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Social.DTOs;
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
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Kullanıcı bulunamadı.");

        var room = await PrivateRoomProvisioner.EnsureAsync(_context, user, cancellationToken);
        var invitation = await PrivateRoomProvisioner.CreateInvitationAsync(_context, room, user.Id, null, null, cancellationToken);

        return RoomInvitationResultFactory.Create(room, invitation, request.BaseAppUrl, null);
    }
}
