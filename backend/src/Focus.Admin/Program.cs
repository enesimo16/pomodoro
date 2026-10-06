using Focus.Application;
using Focus.Infrastructure;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseMiddleware<Focus.Infrastructure.Middleware.UserAccessTrackingMiddleware>();

// Hangfire Kontrol Paneli ve Otomatik Cron Yapılandırması
app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
{
    DashboardTitle = "Focus Hangfire Kontrol Paneli",
    Authorization = new[] { new Focus.Infrastructure.BackgroundJobs.HangfireAuthorizationFilter() }
});

using (var scope = app.Services.CreateScope())
{
    try
    {
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<Hangfire.IRecurringJobManager>();
        Focus.Infrastructure.BackgroundJobs.RecurringJobsConfigurator.ConfigureRecurringJobs(recurringJobManager);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Hangfire zamanlanmış görevleri başlatılırken geçici bir hata oluştu.");
    }
}

app.MapRazorPages();

app.Run();
