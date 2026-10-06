using Focus.Infrastructure.BackgroundJobs;
using Hangfire;
using Hangfire.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Focus.Admin.Pages.Jobs;

public class IndexModel : PageModel
{
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IRecurringJobManager recurringJobManager, ILogger<IndexModel> logger)
    {
        _recurringJobManager = recurringJobManager;
        _logger = logger;
    }

    public List<JobItemViewModel> Jobs { get; set; } = new();
    public int TotalRecurringJobs { get; set; }
    public int ActiveServersCount { get; set; }
    public string StorageInfo { get; set; } = "PostgreSQL";

    [TempData]
    public string? StatusMessage { get; set; }

    public class JobItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CronExpression { get; set; } = string.Empty;
        public string ScheduleDescription { get; set; } = string.Empty;
        public DateTime? LastExecution { get; set; }
        public DateTime? NextExecution { get; set; }
        public string LastJobState { get; set; } = "Zamanlandı";
        public string Queue { get; set; } = "default";
    }

    public void OnGet()
    {
        LoadJobs();
    }

    public IActionResult OnPostTrigger(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            StatusMessage = "Geçersiz görev kimliği.";
            return RedirectToPage();
        }

        try
        {
            RecurringJob.TriggerJob(jobId);
            StatusMessage = $"'{jobId}' görevi başarıyla manuel olarak tetiklendi ve Hangfire kuyruğuna alındı.";
            _logger.LogInformation("Job {JobId} manually triggered from admin dashboard", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering job {JobId}", jobId);
            StatusMessage = $"Görev tetiklenirken hata oluştu: {ex.Message}";
        }

        return RedirectToPage();
    }

    private void LoadJobs()
    {
        var registered = RecurringJobsConfigurator.RegisteredJobs;
        var list = new List<JobItemViewModel>();

        Dictionary<string, RecurringJobDto> hangfireJobs = new();
        try
        {
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            ActiveServersCount = monitoringApi.Servers().Count;

            using var connection = JobStorage.Current.GetConnection();
            var recurringList = connection.GetRecurringJobs();
            foreach (var rj in recurringList)
            {
                hangfireJobs[rj.Id] = rj;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch live Hangfire recurring job states.");
        }

        foreach (var def in registered)
        {
            var item = new JobItemViewModel
            {
                Id = def.Id,
                Title = def.Title,
                Description = def.Description,
                CronExpression = def.CronExpression,
                ScheduleDescription = def.ScheduleDescription,
                LastJobState = "Hazır / Zamanlandı"
            };

            if (hangfireJobs.TryGetValue(def.Id, out var liveJob))
            {
                item.LastExecution = liveJob.LastExecution;
                item.NextExecution = liveJob.NextExecution;
                item.Queue = liveJob.Queue ?? "default";
                if (!string.IsNullOrEmpty(liveJob.LastJobState))
                {
                    item.LastJobState = liveJob.LastJobState;
                }
            }

            list.Add(item);
        }

        Jobs = list;
        TotalRecurringJobs = list.Count;
    }
}
