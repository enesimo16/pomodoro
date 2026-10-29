using FluentAssertions;
using Focus.Application.Features.Auth.Commands;
using Xunit;

namespace Focus.UnitTests.Application.Features.Auth.Commands;

public class CreateGuestCommandValidatorTests
{
    private readonly CreateGuestCommandValidator _validator = new();

    [Fact]
    public void Validate_WithAllNull_IsValid()
    {
        _validator.Validate(new CreateGuestCommand()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithValidValues_IsValid()
    {
        var command = new CreateGuestCommand("Ahmet", "tr-TR", "Europe/Istanbul");

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithTooLongDisplayName_IsInvalid()
    {
        var command = new CreateGuestCommand(new string('a', 101));

        var result = _validator.Validate(command);

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateGuestCommand.DisplayName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<script>")]
    [InlineData("a>b")]
    public void Validate_WithBlankOrMarkupDisplayName_IsInvalid(string displayName)
    {
        var result = _validator.Validate(new CreateGuestCommand(displayName));

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateGuestCommand.DisplayName));
    }

    [Fact]
    public void Validate_WithTooLongLocale_IsInvalid()
    {
        var command = new CreateGuestCommand(Locale: new string('a', 21));

        var result = _validator.Validate(command);

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateGuestCommand.Locale));
    }

    [Theory]
    [InlineData("Mars/Olympus_Mons")]
    [InlineData("not a zone")]
    public void Validate_WithUnknownTimeZone_IsInvalid(string timeZoneId)
    {
        var command = new CreateGuestCommand(TimeZoneId: timeZoneId);

        var result = _validator.Validate(command);

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateGuestCommand.TimeZoneId));
    }

    [Fact]
    public void Validate_WithTooLongTimeZone_IsInvalid()
    {
        var command = new CreateGuestCommand(TimeZoneId: new string('a', 51));

        var result = _validator.Validate(command);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateGuestCommand.TimeZoneId));
    }
}

public class GoogleLoginCommandValidatorTests
{
    private readonly GoogleLoginCommandValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithBlankToken_IsInvalid(string idToken)
    {
        _validator.Validate(new GoogleLoginCommand(idToken)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithOversizedToken_IsInvalid()
    {
        _validator.Validate(new GoogleLoginCommand(new string('a', 4097))).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithToken_IsValid()
    {
        _validator.Validate(new GoogleLoginCommand("token")).IsValid.Should().BeTrue();
    }
}

public class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator = new();

    [Fact]
    public void Validate_WithEmptyToken_IsInvalid()
    {
        _validator.Validate(new RefreshTokenCommand("")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithOversizedToken_IsInvalid()
    {
        _validator.Validate(new RefreshTokenCommand(new string('a', 513))).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithToken_IsValid()
    {
        _validator.Validate(new RefreshTokenCommand("abc")).IsValid.Should().BeTrue();
    }
}
