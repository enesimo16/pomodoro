namespace Focus.Application.Common.Options;

// docs/algoritmalar.md varsayilanlari; appsettings "Algorithms" bolumunden ezilebilir
public class AlgorithmOptions
{
    public const string SectionName = "Algorithms";

    public int XpPerFocusMinute { get; set; } = 10;
    public int CoinsPerFocusMinute { get; set; } = 1;
    public int BreakBonusXp { get; set; } = 50;
    public int BreakBonusCoins { get; set; } = 10;
    public decimal BreakMinCompletionRatio { get; set; } = 0.9m;
    public int BreakMaxGapMinutes { get; set; } = 10;
    public int MinRewardedFocusMinutes { get; set; } = 5;
    public int AbandonRewardMinMinutes { get; set; } = 10;
    public int CompletionToleranceSeconds { get; set; } = 15;
    public int DailyCoinCap { get; set; } = 400;
    public int MaxExtensions { get; set; } = 3;
    public int MaxContinuousFocusMinutes { get; set; } = 90;
    public int StaleRunningGraceMinutes { get; set; } = 10;
    public int StalePausedHours { get; set; } = 3;
    public int RoomMemberStaleMinutes { get; set; } = 30;

    // Seri gun sayisi -> XP/Coin carpani
    public Dictionary<int, decimal> StreakMultipliers { get; set; } = new()
    {
        [3] = 1.10m,
        [7] = 1.25m,
        [30] = 1.50m
    };

    public decimal GetStreakMultiplier(int streakDays)
    {
        var multiplier = 1m;
        foreach (var (minDays, value) in StreakMultipliers.OrderBy(t => t.Key))
        {
            if (streakDays >= minDays) multiplier = value;
        }

        return multiplier;
    }
}
