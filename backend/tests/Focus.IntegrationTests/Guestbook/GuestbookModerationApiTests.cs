using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.Application.Features.Guestbook.DTOs;
using Focus.Application.Features.Social.DTOs;
using Focus.IntegrationTests.Common;
using Xunit;

namespace Focus.IntegrationTests.Guestbook;

public class GuestbookModerationApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public GuestbookModerationApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddEntry_WithOverlongMessage_ShouldReturnBadRequestWithGenericMessage()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("UzunYazar");

        var response = await client.PostAsJsonAsync(
            "/api/v1/study-rooms/lib-lofi/guestbook",
            new CreateGuestbookEntryRequest(new string('x', 301)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddEntry_ToPrivateRoomAsOutsider_ShouldReturnBadRequest()
    {
        var (ownerClient, _) = await _factory.CreateAuthenticatedClientAsync("DefterSahibi");
        var invite = (await (await ownerClient.PostAsync("/api/v1/study-rooms/create-invite", null))
            .Content.ReadFromJsonAsync<RoomInvitationResultDto>())!;
        var (outsider, _) = await _factory.CreateAuthenticatedClientAsync("DefterYabanci");

        var response = await outsider.PostAsJsonAsync(
            $"/api/v1/study-rooms/{invite.RoomCode}/guestbook",
            new CreateGuestbookEntryRequest("merhaba"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteEntry_ByOwner_ShouldRemoveEntryAndRejectOutsiders()
    {
        var (ownerClient, _) = await _factory.CreateAuthenticatedClientAsync("SilenSahip");
        var invite = (await (await ownerClient.PostAsync("/api/v1/study-rooms/create-invite", null))
            .Content.ReadFromJsonAsync<RoomInvitationResultDto>())!;
        var (guest, _) = await _factory.CreateAuthenticatedClientAsync("SilenMisafir");
        await guest.PostAsJsonAsync("/api/v1/study-rooms/join", new Focus.WebAPI.Controllers.JoinRoomRequest(invite.RoomCode, invite.InviteCode));
        var (outsider, _) = await _factory.CreateAuthenticatedClientAsync("SilenYabanci");

        var created = await guest.PostAsJsonAsync($"/api/v1/study-rooms/{invite.RoomCode}/guestbook", new CreateGuestbookEntryRequest("selam"));
        var entry = (await created.Content.ReadFromJsonAsync<GuestbookEntryDto>())!;

        var byOutsider = await outsider.DeleteAsync($"/api/v1/study-rooms/{invite.RoomCode}/guestbook/{entry.Id}");
        var byOwner = await ownerClient.DeleteAsync($"/api/v1/study-rooms/{invite.RoomCode}/guestbook/{entry.Id}");
        var again = await ownerClient.DeleteAsync($"/api/v1/study-rooms/{invite.RoomCode}/guestbook/{entry.Id}");

        byOutsider.StatusCode.Should().Be(HttpStatusCode.NotFound);
        byOwner.StatusCode.Should().Be(HttpStatusCode.NoContent);
        again.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
