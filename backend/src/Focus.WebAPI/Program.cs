using Focus.Application;
using Focus.Application.Common.Interfaces;
using Focus.Infrastructure;
using Focus.Infrastructure.Common;
using Focus.Infrastructure.Persistence;
using Focus.WebAPI.ExceptionHandling;
using Focus.WebAPI.Hubs;
using Focus.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// .NET Servisleri
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();

// Swagger & OpenAPI
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Focus API",
        Version = "v1",
        Description = "Habbo tarzı piksel ev, avatar ve odaklanma platformu REST API uçları."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization başlığı: 'Bearer {token}' formatında giriniz.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Katman Bagimliliklari
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFocusForwardedHeaders();
builder.Services.AddFocusRateLimiting(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<ConflictExceptionHandler>();

// CORS Yapilandirmasi
builder.Services.AddCors();
builder.Services.AddOptions<CorsOptions>()
    .Configure<IConfiguration, IWebHostEnvironment>((options, configuration, environment) =>
    {
        var allowedOrigins = CorsOriginPolicy.ParseAllowedOrigins(configuration["Cors:AllowedOrigins"]);
        var isDevelopment = environment.IsDevelopment();

        options.AddPolicy("FocusClientCors", policy =>
        {
            policy.SetIsOriginAllowed(origin => CorsOriginPolicy.IsOriginAllowed(origin, allowedOrigins, isDevelopment))
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // Gelistirme ortami Swagger
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Focus API v1");
    });
}
else
{
    app.UseHsts();
}

app.UseCors("FocusClientCors");

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.UseMiddleware<Focus.Infrastructure.Middleware.UserAccessTrackingMiddleware>();

app.MapControllers();
app.MapHub<TimerHub>("/hubs/timer");
app.MapHub<RoomHub>("/hubs/room");

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (context.Database.IsNpgsql())
        {
            await context.Database.MigrateAsync();
        }

        await GlobalMemorySeeder.SeedGlobalMemoriesAsync(
            context,
            scope.ServiceProvider.GetRequiredService<IVectorMemoryService>());
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Veritabani migrasyonu veya baslangic verisi yuklenemedi.");
        throw;
    }
}

app.Run();

public partial class Program { }
