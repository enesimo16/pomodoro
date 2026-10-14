using Focus.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Focus.Admin.Pages.Music;

public class IndexModel : PageModel
{
    private readonly IMusicTrackService _musicService;

    public IndexModel(IMusicTrackService musicService)
    {
        _musicService = musicService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SourceFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Genre { get; set; } = "All";

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 12;
    public int TotalPages { get; set; } = 10;

    public int TotalCount => Tracks.Count;
    public int JamendoCount => Tracks.Count(t => t.Source == "Jamendo API");
    public int AiMixerCount => Tracks.Count(t => t.Source == "Yapay Zeka Mikser");
    public int AmbientNoiseCount => Tracks.Count(t => t.Source == "Doğa & Gürültü");

    public List<SoundTrackViewModel> Tracks { get; set; } = new();

    public record SoundTrackViewModel(
        string Id,
        string Title,
        string Artist,
        string Source, // "Jamendo API", "Yapay Zeka Mikser", "Doğa & Gürültü"
        string Genre,
        int Bpm,
        string AudioUrl,
        string CoverUrl,
        int MelodyPercent,
        int AmbiencePercent,
        int NoisePercent,
        int TexturePercent,
        string AmbienceType,
        string NoiseType,
        string Description);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (PageNumber < 1) PageNumber = 1;
        var list = new List<SoundTrackViewModel>();

        var genreQuery = Genre?.ToLowerInvariant() switch
        {
            "lofi" => "lofi chillhop beats",
            "synthwave" => "synthwave retrowave cyberpunk",
            "ambient" => "ambient drone deep meditation",
            "piano" => "classical piano solo instrumental",
            "jazz" => "jazz cafe acoustic trio",
            "alpha" => "alpha waves binaural sleep focus",
            "nature" => "nature rain forest stream",
            _ => string.IsNullOrWhiteSpace(Search) ? "lofi" : Search.Trim()
        };

        // 1. Yapay Zeka 4-Kanallı Akustik Mikser Presets
        var aiPresets = new List<SoundTrackViewModel>
        {
            new(
                "ai-deep-code",
                "Derin Kodlama Akışı",
                "Focus AI Mikser Motoru",
                "Yapay Zeka Mikser",
                "Lo-Fi Synthwave",
                78,
                "https://cdn.pixabay.com/download/audio/2022/05/27/audio_1808fbf07a.mp3",
                "https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=120",
                50, 85, 60, 20,
                "Pencere Yağmuru", "Brown Noise (Derin)",
                "Sağanak pencere yağmuru ve 450Hz kesimli Brown Noise ile dikkat dağıtıcıları tamamen bloke eden ritim."
            ),
            new(
                "ai-chill-read",
                "Sakin Okuma & Araştırma",
                "Focus AI Mikser Motoru",
                "Yapay Zeka Mikser",
                "Chillhop Akustik",
                65,
                "https://cdn.pixabay.com/download/audio/2022/01/18/audio_d0a13f69d2.mp3",
                "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?w=120",
                70, 60, 25, 30,
                "Orman ve Kuşlar", "Pink Noise (Doğal)",
                "Minimal Fender Rhodes melodisi, uzaktan gelen kuş cıvıltıları ve hafif kaset tıslaması."
            ),
            new(
                "ai-zen-meditate",
                "Zen Meditasyon & Alfa Dalgası",
                "Focus AI Mikser Motoru",
                "Yapay Zeka Mikser",
                "Alfa Dalgası Zen",
                60,
                "https://cdn.pixabay.com/download/audio/2022/03/15/audio_c8c8a73467.mp3",
                "https://images.unsplash.com/photo-1518241353330-0f7941c2d9b5?w=120",
                30, 90, 75, 15,
                "Pencere Yağmuru", "Brown Noise (Yoğun)",
                "Zihinsel dinginlik ve stres azaltıcı düşük BPM alfa frekansları."
            ),
            new(
                "ai-night-shift",
                "Gece Yarısı Terminal Ruhu",
                "Focus AI Mikser Motoru",
                "Yapay Zeka Mikser",
                "Cyberpunk Ambient",
                82,
                "https://cdn.pixabay.com/download/audio/2022/10/14/audio_9939f772dd.mp3",
                "https://images.unsplash.com/photo-1508739773434-c26b3d09e071?w=120",
                65, 50, 35, 30,
                "Gece Şehri", "Analog Radyo Uğultusu",
                "Koyu analog synthesizer pad'leri ve hafif mekanik klavye tıkırtısı hissi."
            ),
            new(
                "ai-cozy-fireplace",
                "Sıcak Şömine & Akustik Gitar",
                "Focus AI Mikser Motoru",
                "Yapay Zeka Mikser",
                "Akustik Folk Lo-Fi",
                72,
                "https://cdn.pixabay.com/download/audio/2021/08/04/audio_12b0c7443c.mp3",
                "https://images.unsplash.com/photo-1542601906990-b4d3fb778b09?w=120",
                60, 75, 20, 40,
                "Şömine Çatırtısı", "Plak Cızırtısı (Vinyl)",
                "Odun çıtırtıları eşliğinde sıcak parmak stili akustik arpejler."
            )
        };
        list.AddRange(aiPresets);

        // 2. Jamendo API Parçaları
        try
        {
            var jamendoTracks = await _musicService.SearchTracksAsync(genreQuery, PageSize, PageNumber, cancellationToken);
            foreach (var t in jamendoTracks)
            {
                list.Add(new SoundTrackViewModel(
                    $"jamendo-{t.Id}",
                    t.Name,
                    t.ArtistName,
                    "Jamendo API",
                    "Lo-Fi Instrumental",
                    80,
                    t.AudioUrl,
                    t.ImageUrl ?? "https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=120",
                    65, 40, 20, 15,
                    "Ortam Ambiyansı", "Doğal Akış",
                    $"Jamendo Telifsiz Koleksiyon • Süre: {t.Duration}s"
                ));
            }
        }
        catch
        {
            // API hatasında AI şablonları devam eder
        }

        // 3. Doğa Ambiyansı ve Prosedürel Bilişsel Gürültü Katmanları
        var ambientLayers = new List<SoundTrackViewModel>
        {
            new(
                "ambient-brown-noise",
                "Brown Noise (Derin Bilişsel Kalkan)",
                "Focus Web Audio Generator",
                "Doğa & Gürültü",
                "Synthesized Noise",
                0,
                "https://cdn.pixabay.com/download/audio/2022/05/27/audio_1808fbf07a.mp3",
                "https://images.unsplash.com/photo-1506744038136-46273834b3fb?w=120",
                0, 0, 90, 0,
                "Yok", "Brown Noise (1/f²)",
                "DEHB (ADHD) ve derin odaklanma için optimize edilmiş düşük frekanslı spektral gürültü."
            ),
            new(
                "ambient-rain-window",
                "Pencereye Vuran Sürekli Yağmur",
                "Focus Atmosfer Ses Bankası",
                "Doğa & Gürültü",
                "Doğal Ambiyans",
                0,
                "https://cdn.pixabay.com/download/audio/2022/01/18/audio_d0a13f69d2.mp3",
                "https://images.unsplash.com/photo-1519692933481-e162a57d6721?w=120",
                0, 95, 0, 20,
                "Cam Damlaları", "Yok",
                "Dış dünyayı susturan, ritmik ve dinginleştirici yağmur taneleri."
            ),
            new(
                "ambient-fireplace",
                "Şömine Çatırtısı & Odun Korları",
                "Focus Atmosfer Ses Bankası",
                "Doğa & Gürültü",
                "Doğal Ambiyans",
                0,
                "https://cdn.pixabay.com/download/audio/2021/08/04/audio_12b0c7443c.mp3",
                "https://images.unsplash.com/photo-1542601906990-b4d3fb778b09?w=120",
                0, 85, 0, 45,
                "Şömine Odunu", "Yok",
                "Sıcak ahşap kulübe hissi veren rahatlatıcı mikro çatırtılar."
            ),
            new(
                "ambient-vinyl",
                "Vinil Plak İğne Cızırtısı & Hışırtı",
                "Focus Analog Doku Bankası",
                "Doğa & Gürültü",
                "Analog Doku",
                0,
                "https://cdn.pixabay.com/download/audio/2022/10/14/audio_9939f772dd.mp3",
                "https://images.unsplash.com/photo-1539185441755-769473a23570?w=120",
                0, 0, 0, 85,
                "Yok", "Vinyl Crackle (45 RPM)",
                "Dijital soğukluğu kıran, sıcak nostaljik pikap iğnesi dokusu."
            )
        };
        list.AddRange(ambientLayers);

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
                t.Artist.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                t.Genre.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(s, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        Tracks = list;
    }
}
