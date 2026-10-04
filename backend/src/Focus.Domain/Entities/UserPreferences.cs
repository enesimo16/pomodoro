using Focus.Domain.Common;

namespace Focus.Domain.Entities;

public class UserPreferences : BaseEntity<Guid>
{
    public Guid UserId { get; private set; }
    public int DefaultFocusMinutes { get; private set; } = 25;
    public int ShortBreakMinutes { get; private set; } = 5;
    public int LongBreakMinutes { get; private set; } = 15;
    public int TargetRounds { get; private set; } = 4;
    public bool FlowShieldEnabled { get; private set; }
    public bool WeatherSyncEnabled { get; private set; } = true;
    public string? CityKey { get; private set; }
    public bool AgentEnabled { get; private set; } = true;

    // Navigation
    public User User { get; private set; } = null!;

    // EF Core constructor
    private UserPreferences() { }

    public static UserPreferences CreateDefault(Guid userId)
    {
        return new UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DefaultFocusMinutes = 25,
            ShortBreakMinutes = 5,
            LongBreakMinutes = 15,
            TargetRounds = 4,
            FlowShieldEnabled = false,
            WeatherSyncEnabled = true,
            AgentEnabled = true
        };
    }

    public void UpdatePreferences(
        int defaultFocusMinutes,
        int shortBreakMinutes,
        int longBreakMinutes,
        bool flowShieldEnabled,
        bool weatherSyncEnabled,
        string? cityKey,
        bool agentEnabled,
        int targetRounds = 4)
    {
        DefaultFocusMinutes = defaultFocusMinutes;
        ShortBreakMinutes = shortBreakMinutes;
        LongBreakMinutes = longBreakMinutes;
        FlowShieldEnabled = flowShieldEnabled;
        WeatherSyncEnabled = weatherSyncEnabled;
        CityKey = cityKey;
        AgentEnabled = agentEnabled;
        TargetRounds = Math.Clamp(targetRounds, 1, 20);
    }
}
