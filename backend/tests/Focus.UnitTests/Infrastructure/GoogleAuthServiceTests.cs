using FluentAssertions;
using Focus.Infrastructure.Authentication;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Focus.UnitTests.Infrastructure;

public class GoogleAuthServiceTests
{
    [Fact]
    public void CreateValidationSettings_SetsAudienceToClientId()
    {
        var settings = GoogleAuthService.CreateValidationSettings(new GoogleAuthSettings { ClientId = "client-id.apps.googleusercontent.com" });

        settings.Audience.Should().BeEquivalentTo(new[] { "client-id.apps.googleusercontent.com" });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateValidationSettings_WithoutClientId_Throws(string? clientId)
    {
        var act = () => GoogleAuthService.CreateValidationSettings(new GoogleAuthSettings { ClientId = clientId });

        act.Should().Throw<UnauthorizedAccessException>().WithMessage("Google girişi yapılandırılmamış.");
    }

    [Fact]
    public async Task ValidateTokenAsync_WithoutClientId_RejectsBeforeValidatingToken()
    {
        var service = new GoogleAuthService(Options.Create(new GoogleAuthSettings()), NullLogger<GoogleAuthService>.Instance);

        var act = () => service.ValidateTokenAsync("any-token");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
