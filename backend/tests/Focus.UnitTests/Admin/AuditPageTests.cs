using Focus.Admin.Pages.Audit;
using Focus.Domain.Entities;
using Focus.UnitTests.Common;
using Xunit;

namespace Focus.UnitTests.Admin;

public class AuditPageTests
{
    [Fact]
    public async Task MobileCount_IncludesTabletsToMatchTheTabAndFilter()
    {
        using var context = TestDbContextFactory.Create();
        context.UserDeviceSessions.AddRange(
            UserDeviceSession.Create(null, "10.0.0.1", "ua", "Mobil", "Android", "Chrome"),
            UserDeviceSession.Create(null, "10.0.0.2", "ua", "Tablet", "iPadOS", "Safari"),
            UserDeviceSession.Create(null, "10.0.0.3", "ua", "Masaüstü", "Windows", "Edge"));
        await context.SaveChangesAsync();

        var page = new IndexModel(context) { Filter = "mobile" };
        await page.OnGetAsync();

        Assert.Equal(2, page.MobileCount);
        Assert.Equal(1, page.DesktopCount);
        Assert.Equal(2, page.Sessions.Count);
    }
}
