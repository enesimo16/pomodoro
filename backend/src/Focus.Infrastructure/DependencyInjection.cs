using System.Text;
using Focus.Application.Common.Interfaces;
using Focus.Infrastructure.Authentication;
using Focus.Infrastructure.Common;
using Focus.Infrastructure.Persistence;
using Focus.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Focus.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // PostgreSQL DbContext
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? "Host=localhost;Port=5432;Database=focus_db;Username=focus_user;Password=focus_dev_pg_7Kq2mX9vLp";

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.UseVector();
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            });
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Ayarlar
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<GoogleAuthSettings>(configuration.GetSection(GoogleAuthSettings.SectionName));
        services.Configure<ExternalApiSettings>(configuration.GetSection(ExternalApiSettings.SectionName));

        var jwtSettings = new JwtSettings();
        configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);

        // Servisler
        services.AddHttpContextAccessor();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Harici Medya & API Servisleri (Typed HttpClients)
        services.AddHttpClient<IThemeVideoService, PixabayVideoService>();
        services.AddHttpClient<IMusicTrackService, JamendoMusicService>();
        services.AddHttpClient<IWeatherService, OpenMeteoWeatherService>();

        // Faz 5: Yapay Zeka Koc & Cift Katmanli Hafiza Servisleri
        services.AddSingleton<IAnonymizationService, AnonymizationService>();
        services.AddScoped<IFocusCoachEngine, FocusCoachEngine>();
        services.AddHttpClient<IVectorMemoryService, VectorMemoryService>();
        services.AddHttpClient<ICoachChatService, CoachChatService>();

        // Faz 6: Focus Wrapped, Ziyaretci Defteri & Bildirim Servisleri
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IFocusWrappedService, FocusWrappedService>();
        services.AddScoped<IGuestbookService, GuestbookService>();

        // Faz 7: Admin Is Zekasi & Sistem Sagligi Servisi
        services.AddScoped<IAdminAnalyticsService, AdminAnalyticsService>();

        // JWT Kimlik Dogrulama
        var key = Encoding.UTF8.GetBytes(jwtSettings.SigningKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
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
                    // 1. Authorization header yoksa HttpOnly cookie oku (focus_at)
                    if (string.IsNullOrEmpty(context.Token))
                    {
                        var cookieToken = context.Request.Cookies["focus_at"];
                        if (!string.IsNullOrEmpty(cookieToken))
                        {
                            context.Token = cookieToken;
                        }
                    }

                    // 2. SignalR Baglantisi icin query string oku
                    var path = context.HttpContext.Request.Path;
                    if (path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase))
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }
                    }

                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();

        return services;
    }
}
