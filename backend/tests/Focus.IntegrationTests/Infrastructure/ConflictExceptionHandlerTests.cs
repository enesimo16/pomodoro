using System.Text.Json;
using FluentAssertions;
using Focus.Application.Common.Exceptions;
using Focus.WebAPI.ExceptionHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Focus.IntegrationTests.Infrastructure;

public class ConflictExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_WithConflictException_WritesProblemDetailsWith409()
    {
        var services = new ServiceCollection().AddOptions().AddLogging().AddProblemDetails().BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = services };
        httpContext.Response.Body = new MemoryStream();
        var handler = new ConflictExceptionHandler(services.GetRequiredService<IProblemDetailsService>());

        var handled = await handler.TryHandleAsync(httpContext, new ConflictException("Zaten aktif bir seansın var."), CancellationToken.None);

        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        httpContext.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);
        document.RootElement.GetProperty("title").GetString().Should().Be("Zaten aktif bir seansın var.");
        document.RootElement.GetProperty("status").GetInt32().Should().Be(409);
    }

    [Fact]
    public async Task TryHandleAsync_WithOtherException_IsNotHandled()
    {
        var services = new ServiceCollection().AddOptions().AddLogging().AddProblemDetails().BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = services };
        var handler = new ConflictExceptionHandler(services.GetRequiredService<IProblemDetailsService>());

        var handled = await handler.TryHandleAsync(httpContext, new InvalidOperationException(), CancellationToken.None);

        handled.Should().BeFalse();
    }
}
