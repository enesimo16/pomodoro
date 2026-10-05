using Focus.Application.Features.Coach.DTOs;
using Focus.Domain.Entities;

namespace Focus.Application.Common.Interfaces;

public interface IFocusCoachEngine
{
    CoachAnomalyStatusDto EvaluateAnomalies(
        User user,
        List<FocusSession> recentSessions,
        List<SessionCheckIn> recentCheckIns,
        List<SessionReflection> recentReflections,
        UserStreak? streak);

    double CalculateDailyFatigueScore(List<FocusSession> todaySessions);
    int CalculateWeeklyTotalMinutes(List<FocusSession> past7DaysSessions);
}
