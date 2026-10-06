using Focus.Application;
using Focus.Infrastructure;
using Focus.WebAPI.Hubs;
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

// CORS Yapilandirmasi
var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? new[] { "http://localhost:3000", "http://localhost:5001", "http://127.0.0.1:3000", "http://127.0.0.1:5001" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("FocusClientCors", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin)) return false;
                if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    return uri.Host == "localhost" || uri.Host == "127.0.0.1" || allowedOrigins.Contains(origin);
                }
                return false;
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Gelistirme ortami Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Focus API v1");
    });
}

app.UseCors("FocusClientCors");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<Focus.Infrastructure.Middleware.UserAccessTrackingMiddleware>();

app.MapControllers();
app.MapHub<TimerHub>("/hubs/timer");
app.MapHub<RoomHub>("/hubs/room");

using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<Focus.Infrastructure.Persistence.ApplicationDbContext>();
        await Focus.Infrastructure.Persistence.GlobalMemorySeeder.SeedGlobalMemoriesAsync(context);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Seeder Error]: {ex.Message} -> {ex.InnerException?.Message}");
    }
}

app.Run();

public partial class Program { }
