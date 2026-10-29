using FluentAssertions;
using FluentValidation;
using Focus.Application.Common.Behaviors;
using MediatR;
using Xunit;

namespace Focus.UnitTests.Application.Common.Behaviors;

public class ValidationBehaviorTests
{
    private sealed record SampleRequest(string? Name) : IRequest<string>;

    private sealed class SampleValidator : AbstractValidator<SampleRequest>
    {
        public SampleValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Ad zorunlu.");
        }
    }

    [Fact]
    public async Task Handle_WithoutValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>([]);

        var result = await behavior.Handle(new SampleRequest(null), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_WithValidRequest_CallsNext()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>([new SampleValidator()]);

        var result = await behavior.Handle(new SampleRequest("Ayse"), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ThrowsValidationExceptionAndSkipsNext()
    {
        var nextCalled = false;
        var behavior = new ValidationBehavior<SampleRequest, string>([new SampleValidator()]);

        var act = () => behavior.Handle(new SampleRequest(""), () =>
        {
            nextCalled = true;
            return Task.FromResult("ok");
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainSingle(e => e.ErrorMessage == "Ad zorunlu.");
        nextCalled.Should().BeFalse();
    }
}
