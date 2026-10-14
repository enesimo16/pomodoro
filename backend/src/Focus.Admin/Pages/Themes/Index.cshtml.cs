using Focus.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Focus.Admin.Pages.Themes;

public class IndexModel : PageModel
{
    private readonly IThemeVideoService _videoService;
    private readonly IApplicationDbContext _context;

    public IndexModel(IThemeVideoService videoService, IApplicationDbContext context)
    {
        _videoService = videoService;
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SourceFilter { get; set; }

    public int TotalCount => Themes.Count;
    public int PixabayCount => Themes.Count(t => t.Source == "Pixabay API");
    public int AiPresetCount => Themes.Count(t => t.Source == "Yapay Zeka");
    public int SavedDbCount => Themes.Count(t => t.Source == "Kayıtlı Kütüphane");

    public List<ThemeItemViewModel> Themes { get; set; } = new();

    public record ThemeItemViewModel(
        string Id,
        string Title,
        string Source, // "Pixabay API", "Yapay Zeka", "Kayıtlı Kütüphane"
        string Aesthetic,
        string VideoUrl,
        string ThumbnailUrl,
        string WallColor,
        string FloorColor,
        string AccentColor,
        string WeatherEffect,
        string Description,
        int RecommendedMinutes,
        DateTime? CreatedAt = null);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var list = new List<ThemeItemViewModel>();

        // 1. Yapay Zeka Tasarımı Piksel Oda Temaları (7 Çekirdek Tema)
        var aiThemes = new List<ThemeItemViewModel>
        {
            new(
                "ai-cyberpunk",
                "Cyberpunk Neon Gece",
                "Yapay Zeka",
                "CyberpunkNeon",
                "https://cdn.pixabay.com/video/2017/08/30/11722-231759069_medium.mp4",
                "https://images.unsplash.com/photo-1508739773434-c26b3d09e071?w=640",
                "#100e17",
                "#1a162b",
                "#00fff5",
                "NeonGlow",
                "Siberpunk şehir manzarası, turkuaz ve neon mor ışıklandırma ile yüksek bilişsel tempo.",
                50
            ),
            new(
                "ai-zen",
                "Japon Zen Bahçesi",
                "Yapay Zeka",
                "JapaneseZen",
                "https://cdn.pixabay.com/video/2020/07/28/45763-444458316_medium.mp4",
                "https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?w=640",
                "#2b2621",
                "#3a322b",
                "#c8a870",
                "ClearSky",
                "Bambu ağaçları, su sesi ve doğal toprak tonlarıyla sakinleştirici derin akış.",
                45
            ),
            new(
                "ai-rainyloft",
                "Yağmurlu Çatı Katı",
                "Yapay Zeka",
                "RainyLoft",
                "https://cdn.pixabay.com/video/2017/08/30/11722-231759069_medium.mp4",
                "https://images.unsplash.com/photo-1519692933481-e162a57d6721?w=640",
                "#1c212b",
                "#252d3a",
                "#64b5f6",
                "RainDrops",
                "Tavan camına vuran yağmur damlaları ve koyu kurşuni tonlarla mükemmel kodlama izolasyonu.",
                60
            ),
            new(
                "ai-midnight",
                "Gece Yarısı Minimal",
                "Yapay Zeka",
                "MidnightMinimal",
                "https://cdn.pixabay.com/video/2016/09/21/5391-183787728_medium.mp4",
                "https://images.unsplash.com/photo-1506744038136-46273834b3fb?w=640",
                "#121212",
                "#1e1e1e",
                "#e0e0e0",
                "ClearSky",
                "Sıfır dikkat dağınıklığı sunan monokrom gece yarısı teması.",
                45
            ),
            new(
                "ai-retro",
                "Retro 80s Piksel Stüdyo",
                "Yapay Zeka",
                "RetroStudio",
                "https://cdn.pixabay.com/video/2021/04/12/70868-537446162_medium.mp4",
                "https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=640",
                "#2e112d",
                "#3c1b3c",
                "#ff007f",
                "NeonGlow",
                "Magenta CRT monitör parıltısı ve 8-bit nostalji.",
                35
            ),
            new(
                "ai-cabin",
                "Sıcak Ahşap Dağ Evi",
                "Yapay Zeka",
                "CozyCabin",
                "https://cdn.pixabay.com/video/2020/12/17/59489-491953795_medium.mp4",
                "https://images.unsplash.com/photo-1542601906990-b4d3fb778b09?w=640",
                "#332211",
                "#442e18",
                "#ffaa44",
                "ClearSky",
                "Çıtırtılı şömine ateşi ve ceviz ahşap mobilyalar.",
                45
            ),
            new(
                "ai-spacestation",
                "Kozmik Yörünge İstasyonu",
                "Yapay Zeka",
                "SpaceStation",
                "https://cdn.pixabay.com/video/2019/04/23/23011-332468305_medium.mp4",
                "https://images.unsplash.com/photo-1451187580459-43490279c0fa?w=640",
                "#0d1b2a",
                "#1b263b",
                "#48cae4",
                "ClearSky",
                "Dünya yörüngesi manzarası ve derin sessizlik.",
                90
            )
        };
        list.AddRange(aiThemes);

        // 2. Pixabay API Canlı Video Temaları
        try
        {
            var pxbVideos = await _videoService.SearchVideosAsync("rain window cozy fireplace city", 8, cancellationToken);
            foreach (var v in pxbVideos)
            {
                list.Add(new ThemeItemViewModel(
                    $"pixabay-{v.Id}",
                    CapitalizeFirst(v.Tags?.Split(',').FirstOrDefault() ?? "Döngüsel Video Teması"),
                    "Pixabay API",
                    "ExternalMedia",
                    v.MediumVideoUrl,
                    v.ThumbnailUrl,
                    "#1e293b",
                    "#0f172a",
                    "#38bdf8",
                    "RainDrops",
                    $"Pixabay HD Döngüsü • Süre: {v.Duration}s • Etiketler: {v.Tags}",
                    45
                ));
            }
        }
        catch
        {
            // API kotası veya ağ hatasında AI şablonları güvence sağlar
        }

        // 3. Veritabanından Kullanıcı Kayıtlı Temaları
        try
        {
            var savedFromDb = await _context.SavedAtmospheres
                .OrderByDescending(s => s.CreatedAt)
                .Take(10)
                .ToListAsync(cancellationToken);

            foreach (var s in savedFromDb)
            {
                list.Add(new ThemeItemViewModel(
                    $"saved-{s.Id}",
                    s.Name,
                    "Kayıtlı Kütüphane",
                    s.Aesthetic,
                    "https://cdn.pixabay.com/video/2017/08/30/11722-231759069_medium.mp4",
                    "https://images.unsplash.com/photo-1519692933481-e162a57d6721?w=640",
                    s.WallColor,
                    s.FloorColor,
                    s.AccentLightColor,
                    s.WeatherEffect,
                    s.Description ?? "Kullanıcı tarafından kaydedilmiş özel atmosfer.",
                    s.RecommendedMinutes,
                    s.CreatedAt
                ));
            }
        }
        catch
        {
            // DB erişilemezse devam et
        }

        // Filtreleme ve Arama
        if (!string.IsNullOrWhiteSpace(SourceFilter) && SourceFilter != "All")
        {
            list = list.Where(t => t.Source.Equals(SourceFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var s = Search.Trim();
            list = list.Where(t => 
                t.Title.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                t.Aesthetic.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(s, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        Themes = list;
    }

    private static string CapitalizeFirst(string str)
    {
        str = str.Trim();
        if (string.IsNullOrEmpty(str)) return str;
        return char.ToUpper(str[0], System.Globalization.CultureInfo.InvariantCulture) + str[1..];
    }
}
