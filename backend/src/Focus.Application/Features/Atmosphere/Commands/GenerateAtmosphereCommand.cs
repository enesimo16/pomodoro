namespace Focus.Application.Features.Atmosphere.Commands;

using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Atmosphere.DTOs;
using MediatR;

public record GenerateAtmosphereCommand(
    Guid UserId,
    GenerateAtmosphereRequestDto Request
) : IRequest<GeneratedAtmosphereDto>;

public class GenerateAtmosphereCommandHandler : IRequestHandler<GenerateAtmosphereCommand, GeneratedAtmosphereDto>
{
    private readonly IAtmosphereAiService _atmosphereAiService;

    public GenerateAtmosphereCommandHandler(IAtmosphereAiService atmosphereAiService)
    {
        _atmosphereAiService = atmosphereAiService;
    }

    public async Task<GeneratedAtmosphereDto> Handle(GenerateAtmosphereCommand command, CancellationToken cancellationToken)
    {
        return await _atmosphereAiService.GenerateAtmosphereAsync(
            command.UserId,
            command.Request,
            cancellationToken);
    }
}
