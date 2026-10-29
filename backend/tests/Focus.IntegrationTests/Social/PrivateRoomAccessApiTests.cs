using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.Application.Features.Social.DTOs;
using Focus.IntegrationTests.Common;
using Focus.WebAPI.Controllers;
using Xunit;

namespace Focus.IntegrationTests.Social;

public class PrivateRoomAccessApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PrivateRoomAccessApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<RoomInvitationResultDto> CreateInviteAsync(HttpClient ownerClient)
    {
        var response = await ownerClient.PostAsync("/api/v1/study-rooms/create-invite", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<RoomInvitationResultDto>())!;
    }

    [Fact]
    public async Task Join_PrivateRoomWithoutInvite_ShouldReturnBadRequest()
    {
        var (ownerClient, _) = await _factory.CreateAuthenticatedClientAsync("GizliSahip");
        var invite = await CreateInviteAsync(ownerClient);
        var (strangerClient, _) = await _factory.CreateAuthenticatedClientAsync("GizliYabanci");

        var response = await strangerClient.PostAsJsonAsync("/api/v1/study-rooms/join", new JoinRoomRequest(invite.RoomCode));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetRoomByCode_PrivateRoom_ShouldBeHiddenFromNonMembers()
    {
        var (ownerClient, _) = await _factory.CreateAuthenticatedClientAsync("GorunurSahip");
        var invite = await CreateInviteAsync(ownerClient);
        var (strangerClient, _) = await _factory.CreateAuthenticatedClientAsync("GorunurYabanci");

        var asOwner = await ownerClient.GetAsync($"/api/v1/study-rooms/{invite.RoomCode}");
        var asStranger = await strangerClient.GetAsync($"/api/v1/study-rooms/{invite.RoomCode}");
        var guestbookAsStranger = await strangerClient.GetAsync($"/api/v1/study-rooms/{invite.RoomCode}/guestbook");

        asOwner.StatusCode.Should().Be(HttpStatusCode.OK);
        asStranger.StatusCode.Should().Be(HttpStatusCode.NotFound);
        guestbookAsStranger.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRoomByCode_Anonymous_ShouldReturnUnauthorized()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync("/api/v1/study-rooms/LIB-LOFI");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Leave_WhenNotMember_ShouldReturnNotFound()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("AyrilanUyeDegil");

        var response = await client.PostAsJsonAsync("/api/v1/study-rooms/leave", new JoinRoomRequest("LIB-LOFI"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Join_PrivateRoomWithInvite_ShouldMakeRoomVisible()
    {
        var (ownerClient, _) = await _factory.CreateAuthenticatedClientAsync("DavetSahip");
        var invite = await CreateInviteAsync(ownerClient);
        var (guestClient, _) = await _factory.CreateAuthenticatedClientAsync("DavetliMisafir");

        var join = await guestClient.PostAsJsonAsync("/api/v1/study-rooms/join", new JoinRoomRequest(invite.RoomCode, invite.InviteCode));
        var room = await guestClient.GetAsync($"/api/v1/study-rooms/{invite.RoomCode}");

        join.StatusCode.Should().Be(HttpStatusCode.OK);
        room.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
