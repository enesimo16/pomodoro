using FluentAssertions;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Xunit;

namespace Focus.UnitTests.Notifications;

public class NotificationEntityTests
{
    [Fact]
    public void Create_InitializesNotificationAsUnread()
    {
        var userId = Guid.NewGuid();
        var notification = Notification.Create(
            userId,
            NotificationType.StreakAtRisk,
            "Serin Tehlikede!",
            "Gun bitmeden bir seans tamamla.",
            "/timer");

        notification.Id.Should().NotBeEmpty();
        notification.UserId.Should().Be(userId);
        notification.Type.Should().Be(NotificationType.StreakAtRisk);
        notification.Title.Should().Be("Serin Tehlikede!");
        notification.Message.Should().Be("Gun bitmeden bir seans tamamla.");
        notification.ActionUrl.Should().Be("/timer");
        notification.IsRead.Should().BeFalse();
        notification.ReadAt.Should().BeNull();
        notification.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsRead_SetsIsReadTrueAndPopulatesReadAt()
    {
        var notification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.CoachAdvice,
            "Dinlenme Tavsiyesi",
            "Mola zamani.");

        notification.MarkAsRead();

        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().NotBeNull();
        notification.ReadAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
