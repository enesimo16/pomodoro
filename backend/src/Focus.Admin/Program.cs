using Focus.Application;
using Focus.Infrastructure;
using Focus.Infrastructure.Common;
using Focus.Infrastructure.Middleware;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFocusForwardedHeaders();
builder.Services.AddRazorPages();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    var adminSecurityEnabled = app.Configuration.GetValue("AdminSecurity:Enabled", true);
    if (!adminSecurityEnabled || AdminKeyValidator.GetAllowedKeys(app.Configuration).Length == 0)
    {
        throw new InvalidOperationException(
            "Admin paneli Development disinda AdminSecurity:Enabled=true ve en az bir AdminSecurity:Keys degeri olmadan baslatilamaz. AdminSecurity__Keys__0 ortam degiskenini ayarlayin.");
    }

    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseMiddleware<AdminStealthModeMiddleware>();
app.UseRouting();
app.UseAuthorization();

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
