using Focus.Application.Common.Exceptions;
using Focus.Application.Common.Extensions;
using Focus.Application.Common.Interfaces;
using Focus.Application.Common.Options;
using Focus.Application.Features.Session.DTOs;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Focus.Application.Features.Session.Commands;

public record ExtendSessionCommand(Guid UserId, int ExtraMinutes = 10, Guid? SessionId = null) : IRequest<FocusSessionDto?>;

public class ExtendSessionCommandHandler : IRequestHandler<ExtendSessionCommand, FocusSessionDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly AlgorithmOptions _options;
    private readonly TimeProvider _clock;

    public ExtendSessionCommandHandler(IApplicationDbContext context, IOptions<AlgorithmOptions> options, TimeProvider clock)
    {
        _context = context;
        _options = options.Value;
        _clock = clock;
    }

    public async Task<FocusSessionDto?> Handle(ExtendSessionCommand request, CancellationToken cancellationToken)
    {
        var query = _context.FocusSessions
            .Where(s => s.UserId == request.UserId && (s.Status == SessionStatus.Running || s.Status == SessionStatus.Paused));

        if (request.SessionId.HasValue)
        {
            query = query.Where(s => s.Id == request.SessionId.Value);
        }

        var session = await query.OrderByDescending(s => s.StartedAt).FirstOrDefaultAsync(cancellationToken);
        if (session == null)
        {
            return null;
        }

        var outcome = session.Extend(request.ExtraMinutes, _options.MaxExtensions, _options.MaxContinuousFocusMinutes);
        if (outcome != ExtendOutcome.Extended)
        {
            throw new ConflictException(DescribeRejection(outcome));
        }

        await _context.SaveChangesOrConflictAsync(cancellationToken);

        return FocusSessionDto.FromEntity(session, _clock.GetUtcNow().UtcDateTime);
    }

    private string DescribeRejection(ExtendOutcome outcome) => outcome switch
    {
        ExtendOutcome.NotAllowedForBreak => "Mola seansları uzatılamaz.",
        ExtendOutcome.ExtensionLimitReached => $"En fazla {_options.MaxExtensions} kez uzatabilirsin. Mola vakti.",
        ExtendOutcome.ContinuousLimitReached => $"Kesintisiz odak süresi {_options.MaxContinuousFocusMinutes} dakikaya ulaştı. Mola vakti.",
        _ => "Seans uzatılamadı."
    };
}
