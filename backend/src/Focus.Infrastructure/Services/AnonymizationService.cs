using System.Text.RegularExpressions;
using Focus.Application.Common.Interfaces;

namespace Focus.Infrastructure.Services;

public partial class AnonymizationService : IAnonymizationService
{
    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"@\w+", RegexOptions.IgnoreCase)]
    private static partial Regex MentionRegex();

    [GeneratedRegex(@"(https?:\/\/[^\s]+)", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"\b\d{10,12}\b")]
    private static partial Regex PhoneRegex();

    public string AnonymizeAndGeneralize(string text, string? userDisplayName = null)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var cleaned = text.Trim();

        // 1. E-posta adreslerini temizle
        cleaned = EmailRegex().Replace(cleaned, "[kullanici-eposta]");

        // 2. Telefon numaralarini temizle
        cleaned = PhoneRegex().Replace(cleaned, "[telefon]");

        // 3. Web baglantilarini temizle
        cleaned = UrlRegex().Replace(cleaned, "[baglanti]");

        // 4. @Kullanici etiketlerini temizle
        cleaned = MentionRegex().Replace(cleaned, "[kisi]");

        // 5. Kullanici gorunen adini temizle
        if (!string.IsNullOrWhiteSpace(userDisplayName))
        {
            cleaned = Regex.Replace(cleaned, Regex.Escape(userDisplayName), "Kullanici", RegexOptions.IgnoreCase);
        }

        // 6. Birinci tekil sahis kalıplarini genel stratejiye donustur
        cleaned = Regex.Replace(cleaned, @"\b(ben|benim|kendim|bana|bende)\b", "kullanici", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"\b(yaptim|ettim|calistim|odaklandim)\b", "uygulandiginda", RegexOptions.IgnoreCase);

        return cleaned.Trim();
    }
}
