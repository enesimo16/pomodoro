using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.Application.Features.Social.DTOs;
using Focus.IntegrationTests.Common;
using Focus.WebAPI.Controllers;
using Xunit;

namespace Focus.IntegrationTests.Social;

public class RoomModerationApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RoomModerationApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Kick_ByOwner_ShouldRemoveGuestAndRejectNonOwner()
    {
        var (ownerClient, _) = await _factory.CreateAuthenticatedClientAsync("AtanSahip");
        var invite = (await (await ownerClient.PostAsync("/api/v1/study-rooms/create-invite", null))
            .Content.ReadFromJsonAsync<RoomInvitationResultDto>())!;
        var (guestClient, guestAuth) = await _factory.CreateAuthenticatedClientAsync("AtilanMisafir");
        await guestClient.PostAsJsonAsync("/api/v1/study-rooms/join", new JoinRoomRequest(invite.RoomCode, invite.InviteCode));

        var byGuest = await guestClient.DeleteAsync($"/api/v1/study-rooms/{invite.RoomCode}/members/{guestAuth.User.Id}");
        var byOwner = await ownerClient.DeleteAsync($"/api/v1/study-rooms/{invite.RoomCode}/members/{guestAuth.User.Id}");
        var roomAfterKick = await guestClient.GetAsync($"/api/v1/study-rooms/{invite.RoomCode}");

        byGuest.StatusCode.Should().Be(HttpStatusCode.NotFound);
        byOwner.StatusCode.Should().Be(HttpStatusCode.NoContent);
        roomAfterKick.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reaction_WithOverlongText_ShouldReturnBadRequest()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("UzunTepki");
        await client.PostAsJsonAsync("/api/v1/study-rooms/join", new JoinRoomRequest("LIB-LOFI"));

        var response = await client.PostAsJsonAsync(
            "/api/v1/study-rooms/reaction",
            new RoomReactionRequest("LIB-LOFI", new string('x', 40)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Reaction_AsMember_ShouldSucceed()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("TepkiUyesi");
        await client.PostAsJsonAsync("/api/v1/study-rooms/join", new JoinRoomRequest("LIB-LOFI"));

        var response = await client.PostAsJsonAsync("/api/v1/study-rooms/reaction", new RoomReactionRequest("LIB-LOFI", "Kahve"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
