using Focus.Admin.Pages.AiCoach;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Coach.DTOs;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Focus.UnitTests.Common;
using Moq;
using Xunit;

namespace Focus.UnitTests.Admin;

public class PersonalMemoryPageTests
{
    private static PersonalModel CreatePage(Focus.Infrastructure.Persistence.ApplicationDbContext context)
    {
        var engine = new Mock<IFocusCoachEngine>();
        engine.Setup(e => e.EvaluateAnomalies(It.IsAny<User>(), It.IsAny<List<FocusSession>>(), It.IsAny<List<SessionCheckIn>>(), It.IsAny<List<SessionReflection>>(), It.IsAny<UserStreak?>()))
            .Returns(new CoachAnomalyStatusDto(false, AnomalyType.None, 0, 0, 0, "ok", "ok"));
        return new PersonalModel(context, engine.Object);
    }

    [Fact]
    public async Task SelectedUser_OutsideLatestFifty_IsStillLoaded()
    {
        using var context = TestDbContextFactory.Create();
        var users = Enumerable.Range(0, 120).Select(i => User.CreateGuest($"Kullanici{i}")).ToList();
        context.Users.AddRange(users);
        await context.SaveChangesAsync();
        var oldest = users.OrderBy(u => u.CreatedAt).First();

        var page = CreatePage(context);
        page.SelectedUserId = oldest.Id;
        await page.OnGetAsync();

        Assert.NotNull(page.CurrentUser);
        Assert.Equal(oldest.Id, page.CurrentUser!.Id);
        Assert.Contains(page.UserOptions, o => o.Id == oldest.Id);
    }

    [Fact]
    public async Task Counts_AreNotCappedByTheRecentWindow()
    {
        using var context = TestDbContextFactory.Create();
        var user = User.CreateGuest("Sayac");
        context.Users.Add(user);
        var sessionId = Guid.NewGuid();
        for (var i = 0; i < 45; i++)
        {
            context.SessionReflections.Add(new SessionReflection(sessionId, user.Id, 4, SessionMoodAfter.Refreshed, null));
        }

        await context.SaveChangesAsync();

        var page = CreatePage(context);
        page.SelectedUserId = user.Id;
        await page.OnGetAsync();

        Assert.Equal(45, page.TotalReflections);
        Assert.Equal(4.0, page.AverageFocusQuality);
    }

    [Fact]
    public void PeakHour_UsesIstanbulTimeAndWrapsPastMidnight()
    {
        var user = User.CreateGuest("Gece");
        // 20:30 UTC = 23:30 Istanbul, pencere 23:00 - 01:00 olmali
        var start = new DateTime(2026, 10, 6, 20, 30, 0, DateTimeKind.Utc);
        var session = FocusSession.Start(user.Id, null, SessionKind.Focus, 25, 5, now: start);
        session.Complete(1500, 10, 5, start.AddMinutes(25));

        var insights = PersonalModel.GeneratePersonalInsights(user, [session], [], [], null, []);

        var peak = Assert.Single(insights, i => i.Title == "Zirve Odaklanma Penceresi");
        Assert.Contains("23:00 - 01:00", peak.Description, StringComparison.Ordinal);
    }
}
