using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Session.DTOs;
using Focus.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Focus.Application.Features.Session.Commands;

public record ResumeSessionCommand(Guid UserId, Guid? SessionId = null) : IRequest<FocusSessionDto?>;

public class ResumeSessionCommandHandler : IRequestHandler<ResumeSessionCommand, FocusSessionDto?>
{
    private readonly IApplicationDbContext _context;

    public ResumeSessionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FocusSessionDto?> Handle(ResumeSessionCommand request, CancellationToken cancellationToken)
    {
        var query = _context.FocusSessions
            .Where(s => s.UserId == request.UserId && s.Status == SessionStatus.Paused);

        if (request.SessionId.HasValue)
        {
            query = query.Where(s => s.Id == request.SessionId.Value);
        }

        var session = await query.OrderByDescending(s => s.StartedAt).FirstOrDefaultAsync(cancellationToken);
        if (session == null)
        {
            return null;
        }

        session.Resume();
        await _context.SaveChangesAsync(cancellationToken);

        return FocusSessionDto.FromEntity(session);
    }
}
