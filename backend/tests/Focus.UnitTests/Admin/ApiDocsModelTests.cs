using Focus.Admin.Pages.ApiDocs;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Focus.UnitTests.Admin;

public class ApiDocsModelTests
{
    private static IndexModel CreateLoadedModel()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var model = new IndexModel(configuration);
        model.OnGet();
        return model;
    }

    [Fact]
    public void Endpoints_DoNotDocumentASuccessDataEnvelope()
    {
        var model = CreateLoadedModel();

        var responses = model.Categories.SelectMany(c => c.Endpoints).Select(e => e.JsonSuccessResponse).Where(r => r is not null);

        Assert.All(responses, r => Assert.DoesNotMatch(@"""success""\s*:\s*true,\s*""data""", r));
    }

    [Fact]
    public void AuthEndpoints_DocumentTheFlatAuthResponse()
    {
        var model = CreateLoadedModel();

        var guest = model.Categories.SelectMany(c => c.Endpoints).Single(e => e.Path == "/api/v1/auth/guest");

        Assert.Contains("\"accessToken\"", guest.JsonSuccessResponse);
        Assert.Contains("\"refreshToken\"", guest.JsonSuccessResponse);
        Assert.Matches(@"^\{\s*""user""", guest.JsonSuccessResponse);
    }

    [Fact]
    public void TestLabLinks_PointToExistingLabTabs()
    {
        var model = CreateLoadedModel();

        var tabs = model.Categories.SelectMany(c => c.Endpoints).Select(e => e.TestLabTab);

        Assert.All(tabs, t => Assert.StartsWith("#tab-", t));
    }

    [Fact]
    public void Hubs_DoNotListRemovedSubscribeToUser()
    {
        var model = CreateLoadedModel();

        var methods = model.Hubs.SelectMany(h => h.ServerMethods).Select(m => m.MethodName);

        Assert.DoesNotContain("SubscribeToUser", methods);
    }
}
