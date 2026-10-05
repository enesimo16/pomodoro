using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Coach.DTOs;
using MediatR;

namespace Focus.Application.Features.Coach.Commands;

public record SendCoachChatCommand(Guid UserId, string Message) : IRequest<CoachChatResponseDto>;

public class SendCoachChatCommandHandler : IRequestHandler<SendCoachChatCommand, CoachChatResponseDto>
{
    private readonly ICoachChatService _coachChatService;

    public SendCoachChatCommandHandler(ICoachChatService coachChatService)
    {
        _coachChatService = coachChatService;
    }

    public async Task<CoachChatResponseDto> Handle(SendCoachChatCommand request, CancellationToken cancellationToken)
    {
        return await _coachChatService.GenerateCoachResponseAsync(request.UserId, request.Message, cancellationToken);
    }
}
