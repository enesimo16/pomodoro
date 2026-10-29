using System.Globalization;

namespace Focus.Admin.Common;

public static class TextHelpers
{
    public static string Initial(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "?";
        }

        var trimmed = name.Trim();
        var first = StringInfo.GetNextTextElement(trimmed);
        return first.ToUpper(CultureInfo.CurrentCulture);
    }
}
