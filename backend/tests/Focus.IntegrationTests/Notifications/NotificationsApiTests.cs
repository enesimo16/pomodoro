using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Notifications.DTOs;
using Focus.Domain.Enums;
using Focus.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Focus.IntegrationTests.Notifications;

public class NotificationsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public NotificationsApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetNotifications_ReturnsListSuccessfully()
    {
        var (client, auth) = await _factory.CreateAuthenticatedClientAsync("NotifUser1");

        // Seed notification
        using (var scope = _factory.Services.CreateScope())
        {
            var notifService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            await notifService.CreateNotificationAsync(
                auth.User.Id,
                NotificationType.StreakAtRisk,
                "Serin Risk Altında!",
                "Bugün seans yapmayı unutma.");
        }

        var response = await client.GetAsync("/api/v1/notifications");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notifs = await response.Content.ReadFromJsonAsync<List<NotificationDto>>();
        notifs.Should().NotBeNull();
        notifs!.Should().Contain(n => n.Title == "Serin Risk Altında!");
    }

    [Fact]
    public async Task MarkAsRead_MarksNotificationRead()
    {
        var (client, auth) = await _factory.CreateAuthenticatedClientAsync("NotifUser2");

        Guid notifId;
        using (var scope = _factory.Services.CreateScope())
        {
            var notifService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var created = await notifService.CreateNotificationAsync(
                auth.User.Id,
                NotificationType.CoachAdvice,
                "Mola Vakti",
                "Biraz dinlen.");
            notifId = created.Id;
        }

        var patchRes = await client.PatchAsync($"/api/v1/notifications/{notifId}/read", null);
        patchRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var listRes = await client.GetAsync("/api/v1/notifications");
        var list = await listRes.Content.ReadFromJsonAsync<List<NotificationDto>>();
        list.Should().Contain(n => n.Id == notifId && n.IsRead);
    }

    [Fact]
    public async Task MarkAllAsRead_MarksAllUnreadNotifications()
    {
        var (client, auth) = await _factory.CreateAuthenticatedClientAsync("NotifUser3");

        using (var scope = _factory.Services.CreateScope())
        {
            var notifService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            await notifService.CreateNotificationAsync(auth.User.Id, NotificationType.System, "Duyuru 1", "Sistem bakımı");
            await notifService.CreateNotificationAsync(auth.User.Id, NotificationType.System, "Duyuru 2", "Yeni tema eklendi");
        }

        var postRes = await client.PostAsync("/api/v1/notifications/read-all", null);
        postRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var listRes = await client.GetAsync("/api/v1/notifications");
        var list = await listRes.Content.ReadFromJsonAsync<List<NotificationDto>>();
        list.Should().OnlyContain(n => n.IsRead);
    }
}
