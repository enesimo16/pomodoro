using System.Text;
using Focus.Application.Common.Interfaces;
using Focus.Application.Common.Options;
using Focus.Infrastructure.Authentication;
using Focus.Infrastructure.BackgroundJobs;
using Focus.Infrastructure.Common;
using Focus.Infrastructure.Persistence;
using Focus.Infrastructure.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Focus.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // PostgreSQL DbContext
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            var connectionString = GetPostgresConnectionString(serviceProvider.GetRequiredService<IConfiguration>());

            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.UseVector();
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            });
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Ayarlar
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .Validate(
                JwtSettings.HasValidSigningKey,
                $"Auth:Jwt:SigningKey tanimli degil veya {JwtSettings.MinSigningKeyBytes} bayttan kisa. Auth__Jwt__SigningKey ortam degiskeni ile en az {JwtSettings.MinSigningKeyBytes} baytlik gizli bir anahtar verin.")
            .ValidateOnStart();
        services.Configure<AlgorithmOptions>(configuration.GetSection(AlgorithmOptions.SectionName));
        services.Configure<GoogleAuthSettings>(configuration.GetSection(GoogleAuthSettings.SectionName));
        services.Configure<ExternalApiSettings>(configuration.GetSection(ExternalApiSettings.SectionName));
        services.Configure<AiSettings>(configuration.GetSection(AiSettings.SectionName));

        // Servisler
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Harici Medya & API Servisleri (Typed HttpClients)
        services.AddHttpClient<IThemeVideoService, PixabayVideoService>(ConfigureExternalClient);
        services.AddHttpClient<IMusicTrackService, JamendoMusicService>(ConfigureExternalClient);
        services.AddHttpClient<IWeatherService, OpenMeteoWeatherService>(ConfigureExternalClient);

        // Faz 5: Yapay Zeka Koc, Cift Katmanli Hafiza & Atmosfer Uretim Servisleri
        services.AddSingleton<IAnonymizationService, AnonymizationService>();
        services.AddScoped<IFocusCoachEngine, FocusCoachEngine>();
        services.AddScoped<IAiUsageService, AiUsageService>();
        services.AddHttpClient<IVectorMemoryService, VectorMemoryService>(ConfigureGeminiClient);
        services.AddHttpClient<ICoachChatService, CoachChatService>(ConfigureGeminiClient);
        services.AddHttpClient<IAtmosphereAiService, AtmosphereAiService>(ConfigureGeminiClient);

        // Faz 6: Focus Wrapped, Ziyaretci Defteri & Bildirim Servisleri
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IFocusWrappedService, FocusWrappedService>();
        services.AddScoped<IGuestbookService, GuestbookService>();

        // Faz 7: Admin Is Zekasi & Sistem Sagligi Servisi
        services.AddScoped<IAdminAnalyticsService, AdminAnalyticsService>();
        services.AddScoped<IUserAccessTrackingService, UserAccessTrackingService>();

        // Hangfire Arka Plan Gorevleri ve Zamanlayici
        services.AddHangfire((serviceProvider, config) => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                c => c.UseNpgsqlConnection(GetPostgresConnectionString(serviceProvider.GetRequiredService<IConfiguration>())),
                new PostgreSqlStorageOptions
                {
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    InvisibilityTimeout = TimeSpan.FromMinutes(30)
                }));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 2;
            options.ServerName = $"focus-worker-{Environment.ProcessId}";
        });

        services.AddScoped<IBackgroundJobService, BackgroundJobService>();

        // JWT Kimlik Dogrulama
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, jwtOptions) =>
            {
                var jwtSettings = jwtOptions.Value;

                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var token = JwtRequestTokenResolver.Resolve(context.Request);
                        if (token != null)
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }

    private static void ConfigureExternalClient(IServiceProvider serviceProvider, HttpClient client)
    {
        var settings = serviceProvider.GetRequiredService<IOptions<ExternalApiSettings>>().Value;
        client.Timeout = TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds));
    }

    private static void ConfigureGeminiClient(IServiceProvider serviceProvider, HttpClient client)
    {
        var settings = serviceProvider.GetRequiredService<IOptions<AiSettings>>().Value.Gemini;
        client.BaseAddress = new Uri(GeminiSettings.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds));
        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            client.DefaultRequestHeaders.Add("x-goog-api-key", settings.ApiKey);
        }
    }

    private static string GetPostgresConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Postgres tanimli degil. ConnectionStrings__Postgres ortam degiskenini veya dotnet user-secrets degerini ayarlayin.");
        }

        return connectionString;
    }
}
