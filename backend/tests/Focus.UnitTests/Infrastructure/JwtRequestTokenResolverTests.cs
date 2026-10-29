using Focus.Infrastructure.Authentication;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Focus.UnitTests.Infrastructure;

public class JwtRequestTokenResolverTests
{
    [Fact]
    public void Resolve_WithAuthorizationHeaderAndCookie_ShouldDeferToHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer header-token";
        context.Request.Headers.Append("Cookie", "focus_at=cookie-token");

        Assert.Null(JwtRequestTokenResolver.Resolve(context.Request));
    }

    [Fact]
    public void Resolve_WithCookieOnly_ShouldReturnCookieToken()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append("Cookie", "focus_at=cookie-token");

        Assert.Equal("cookie-token", JwtRequestTokenResolver.Resolve(context.Request));
    }

    [Fact]
    public void Resolve_WithNothing_ShouldReturnNull()
    {
        Assert.Null(JwtRequestTokenResolver.Resolve(new DefaultHttpContext().Request));
    }

    [Fact]
    public void Resolve_OnHubPathWithQueryToken_ShouldPreferQueryOverCookie()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/hubs/room";
        context.Request.QueryString = new QueryString("?access_token=query-token");
        context.Request.Headers.Append("Cookie", "focus_at=cookie-token");

        Assert.Equal("query-token", JwtRequestTokenResolver.Resolve(context.Request));
    }

    [Fact]
    public void Resolve_OnNonHubPathWithQueryToken_ShouldIgnoreQuery()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/auth/me";
        context.Request.QueryString = new QueryString("?access_token=query-token");

        Assert.Null(JwtRequestTokenResolver.Resolve(context.Request));
    }
}
