using System.Text.RegularExpressions;

namespace Focus.Admin.Common;

public static partial class CssSafe
{
    public const string FallbackColor = "#94a3b8";

    public static string Color(string? value, string fallback = FallbackColor)
    {
        return value is not null && HexColor().IsMatch(value) ? value : fallback;
    }

    [GeneratedRegex(@"\A#[0-9a-fA-F]{6}\z")]
    private static partial Regex HexColor();
}
