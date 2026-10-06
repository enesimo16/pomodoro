using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.Application.Features.Guestbook.DTOs;
using Focus.Domain.Enums;
using Focus.IntegrationTests.Common;
using Xunit;

namespace Focus.IntegrationTests.Guestbook;

public class GuestbookApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public GuestbookApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddAndGetGuestbookEntries_WorksCorrectly()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("ZiyaretciUser");

        var addRequest = new CreateGuestbookEntryRequest(
            Message: "Masan harika duruyor, basarilar!",
            GiftType: GiftType.Coffee);

        var postRes = await client.PostAsJsonAsync("/api/v1/study-rooms/LIB-LOFI/guestbook", addRequest);
        postRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var entry = await postRes.Content.ReadFromJsonAsync<GuestbookEntryDto>();
        entry.Should().NotBeNull();
        entry!.Message.Should().Be("Masan harika duruyor, basarilar!");
        entry.GiftType.Should().Be(GiftType.Coffee);
        entry.StudyRoomCode.Should().Be("LIB-LOFI");

        // List
        var listRes = await client.GetAsync("/api/v1/study-rooms/LIB-LOFI/guestbook");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var entries = await listRes.Content.ReadFromJsonAsync<List<GuestbookEntryDto>>();
        entries.Should().NotBeNull();
        entries!.Should().Contain(e => e.Id == entry.Id);
    }
}
