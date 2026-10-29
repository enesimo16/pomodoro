using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Focus.Infrastructure.Common;

public static class ForwardedHeadersSetup
{
    public const string KnownProxiesKey = "ForwardedHeaders:KnownProxies";

    // Yalnizca loopback ve ForwardedHeaders:KnownProxies icindeki (virgullu) adreslerden gelen X-Forwarded-* basliklarina guvenilir
    public static IServiceCollection AddFocusForwardedHeaders(this IServiceCollection services)
    {
        services.AddOptions<ForwardedHeadersOptions>()
            .Configure<IConfiguration>((options, configuration) =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

                var proxies = (configuration[KnownProxiesKey] ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var proxy in proxies)
                {
                    options.KnownProxies.Add(IPAddress.Parse(proxy));
                }
            });

        return services;
    }
}
