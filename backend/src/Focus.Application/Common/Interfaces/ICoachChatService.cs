using Focus.Application.Features.Coach.DTOs;

namespace Focus.Application.Common.Interfaces;

public interface ICoachChatService
{
    Task<CoachChatResponseDto> GenerateCoachResponseAsync(Guid userId, string userMessage, CancellationToken cancellationToken = default);
}
