using Focus.Domain.Entities;
using Focus.Domain.Enums;

namespace Focus.Application.Features.Session.DTOs;

public record FocusSessionDto(
    Guid Id,
    Guid UserId,
    Guid? RoomId,
    string Kind,
    string Status,
    int FocusMinutes,
    int BreakMinutes,
    int PlannedMinutes,
    int CurrentRound,
    int TargetRounds,
    DateTime StartedAt,
    DateTime PlannedEndAt,
    DateTime? EndedAt,
    DateTime? PausedAt,
    int TotalPausedSeconds,
    int NetDurationSeconds,
    int RemainingSeconds,
    int ExtensionCount,
    int XpEarned,
    int CoinsEarned,
    string? ThemeId,
    string? MixPresetId,
    string NextKind,
    int NextRound,
    bool IsCycleFinished)
{
    public static FocusSessionDto FromEntity(FocusSession session)
    {
        var now = DateTime.UtcNow;
        var remainingSeconds = 0;

        if (session.Status == SessionStatus.Running)
        {
            var diff = (session.PlannedEndAt - now).TotalSeconds;
            remainingSeconds = Math.Max(0, (int)diff);
        }
        else if (session.Status == SessionStatus.Paused && session.PausedAt.HasValue)
        {
            var diff = (session.PlannedEndAt - session.PausedAt.Value).TotalSeconds;
            remainingSeconds = Math.Max(0, (int)diff);
        }

        var (nextKind, nextRound, isCycleFinished) = session.GetNextCycleStep();

        return new FocusSessionDto(
            session.Id,
            session.UserId,
            session.RoomId,
            session.Kind.ToString(),
            session.Status.ToString(),
            session.FocusMinutes,
            session.BreakMinutes,
            session.PlannedMinutes,
            session.CurrentRound,
            session.TargetRounds,
            session.StartedAt,
            session.PlannedEndAt,
            session.EndedAt,
            session.PausedAt,
            session.TotalPausedSeconds,
            session.NetDurationSeconds,
            remainingSeconds,
            session.ExtensionCount,
            session.XpEarned,
            session.CoinsEarned,
            session.ThemeId,
            session.MixPresetId,
            nextKind.ToString(),
            nextRound,
            isCycleFinished);
    }
}

public record SessionCompletionResultDto(
    FocusSessionDto Session,
    int XpEarned,
    int CoinsEarned,
    int NewLevel,
    long TotalXp,
    int TotalCoins,
    bool LevelUp,
    string NextKind,
    int NextRound,
    bool IsCycleFinished);
