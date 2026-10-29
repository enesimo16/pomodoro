namespace Focus.Domain.Common;

public static class LevelCurve
{
    private const double Coefficient = 150.0;
    private const double Exponent = 1.6;

    // docs/algoritmalar.md 2.2: L. seviyeye ulasmak icin gereken toplam XP = 150 * L^1.6
    public static long XpRequiredForLevel(int level)
    {
        if (level <= 1) return 0;

        return (long)Math.Round(Coefficient * Math.Pow(level, Exponent));
    }

    public static int LevelForXp(long xp)
    {
        if (xp <= 0) return 1;

        var level = Math.Max(1, (int)Math.Floor(Math.Pow(xp / Coefficient, 1.0 / Exponent)));

        while (XpRequiredForLevel(level + 1) <= xp) level++;
        while (level > 1 && XpRequiredForLevel(level) > xp) level--;

        return level;
    }
}
