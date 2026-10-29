using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Focus.Application.Common.Interfaces;
using Focus.Application.Features.Auth.DTOs;
using Focus.Infrastructure.Persistence;
using Hangfire;
using Hangfire.InMemory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Focus.IntegrationTests.Common;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestSigningKey = "integration-tests-signing-key-0123456789abcdef";

    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected virtual string EnvironmentName => "Development";

    // Testler appsettings.json icindeki degerlere bagimli olmasin diye gerekli ayarlar bellekten enjekte edilir
    protected virtual IReadOnlyDictionary<string, string?> TestConfiguration => new Dictionary<string, string?>
    {
        ["ConnectionStrings:Postgres"] = "Host=localhost;Database=focus_test;Username=test;Password=test",
        ["Auth:Jwt:SigningKey"] = TestSigningKey,
        ["Auth:Jwt:Issuer"] = "focus-api",
        ["Auth:Jwt:Audience"] = "focus-web",
        ["Cors:AllowedOrigins"] = "http://localhost:3000",
        ["RateLimiting:Auth:PermitLimit"] = "100000",
        ["RateLimiting:Ai:PermitLimit"] = "100000",
        ["RateLimiting:Media:PermitLimit"] = "100000"
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(TestConfiguration));

        builder.ConfigureServices(services =>
        {
            // Eski DbContext tanimlarini temizle
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(ApplicationDbContext) ||
                d.ServiceType == typeof(IApplicationDbContext)).ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            // Npgsql ile InMemory saglayici cakismasini onlemek icin izole InternalServiceProvider
            var internalServiceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName)
                       .UseInternalServiceProvider(internalServiceProvider);
            });

            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

            // Hangfire Postgres deposu yerine bellek ici depo kullanilir; testler veritabani gerektirmez
            services.AddHangfire(config => config.UseInMemoryStorage());

            services.AddTransient<IStartupFilter, RemoteIpStartupFilter>();
        });

        builder.UseEnvironment(EnvironmentName);
    }

    public async Task<(HttpClient Client, AuthResponseDto AuthData)> CreateAuthenticatedClientAsync(string? displayName = null)
    {
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        }

        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/guest", new { displayName });
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Guest creation failed: {response.StatusCode} - {err}");
        }

        var authData = (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authData.AccessToken);

        return (client, authData);
    }

    // TestServer uzak adres vermez; gercek bir istemci adresi simule edilir ki ForwardedHeaders guvensiz kaynaktan gelen basliklari yok saysin
    private sealed class RemoteIpStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.Use((context, nextMiddleware) =>
                {
                    context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.9");
                    return nextMiddleware();
                });
                next(app);
            };
        }
    }
}
