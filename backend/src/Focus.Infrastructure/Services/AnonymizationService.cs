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

    [GeneratedRegex(@"(?<![\w+])\+?\d[\d\s().-]{7,18}\d(?!\w)")]
    private static partial Regex PhoneCandidateRegex();

    [GeneratedRegex(@"\b[A-Z]{2}\d{2}(?:\s?[A-Z0-9]{4}){3,7}(?:\s?[A-Z0-9]{1,4})?\b", RegexOptions.IgnoreCase)]
    private static partial Regex IbanRegex();

    private const int MinPhoneDigits = 10;
    private const int MaxPhoneDigits = 13;

    public string AnonymizeAndGeneralize(string text, string? userDisplayName = null)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var cleaned = text.Trim();

        // 1. E-posta adreslerini temizle
        cleaned = EmailRegex().Replace(cleaned, "[kullanici-eposta]");

        // 2. Web baglantilarini temizle
        cleaned = UrlRegex().Replace(cleaned, "[baglanti]");

        // 3. IBAN ve telefon numaralarini temizle
        cleaned = IbanRegex().Replace(cleaned, "[iban]");
        cleaned = PhoneCandidateRegex().Replace(cleaned, match =>
        {
            var digits = match.Value.Count(char.IsDigit);
            return digits is >= MinPhoneDigits and <= MaxPhoneDigits ? "[telefon]" : match.Value;
        });

        // 4. @Kullanici etiketlerini temizle
        cleaned = MentionRegex().Replace(cleaned, "[kisi]");

        // 5. Kullanici gorunen adini sozcuk siniri gozeterek temizle
        var name = userDisplayName?.Trim();
        if (!string.IsNullOrEmpty(name))
        {
            cleaned = Regex.Replace(cleaned, $@"(?<!\w){Regex.Escape(name)}(?!\w)", "Kullanici", RegexOptions.IgnoreCase);
        }

        // 6. Birinci tekil sahis kalıplarini genel stratejiye donustur
        cleaned = Regex.Replace(cleaned, @"\b(ben|benim|kendim|bana|bende)\b", "kullanici", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"\b(yaptim|ettim|calistim|odaklandim)\b", "uygulandiginda", RegexOptions.IgnoreCase);

        return cleaned.Trim();
    }
}
