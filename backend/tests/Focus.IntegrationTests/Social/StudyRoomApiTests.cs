using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.Application.Features.Social.DTOs;
using Focus.IntegrationTests.Common;
using Focus.WebAPI.Controllers;
using Xunit;

namespace Focus.IntegrationTests.Social;

public class StudyRoomApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public StudyRoomApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPublicRooms_ShouldReturnSeededLibrariesAndMarket()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("KutuphaneMeraklisi");

        var response = await client.GetAsync("/api/v1/study-rooms/public");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rooms = await response.Content.ReadFromJsonAsync<List<StudyRoomDto>>();
        rooms.Should().NotBeNull();
        rooms!.Count.Should().BeGreaterThanOrEqualTo(4);

        rooms.Should().Contain(r => r.Code == "LIB-LOFI");
        rooms.Should().Contain(r => r.Code == "LIB-WOOD");
        rooms.Should().Contain(r => r.Code == "LIB-NIGHT");
        rooms.Should().Contain(r => r.Code == "SHOP-BAZAAR");
    }

    [Fact]
    public async Task CreateInvite_ShouldReturnValidShareLinks()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync("OdaSahibiLink");

        var response = await client.PostAsync("/api/v1/study-rooms/create-invite", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var invite = await response.Content.ReadFromJsonAsync<RoomInvitationResultDto>();
        invite.Should().NotBeNull();
        invite!.RoomCode.Should().StartWith("FOC-");
        invite.WhatsAppLink.Should().Contain("api.whatsapp.com");
        invite.DirectLink.Should().Contain("/join/");
    }

    [Fact]
    public async Task JoinRoom_WhenFreeCapacityExceeded_ShouldReturnBadRequest()
    {
        // 1. Oda sahibi (Free kullanıcı)
        var (ownerClient, _) = await _factory.CreateAuthenticatedClientAsync("EvSahibi");
        var inviteRes = await ownerClient.PostAsync("/api/v1/study-rooms/create-invite", null);
        var invite = await inviteRes.Content.ReadFromJsonAsync<RoomInvitationResultDto>();

        // 2. Birinci misafir odaya katılır (Sahip + 1 = 2 kişi, serbest)
        var (guest1Client, _) = await _factory.CreateAuthenticatedClientAsync("Misafir1");
        var join1Res = await guest1Client.PostAsJsonAsync("/api/v1/study-rooms/join", new JoinRoomRequest(invite!.RoomCode, invite.InviteCode));
        join1Res.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. İkinci misafir katılmaya çalışır (Toplam 3 kişi olur, Free limitten dolayı engellenmeli)
        var (guest2Client, _) = await _factory.CreateAuthenticatedClientAsync("Misafir2");
        var join2Res = await guest2Client.PostAsJsonAsync("/api/v1/study-rooms/join", new JoinRoomRequest(invite.RoomCode, invite.InviteCode));
        join2Res.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var failResult = await join2Res.Content.ReadFromJsonAsync<JoinRoomResultDto>();
        failResult.Should().NotBeNull();
        failResult!.Success.Should().BeFalse();
        failResult.Message.Should().Contain("Premium");
    }

    [Fact]
    public async Task SitAtSeat_WhenSeatOccupied_ShouldReturnBadRequest()
    {
        var (client1, _) = await _factory.CreateAuthenticatedClientAsync("Oturucu1");
        var (client2, _) = await _factory.CreateAuthenticatedClientAsync("Oturucu2");

        // Her iki kullanıcı da genel kütüphaneye katılır (LIB-LOFI)
        await client1.PostAsJsonAsync("/api/v1/study-rooms/join", new JoinRoomRequest("LIB-LOFI"));
        await client2.PostAsJsonAsync("/api/v1/study-rooms/join", new JoinRoomRequest("LIB-LOFI"));

        // User1 masa 1'e oturur
        var sit1 = await client1.PostAsJsonAsync("/api/v1/study-rooms/sit", new SitAtSeatRequest("LIB-LOFI", 1));
        sit1.StatusCode.Should().Be(HttpStatusCode.OK);

        // User2 aynı masaya oturmaya çalışır
        var sit2 = await client2.PostAsJsonAsync("/api/v1/study-rooms/sit", new SitAtSeatRequest("LIB-LOFI", 1));
        sit2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var err = await sit2.Content.ReadFromJsonAsync<SitSeatResultDto>();
        err!.Success.Should().BeFalse();
        err.Message.Should().Contain("dolu");
    }
}
