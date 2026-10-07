namespace Focus.Application.Features.Atmosphere.Commands;

using Focus.Application.Common.Interfaces;
using MediatR;

public record ContributeSessionInsightCommand(
    Guid UserId,
    Guid? SessionId = null,
    int? PlannedDurationMinutes = null,
    int? FocusQuality = null,
    string? DistractionOrLearningNotes = null
) : IRequest<bool>;

public class ContributeSessionInsightCommandHandler : IRequestHandler<ContributeSessionInsightCommand, bool>
{
    private readonly IAtmosphereAiService _atmosphereAiService;

    public ContributeSessionInsightCommandHandler(IAtmosphereAiService atmosphereAiService)
    {
        _atmosphereAiService = atmosphereAiService;
    }

    public async Task<bool> Handle(ContributeSessionInsightCommand command, CancellationToken cancellationToken)
    {
        return await _atmosphereAiService.ContributeSessionInsightAsync(
            command.UserId,
            command.SessionId,
            command.PlannedDurationMinutes,
            command.FocusQuality,
            command.DistractionOrLearningNotes,
            cancellationToken);
    }
}
