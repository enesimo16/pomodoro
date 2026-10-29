using Focus.Application.Features.Session.Commands;
using Focus.Domain.Enums;
using Xunit;

namespace Focus.UnitTests.Session;

public class SessionValidatorTests
{
    private readonly StartSessionCommandValidator _startValidator = new();
    private readonly ExtendSessionCommandValidator _extendValidator = new();

    private static StartSessionCommand ValidStart() => new(Guid.NewGuid(), SessionKind.Focus, 25, 5, 1, 4, "rain", "lofi");

    [Fact]
    public void StartSession_WithValidValues_Passes()
    {
        Assert.True(_startValidator.Validate(ValidStart()).IsValid);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(0)]
    [InlineData(-1)]
    public void StartSession_WithUndefinedKind_Fails(int kind)
    {
        var result = _startValidator.Validate(ValidStart() with { Kind = (SessionKind)kind });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(StartSessionCommand.Kind));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(181)]
    public void StartSession_WithFocusMinutesOutOfRange_Fails(int minutes)
    {
        var result = _startValidator.Validate(ValidStart() with { FocusMinutes = minutes });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(StartSessionCommand.FocusMinutes));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void StartSession_WithBreakMinutesOutOfRange_Fails(int minutes)
    {
        var result = _startValidator.Validate(ValidStart() with { BreakMinutes = minutes });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(StartSessionCommand.BreakMinutes));
    }

    [Fact]
    public void StartSession_WithRoundsOutOfRange_Fails()
    {
        Assert.False(_startValidator.Validate(ValidStart() with { TargetRounds = 21 }).IsValid);
        Assert.False(_startValidator.Validate(ValidStart() with { TargetRounds = 0 }).IsValid);
        Assert.False(_startValidator.Validate(ValidStart() with { CurrentRound = 5, TargetRounds = 4 }).IsValid);
    }

    [Fact]
    public void StartSession_WithOverlongIdentifiers_Fails()
    {
        var tooLong = new string('x', StartSessionCommandValidator.MaxIdentifierLength + 1);

        Assert.False(_startValidator.Validate(ValidStart() with { ThemeId = tooLong }).IsValid);
        Assert.False(_startValidator.Validate(ValidStart() with { MixPresetId = tooLong }).IsValid);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(30, true)]
    [InlineData(0, false)]
    [InlineData(-10, false)]
    [InlineData(31, false)]
    public void ExtendSession_ValidatesExtraMinutes(int extra, bool expectedValid)
    {
        var result = _extendValidator.Validate(new ExtendSessionCommand(Guid.NewGuid(), extra));

        Assert.Equal(expectedValid, result.IsValid);
    }
}
