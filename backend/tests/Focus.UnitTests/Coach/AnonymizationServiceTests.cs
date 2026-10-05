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
}
