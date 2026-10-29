using FluentAssertions;
using Focus.Infrastructure.Services;
using Xunit;

namespace Focus.UnitTests.Coach;

public class AnonymizationServiceTests
{
    private readonly AnonymizationService _service = new();

    [Fact]
    public void AnonymizeAndGeneralize_StripsEmailsAndPhones()
    {
        var input = "Sorularınız için test.user@example.com veya 05551234567 numarasından bana ulaşın.";
        var sanitized = _service.AnonymizeAndGeneralize(input);

        sanitized.Should().NotContain("test.user@example.com");
        sanitized.Should().NotContain("05551234567");
        sanitized.Should().Contain("[kullanici-eposta]");
        sanitized.Should().Contain("[telefon]");
    }

    [Fact]
    public void AnonymizeAndGeneralize_StripsUserDisplayName()
    {
        var input = "Bugün Mehmet ile birlikte proje üzerinde çalıştım.";
        var sanitized = _service.AnonymizeAndGeneralize(input, "Mehmet");

        sanitized.Should().NotContain("Mehmet");
        sanitized.Should().Contain("Kullanici");
    }

    [Fact]
    public void AnonymizeAndGeneralize_GeneralizesFirstPersonPronouns()
    {
        var input = "Ben 50 dakika çalıştım ve çok iyi odaklandım.";
        var sanitized = _service.AnonymizeAndGeneralize(input);

        sanitized.ToLowerInvariant().Should().NotContain("ben");
        sanitized.Should().Contain("kullanici");
    }

    [Theory]
    [InlineData("Beni +90 555 123 45 67 numarasindan ara")]
    [InlineData("Telefonum 0555-123-45-67")]
    [InlineData("Numara (0555) 123 45 67 olarak kayitli")]
    public void AnonymizeAndGeneralize_StripsFormattedPhoneNumbers(string input)
    {
        var sanitized = _service.AnonymizeAndGeneralize(input);

        sanitized.Should().Contain("[telefon]");
        sanitized.Should().NotContain("123");
    }

    [Fact]
    public void AnonymizeAndGeneralize_StripsIban()
    {
        var sanitized = _service.AnonymizeAndGeneralize("Hesap TR33 0006 1005 1978 6457 8413 26 uzerine yatsin");

        sanitized.Should().Contain("[iban]");
        sanitized.Should().NotContain("0006");
    }

    [Fact]
    public void AnonymizeAndGeneralize_KeepsPlainDurationNumbers()
    {
        var sanitized = _service.AnonymizeAndGeneralize("25 dakika calisip 5 dakika mola, sonra 45 10 45 ritmi");

        sanitized.Should().NotContain("[telefon]");
    }

    [Fact]
    public void AnonymizeAndGeneralize_DisplayNameMatchesWholeWordsOnly()
    {
        var sanitized = _service.AnonymizeAndGeneralize("Ali ile Alinan notlari gozden gecirdik", "Ali");

        sanitized.Should().Contain("Alinan");
        sanitized.Should().StartWith("Kullanici ile");
    }
}
