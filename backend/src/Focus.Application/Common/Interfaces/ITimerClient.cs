using Focus.Application.Features.Session.DTOs;

namespace Focus.Application.Common.Interfaces;

public interface ITimerClient
{
    Task TimerStarted(FocusSessionDto session);
    Task TimerPaused(FocusSessionDto session);
    Task TimerResumed(FocusSessionDto session);
    Task TimerExtended(FocusSessionDto session);
    Task TimerCompleted(SessionCompletionResultDto result);
    Task TimerAbandoned(FocusSessionDto session);
    Task DeskLightToggled(bool isTurnedOn);
}
