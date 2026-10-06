using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.Application.Features.Wrapped.DTOs;
using Focus.IntegrationTests.Common;
using Xunit;

namespace Focus.IntegrationTests.Wrapped;

public class WrappedApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public WrappedApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetWeeklyWrapped_ReturnsOkAndValidWrappedDto()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("WrappedUser1");

        var response = await client.GetAsync("/api/v1/wrapped/weekly");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var wrapped = await response.Content.ReadFromJsonAsync<FocusWrappedDto>();
        wrapped.Should().NotBeNull();
        wrapped!.Period.Should().Be("Weekly");
        wrapped.ChronotypeName.Should().NotBeNullOrWhiteSpace();
        wrapped.TopDayOfWeek.Should().NotBeNullOrWhiteSpace();
        wrapped.SummaryInsight.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetMonthlyWrapped_ReturnsOkAndValidWrappedDto()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("WrappedUser2");

        var response = await client.GetAsync("/api/v1/wrapped/monthly");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var wrapped = await response.Content.ReadFromJsonAsync<FocusWrappedDto>();
        wrapped.Should().NotBeNull();
        wrapped!.Period.Should().Be("Monthly");
    }

    [Fact]
    public async Task GetWeeklyWrapped_Unauthorized_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/wrapped/weekly");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
