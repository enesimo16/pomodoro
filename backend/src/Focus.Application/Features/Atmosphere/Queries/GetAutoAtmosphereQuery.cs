namespace Focus.Application.Features.Atmosphere.Queries;

using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Atmosphere.DTOs;
using MediatR;

public record GetAutoAtmosphereQuery(Guid UserId) : IRequest<GeneratedAtmosphereDto>;

public class GetAutoAtmosphereQueryHandler : IRequestHandler<GetAutoAtmosphereQuery, GeneratedAtmosphereDto>
{
    private readonly IAtmosphereAiService _atmosphereAiService;

    public GetAutoAtmosphereQueryHandler(IAtmosphereAiService atmosphereAiService)
    {
        _atmosphereAiService = atmosphereAiService;
    }

    public async Task<GeneratedAtmosphereDto> Handle(GetAutoAtmosphereQuery query, CancellationToken cancellationToken)
    {
        return await _atmosphereAiService.GenerateAutoAtmosphereAsync(query.UserId, cancellationToken);
    }
}
