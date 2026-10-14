using Focus.Application.Common.Interfaces;
using Focus.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Focus.Admin.Pages.AiCoach;

public class AtmosphereModel : PageModel
{
    private readonly IApplicationDbContext _context;
    private readonly IWeatherService _weatherService;

    public AtmosphereModel(IApplicationDbContext context, IWeatherService weatherService)
    {
        _context = context;
        _weatherService = weatherService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    // KPI Metrikleri
    public int TotalSavedAtmospheres { get; set; }
    public int AiGeneratedCount { get; set; }
    public string CurrentWeatherSummary { get; set; } = "Yükleniyor...";
    public double CurrentTemperature { get; set; }

    // Liste
    public List<AtmosphereItem> Atmospheres { get; set; } = new();

    public record AtmosphereItem(
        Guid Id,
        string OwnerName,
        string Name,
        string Description,
        string Aesthetic,
        string WallColor,
        string FloorColor,
        string WeatherEffect,
        string MusicGenre,
        string AmbienceType,
        string NoiseType,
        bool IsAiGenerated,
        string? PromptUsed,
        DateTime CreatedAt);

    public async Task OnGetAsync()
    {
        TotalSavedAtmospheres = await _context.SavedAtmospheres.CountAsync();
        AiGeneratedCount = await _context.SavedAtmospheres.CountAsync(a => a.IsAiGenerated);

        // Canlı Hava Durumu Senkronu
        try
        {
            var weather = await _weatherService.GetCurrentWeatherAsync(41.0082, 28.9784); // İstanbul
            if (weather != null)
            {
                CurrentWeatherSummary = $"{weather.Description} ({weather.WeatherCondition})";
                CurrentTemperature = weather.Temperature;
            }
            else
            {
                CurrentWeatherSummary = "Açık / Standart";
                CurrentTemperature = 20.0;
            }
        }
        catch
        {
            CurrentWeatherSummary = "Açık / Standart";
            CurrentTemperature = 20.0;
        }

        // Atmosfer Sorgusu
        var query = _context.SavedAtmospheres
            .Include(a => a.User)
            .AsNoTracking();

        if (Filter == "ai")
        {
            query = query.Where(a => a.IsAiGenerated);
        }
        else if (Filter == "manual")
        {
            query = query.Where(a => !a.IsAiGenerated);
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var pattern = $"%{Search.Trim()}%";
            query = query.Where(a => EF.Functions.ILike(a.Name, pattern) ||
                                     EF.Functions.ILike(a.Description, pattern) ||
                                     (a.PromptUsed != null && EF.Functions.ILike(a.PromptUsed, pattern)));
        }

        Atmospheres = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(50)
            .Select(a => new AtmosphereItem(
                a.Id,
                a.User.DisplayName,
                a.Name,
                a.Description,
                a.Aesthetic,
                a.WallColor,
                a.FloorColor,
                a.WeatherEffect,
                a.MusicGenre,
                a.AmbienceType,
                a.NoiseType,
                a.IsAiGenerated,
                a.PromptUsed,
                a.CreatedAt))
            .ToListAsync();
    }
}
