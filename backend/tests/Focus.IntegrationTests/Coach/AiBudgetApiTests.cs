using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.Application.Common.Exceptions;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Coach.DTOs;
using Focus.IntegrationTests.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Focus.IntegrationTests.Coach;

public class AiBudgetApiTests : IClassFixture<AiBudgetApiTests.BudgetExceededFactory>
{
    public sealed class BudgetExceededFactory : CustomWebApplicationFactory
    {
        private sealed class ExhaustedCoachChatService : ICoachChatService
        {
            public Task<CoachChatResponseDto> GenerateCoachResponseAsync(Guid userId, string userMessage, CancellationToken cancellationToken = default)
                => throw new AiBudgetExceededException();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICoachChatService>();
                services.AddScoped<ICoachChatService, ExhaustedCoachChatService>();
            });
        }
    }

    private readonly BudgetExceededFactory _factory;

    public AiBudgetApiTests(BudgetExceededFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Chat_WhenDailyBudgetExhausted_ReturnsTooManyRequests()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("ButceKullanicisi");

        var response = await client.PostAsJsonAsync("/api/v1/coach/chat", new CoachChatRequest("Merhaba"));

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Chat_WithOverlongMessage_ReturnsBadRequest()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("UzunMesajKullanicisi");

        var response = await client.PostAsJsonAsync("/api/v1/coach/chat", new CoachChatRequest(new string('a', 1001)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
