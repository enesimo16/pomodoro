using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace Focus.Admin.Pages.ApiDocs;

public class IndexModel : PageModel
{
    private readonly IConfiguration _configuration;

    public string WebApiBaseUrl { get; set; } = "http://localhost:5000";
    public int AdminKeyCount { get; set; }
    public List<ApiCategoryDoc> Categories { get; set; } = new();
    public List<SignalRHubDoc> Hubs { get; set; } = new();

    public IndexModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void OnGet()
    {
        WebApiBaseUrl = _configuration.GetValue<string>("WebApiBaseUrl") ?? "http://localhost:5000";
        AdminKeyCount = _configuration.GetSection("AdminSecurity:Keys").Get<string[]>()?.Count(k => !string.IsNullOrWhiteSpace(k)) ?? 0;
        Categories = BuildCategories();
        Hubs = BuildHubs();
    }

    private const string UserJson = """
        {
          "id": "7a1d3f92-8021-4b11-a887-32145b9812af",
          "displayName": "Misafir#4821",
          "email": null,
          "isGuest": true,
          "level": 1,
          "currentXp": 0,
          "locale": "tr-TR",
          "timeZoneId": "Europe/Istanbul",
          "createdAt": "2026-10-06T10:00:00Z"
        }
        """;

    private const string AuthResponseJson = """
        {
          "user": { "id": "7a1d3f92-8021-4b11-a887-32145b9812af", "displayName": "Misafir#4821", "email": null, "isGuest": true, "level": 1, "currentXp": 0, "locale": "tr-TR", "timeZoneId": "Europe/Istanbul", "createdAt": "2026-10-06T10:00:00Z" },
          "avatar": { "id": "0c5e...", "userId": "7a1d3f92-8021-4b11-a887-32145b9812af", "skinTone": "tone_1", "hairStyle": "messy_short", "hairColor": "dark_brown", "topItemCatalogId": "tshirt_basic_white", "bottomItemCatalogId": "jeans_basic_blue", "hatItemCatalogId": null, "glassesItemCatalogId": null, "shoesItemCatalogId": "sneakers_white" },
          "room": { "id": "b2f1...", "ownerUserId": "7a1d3f92-8021-4b11-a887-32145b9812af", "name": "Misafir#4821'in Odası", "roomType": "Studio", "wallPaperId": "wallpaper_brick_white", "floorId": "floor_parquet_oak", "isPublic": false, "inviteCode": "A1B2C3D4", "maxVisitors": 5, "items": [] },
          "accessToken": "eyJhbGciOiJIUzI1NiIs...",
          "refreshToken": "q8Zk...",
          "expiresAt": "2026-10-06T10:15:00Z"
        }
        """;

    private const string AvatarJson = """
        {
          "id": "0c5e...",
          "userId": "7a1d3f92-8021-4b11-a887-32145b9812af",
          "skinTone": "tone_1",
          "hairStyle": "messy_short",
          "hairColor": "dark_brown",
          "topItemCatalogId": "tshirt_basic_white",
          "bottomItemCatalogId": "jeans_basic_blue",
          "hatItemCatalogId": null,
          "glassesItemCatalogId": null,
          "shoesItemCatalogId": "sneakers_white"
        }
        """;

    private const string RoomJson = """
        {
          "id": "b2f1...",
          "ownerUserId": "7a1d3f92-8021-4b11-a887-32145b9812af",
          "name": "Benim Çalışma Odam",
          "roomType": "Studio",
          "wallPaperId": "wallpaper_brick_white",
          "floorId": "floor_parquet_oak",
          "isPublic": false,
          "inviteCode": "A1B2C3D4",
          "maxVisitors": 5,
          "items": [
            { "id": "e61a...", "catalogItemId": "desk_vintage", "gridX": 4, "gridY": 5, "rotation": 0 }
          ]
        }
        """;

    private const string RoomItemJson = """
        { "id": "e61a...", "catalogItemId": "desk_vintage", "gridX": 4, "gridY": 5, "rotation": 0 }
        """;

    private const string SessionJson = """
        {
          "id": "5d0c...",
          "userId": "7a1d3f92-8021-4b11-a887-32145b9812af",
          "roomId": null,
          "kind": "Focus",
          "status": "Running",
          "focusMinutes": 25,
          "breakMinutes": 5,
          "plannedMinutes": 25,
          "currentRound": 1,
          "targetRounds": 4,
          "startedAt": "2026-10-06T10:00:00Z",
          "plannedEndAt": "2026-10-06T10:25:00Z",
          "endedAt": null,
          "pausedAt": null,
          "totalPausedSeconds": 0,
          "netDurationSeconds": 0,
          "remainingSeconds": 1500,
          "extensionCount": 0,
          "xpEarned": 0,
          "coinsEarned": 0,
          "themeId": null,
          "mixPresetId": null,
          "nextKind": "Break",
          "nextRound": 1,
          "isCycleFinished": false
        }
        """;

    private const string AtmosphereJson = """
        {
          "atmosphereId": null,
          "themeName": "Siber Gece & Neon Yağmur",
          "description": "Yağmurlu neon gece, derin kodlama odağı.",
          "visualTheme": { "aesthetic": "CyberpunkNeon", "wallColor": "#161224", "floorColor": "#0d0a17", "accentLightColor": "#00f0ff", "wallPaperId": null, "floorId": null, "weatherEffect": "Rain", "windowVideoQuery": "neon rain window" },
          "soundscapeMixer": {
            "presetName": "Neon Yağmur",
            "layer1Melody": { "genre": "Lo-Fi Beats", "searchQuery": "lofi synth", "volume": 0.65, "targetBpm": 80 },
            "layer2Ambience": { "ambienceType": "Rainstorm", "volume": 0.5 },
            "layer3Noise": { "noiseType": "BrownNoise", "volume": 0.35, "cutoffFrequencyHz": 400 },
            "layer4Texture": { "textureType": "VinylCrackle", "volume": 0.25 }
          },
          "recommendedSession": { "durationMinutes": 45, "breakMinutes": 5, "flowShieldLevel": "Standard", "targetRounds": 4 },
          "coachMessage": "Masa lambanı yaktım, derin odaklanma başlasın.",
          "source": "Prompt",
          "createdAt": "2026-10-06T10:00:00Z"
        }
        """;

    private const string StudyRoomJson = """
        {
          "id": "9c3a...",
          "code": "LIB-001",
          "name": "Sessiz Kütüphane",
          "description": "Birlikte sessiz çalışma odası",
          "type": "Library",
          "ownerUserId": null,
          "ownerDisplayName": null,
          "currentMemberCount": 2,
          "maxCapacity": 12,
          "isPrivate": false,
          "themeId": null,
          "musicTrackId": null,
          "coWorkingBonusPercent": 10,
          "members": [
            { "id": "1f2e...", "userId": "7a1d3f92-8021-4b11-a887-32145b9812af", "displayName": "Misafir#4821", "level": 1, "avatarSummary": null, "seatIndex": 3, "isFocusing": true, "joinedAt": "2026-10-06T10:00:00Z" }
          ],
          "focusingCount": 1
        }
        """;

    private static ApiEndpointDoc Endpoint(
        string method,
        string path,
        string title,
        string description,
        bool requiresAuth,
        string testTab,
        string? request,
        string? requestBody,
        string? response)
    {
        return new ApiEndpointDoc
        {
            Method = method,
            Path = path,
            Title = title,
            Description = description,
            RequiresAuth = requiresAuth,
            TestLabTab = testTab,
            TypeScriptRequest = request ?? "// Gövde gerekmez",
            JsonRequestBody = requestBody,
            JsonSuccessResponse = response
        };
    }

    private static List<ApiCategoryDoc> BuildCategories()
    {
        return new List<ApiCategoryDoc>
        {
            new ApiCategoryDoc
            {
                Id = "auth",
                Title = "1. Kimlik, Token & Profil (Auth)",
                Description = "Misafir hesabı, Google girişi, JWT yenileme ve oturum yönetimi. Yanıtlar sarmalayıcı (envelope) olmadan düz DTO olarak döner; hatalar RFC 9457 problem+json biçimindedir.",
                Endpoints = new List<ApiEndpointDoc>
                {
                    Endpoint("POST", "/api/v1/auth/guest", "Misafir Hesabı Aç",
                        "Kayıt formu olmadan misafir hesap, varsayılan avatar ve başlangıç odası oluşturur; HttpOnly çerezleri de atar. displayName verilirse boş olamaz ve '<' '>' içeremez.",
                        false, "#tab-auth",
                        "interface GuestLoginRequest {\n  displayName?: string; // en fazla 100 karakter\n  locale?: string;\n  timeZoneId?: string; // örn: 'Europe/Istanbul'\n}",
                        "{\n  \"displayName\": \"Misafir Odakçı\"\n}",
                        AuthResponseJson),
                    Endpoint("POST", "/api/v1/auth/google", "Google ile Giriş",
                        "Google id_token ile giriş yapar veya hesap oluşturur. Açık bir misafir oturumu varsa hesap o misafire bağlanır.",
                        false, "#tab-auth",
                        "interface GoogleLoginRequest {\n  idToken: string;\n}",
                        "{\n  \"idToken\": \"<google_id_token>\"\n}",
                        AuthResponseJson),
                    Endpoint("POST", "/api/v1/auth/claim", "Misafir Hesabını Sahiplen",
                        "Oturum açmış misafir hesabını Google kimliğiyle kalıcı hesaba dönüştürür.",
                        true, "#tab-auth",
                        "interface GoogleLoginRequest {\n  idToken: string;\n}",
                        "{\n  \"idToken\": \"<google_id_token>\"\n}",
                        AuthResponseJson),
                    Endpoint("POST", "/api/v1/auth/refresh", "Token Yenile (Refresh)",
                        "Refresh token ile yeni access/refresh token çifti üretir. Gövde boşsa focus_rt çerezi kullanılır.",
                        false, "#tab-auth",
                        "interface RefreshTokenRequest {\n  refreshToken?: string;\n}",
                        "{\n  \"refreshToken\": \"q8Zk...\"\n}",
                        AuthResponseJson),
                    Endpoint("POST", "/api/v1/auth/logout", "Çıkış Yap",
                        "Refresh token'ı iptal eder ve çerezleri temizler.",
                        false, "#tab-auth",
                        "interface RevokeTokenRequest {\n  refreshToken?: string;\n}",
                        "{\n  \"refreshToken\": \"q8Zk...\"\n}",
                        null),
                    Endpoint("GET", "/api/v1/auth/me", "Profil Bilgisi Getir",
                        "Oturum açmış kullanıcının profilini, avatarını, odasını ve toplam coin bakiyesini döner.",
                        true, "#tab-auth", null, null,
                        "{\n  \"user\": " + UserJson.Replace("\n", "\n  ") + ",\n  \"avatar\": { \"...\": \"AvatarDto\" },\n  \"room\": { \"...\": \"PixelRoomDto\" },\n  \"totalCoins\": 50\n}")
                }
            },
            new ApiCategoryDoc
            {
                Id = "avatar",
                Title = "2. Piksel Karakter & Gardırop (Avatar)",
                Description = "Karakter görünümü ve kuşanılan kıyafetler.",
                Endpoints = new List<ApiEndpointDoc>
                {
                    Endpoint("GET", "/api/v1/avatar", "Aktif Avatarı Getir",
                        "Kullanıcının ten rengi, saç stili/rengi ve üzerindeki kıyafet katalog kimliklerini döner.",
                        true, "#tab-avatar", null, null, AvatarJson),
                    Endpoint("PATCH", "/api/v1/avatar", "Avatar Görünümünü Güncelle",
                        "Gönderilen alanları günceller. Kıyafet alanları envanterde sahip olunan katalog kimlikleri olmalıdır; şapka/gözlük çıkarmak için clearHat/clearGlasses kullanılır.",
                        true, "#tab-avatar",
                        "interface UpdateAvatarRequest {\n  skinTone?: string;\n  hairStyle?: string;\n  hairColor?: string;\n  topItemId?: string;\n  bottomItemId?: string;\n  hatItemId?: string;\n  glassesItemId?: string;\n  shoesItemId?: string;\n  clearHat?: boolean;\n  clearGlasses?: boolean;\n}",
                        "{\n  \"hairStyle\": \"messy_short\",\n  \"topItemId\": \"tshirt_basic_white\",\n  \"clearHat\": true\n}",
                        AvatarJson)
                }
            },
            new ApiCategoryDoc
            {
                Id = "room",
                Title = "3. İzometrik Piksel Oda & Dekorasyon (Room)",
                Description = "Kişisel çalışma odası, duvar/zemin kaplamaları ve mobilya yerleşimi.",
                Endpoints = new List<ApiEndpointDoc>
                {
                    Endpoint("GET", "/api/v1/room", "Odamı Getir",
                        "Kullanıcının odasını, tema kimliklerini ve yerleştirilmiş mobilyaları döner.",
                        true, "#tab-room", null, null, RoomJson),
                    Endpoint("GET", "/api/v1/room/{id}", "Odayı Kimliğiyle Getir",
                        "Herkese açık bir odayı veya sahibi olduğunuz odayı kimliğiyle getirir; davet kodu yalnızca sahibine görünür.",
                        false, "#tab-room", null, null, RoomJson),
                    Endpoint("POST", "/api/v1/room/items", "Mobilya Yerleştir",
                        "Envanterdeki bir eşyayı ızgara koordinatına yerleştirir (201 Created). Izgara sınırı ve oda eşya üst sınırı sunucuda denetlenir.",
                        true, "#tab-room",
                        "interface AddRoomItemRequest {\n  catalogItemId: string;\n  gridX: number;\n  gridY: number;\n  rotation: number; // 0, 90, 180, 270\n}",
                        "{\n  \"catalogItemId\": \"desk_vintage\",\n  \"gridX\": 4,\n  \"gridY\": 5,\n  \"rotation\": 0\n}",
                        RoomItemJson),
                    Endpoint("PUT", "/api/v1/room/items/{id}", "Mobilyayı Taşı",
                        "Yerleştirilmiş mobilyanın koordinatını ve yönünü günceller.",
                        true, "#tab-room",
                        "interface MoveRoomItemRequest {\n  gridX: number;\n  gridY: number;\n  rotation: number;\n}",
                        "{\n  \"gridX\": 6,\n  \"gridY\": 2,\n  \"rotation\": 90\n}",
                        RoomItemJson),
                    Endpoint("DELETE", "/api/v1/room/items/{id}", "Mobilyayı Kaldır",
                        "Mobilyayı odadan kaldırır (204 No Content).",
                        true, "#tab-room", null, null, null),
                    Endpoint("PATCH", "/api/v1/room/theme", "Duvar & Zemin Temasını Değiştir",
                        "Sahip olunan duvar kağıdı ve zemin katalog kimliklerini odaya uygular.",
                        true, "#tab-room",
                        "interface UpdateRoomThemeRequest {\n  wallpaperCatalogId?: string;\n  floorCatalogId?: string;\n}",
                        "{\n  \"wallpaperCatalogId\": \"wallpaper_brick_white\",\n  \"floorCatalogId\": \"floor_parquet_oak\"\n}",
                        RoomJson),
                    Endpoint("PUT", "/api/v1/room/details", "Oda Bilgilerini Güncelle",
                        "Oda adını, görünürlüğünü ve ziyaretçi kapasitesini günceller.",
                        true, "#tab-room",
                        "interface UpdateRoomDetailsRequest {\n  name: string;\n  isPublic: boolean;\n  maxVisitors?: number; // varsayılan 5\n}",
                        "{\n  \"name\": \"Gece Çalışma Odam\",\n  \"isPublic\": true,\n  \"maxVisitors\": 5\n}",
                        RoomJson)
                }
            },
            new ApiCategoryDoc
            {
                Id = "session",
                Title = "4. Odak Sayacı & Flow Shield (Session)",
                Description = "Sunucu otoriteli odak/mola seansları. Durum değişiklikleri TimerHub üzerinden de yayınlanır. Kimlik gövdesiz uçlar aktif seansı kullanır, {id} varyantları belirli bir seansı hedefler.",
                Endpoints = new List<ApiEndpointDoc>
                {
                    Endpoint("GET", "/api/v1/sessions/active", "Aktif Seansı Getir",
                        "Devam eden veya duraklatılmış seansı döner; yoksa 204 No Content.",
                        true, "#tab-session", null, null, SessionJson),
                    Endpoint("POST", "/api/v1/sessions", "Seans Başlat",
                        "Odak veya mola seansı başlatır (201 Created). Aktif seans varsa 409 Conflict döner.",
                        true, "#tab-session",
                        "interface StartSessionRequest {\n  kind?: 'Focus' | 'Break'; // varsayılan 'Focus'\n  focusMinutes?: number; // varsayılan 25\n  breakMinutes?: number; // varsayılan 5\n  currentRound?: number; // varsayılan 1\n  targetRounds?: number; // varsayılan 4\n  themeId?: string;\n  mixPresetId?: string;\n}",
                        "{\n  \"kind\": \"Focus\",\n  \"focusMinutes\": 25,\n  \"breakMinutes\": 5,\n  \"targetRounds\": 4\n}",
                        SessionJson),
                    Endpoint("POST", "/api/v1/sessions/pause", "Seansı Duraklat",
                        "Aktif seansı duraklatır; duraklama süresi net süreden düşülür. Belirli seans için /sessions/{id}/pause kullanılır.",
                        true, "#tab-session", null, null, SessionJson),
                    Endpoint("POST", "/api/v1/sessions/resume", "Seansı Sürdür",
                        "Duraklatılmış seansı sürdürür. Belirli seans için /sessions/{id}/resume kullanılır.",
                        true, "#tab-session", null, null, SessionJson),
                    Endpoint("POST", "/api/v1/sessions/extend", "Seansı Uzat",
                        "Planlanan bitişi uzatır. Belirli seans için /sessions/{id}/extend kullanılır.",
                        true, "#tab-session",
                        "interface ExtendSessionRequest {\n  extraMinutes?: number; // varsayılan 10\n}",
                        "{\n  \"extraMinutes\": 10\n}",
                        SessionJson),
                    Endpoint("POST", "/api/v1/sessions/complete", "Seansı Tamamla",
                        "Seansı sunucu süresine göre tamamlar; XP ve Focus Coin ödülünü hesaplar. Belirli seans için /sessions/{id}/complete kullanılır.",
                        true, "#tab-session", null, null,
                        "{\n  \"session\": { \"...\": \"FocusSessionDto\" },\n  \"xpEarned\": 180,\n  \"coinsEarned\": 45,\n  \"newLevel\": 2,\n  \"totalXp\": 330,\n  \"totalCoins\": 95,\n  \"levelUp\": true,\n  \"nextKind\": \"Break\",\n  \"nextRound\": 1,\n  \"isCycleFinished\": false\n}"),
                    Endpoint("POST", "/api/v1/sessions/abandon", "Seansı Bırak",
                        "Seansı terk eder (ödül verilmez). Belirli seans için /sessions/{id}/abandon kullanılır.",
                        true, "#tab-session", null, null, SessionJson)
                }
            },
            new ApiCategoryDoc
            {
                Id = "media",
                Title = "5. Medya, Canlı Video & Ambiyans (Media)",
                Description = "Pixabay arkaplan videoları, Jamendo müzik parçaları ve Open-Meteo hava durumu. Tüm uçlar sunucu tarafında anahtar saklar; istemci anahtar görmez.",
                Endpoints = new List<ApiEndpointDoc>
                {
                    Endpoint("GET", "/api/v1/media/themes", "Pixabay Arkaplan Videoları",
                        "Oda penceresi için döngüsel video arar.",
                        false, "#tab-media",
                        "// Query: ?query=rain+window&perPage=12&page=1",
                        null,
                        "[\n  {\n    \"id\": 1204,\n    \"pageUrl\": \"https://pixabay.com/videos/...\",\n    \"duration\": 24,\n    \"thumbnailUrl\": \"https://i.vimeocdn.com/video/...\",\n    \"mediumVideoUrl\": \"https://cdn.pixabay.com/video/...\",\n    \"largeVideoUrl\": \"https://cdn.pixabay.com/video/...\",\n    \"tags\": \"rain, window, cozy\"\n  }\n]"),
                    Endpoint("GET", "/api/v1/media/tracks", "Telifsiz Müzik Parçaları",
                        "Plak çalar için Jamendo üzerinden Lo-Fi, caz ve enstrümantal parça arar.",
                        false, "#tab-media",
                        "// Query: ?query=lofi&limit=12&page=1",
                        null,
                        "[\n  {\n    \"id\": \"1532\",\n    \"name\": \"Late Night Study\",\n    \"duration\": 164,\n    \"artistName\": \"Lofi Artist\",\n    \"audioUrl\": \"https://prod-1.storage.jamendo.com/...\",\n    \"imageUrl\": \"https://usercontent.jamendo.com/...\",\n    \"licenseCcUrl\": \"https://creativecommons.org/licenses/by/3.0/\"\n  }\n]"),
                    Endpoint("GET", "/api/v1/media/weather", "Open-Meteo Hava Durumu",
                        "Koordinata göre canlı hava durumunu ve önerilen oda temasını döner. weatherCode yalnızca simülasyon içindir. Koordinatlar geçersizse 400, sağlayıcı yanıt vermezse 503 döner.",
                        false, "#tab-media",
                        "// Query: ?latitude=41.0082&longitude=28.9784&weatherCode=61 (isteğe bağlı)",
                        null,
                        "{\n  \"temperature\": 18.5,\n  \"windSpeed\": 12.3,\n  \"weatherCode\": 61,\n  \"weatherCondition\": \"Rain\",\n  \"suggestedTheme\": \"rainy_window\",\n  \"windowEffect\": \"RainDrops\",\n  \"soundscapeRecommendation\": \"Rainstorm\",\n  \"roomLightingColor\": \"#6a7fa8\",\n  \"description\": \"Hafif yağmur\",\n  \"isDay\": false,\n  \"time\": \"2026-10-06T21:00:00Z\"\n}")
                }
            },
            new ApiCategoryDoc
            {
                Id = "shop",
                Title = "6. Mağaza, Katalog & Ekonomi (Shop)",
                Description = "Focus Coin ile mobilya ve kostüm satın alma, envanter ve seri dondurma. category alanları sayısal enum değeridir (Desk=1, Chair=2, Furniture=3, Clothing=4, Accessory=5, Wallpaper=6, Floor=7).",
                Endpoints = new List<ApiEndpointDoc>
                {
                    Endpoint("GET", "/api/v1/shop/catalog", "Mağaza Kataloğunu Getir",
                        "Satın alınabilir eşyaları listeler; oturum açıksa sahiplik ve bakiye durumu da hesaplanır.",
                        false, "#tab-shop",
                        "// Query: ?category=3&tier=1&search=neon (hepsi isteğe bağlı)",
                        null,
                        "[\n  {\n    \"id\": \"desk_vintage\",\n    \"name\": \"Vintage Masa\",\n    \"description\": \"Ahşap çalışma masası\",\n    \"category\": 1,\n    \"categoryName\": \"Masa\",\n    \"tier\": 1,\n    \"requiredLevel\": 1,\n    \"coinPrice\": 120,\n    \"spriteUrl\": \"/sprites/desk_vintage.png\",\n    \"isInteractive\": false,\n    \"gridWidth\": 2,\n    \"gridHeight\": 1,\n    \"isOwned\": false,\n    \"canAfford\": true,\n    \"canUnlock\": true\n  }\n]"),
                    Endpoint("GET", "/api/v1/shop/inventory", "Kullanıcı Envanteri",
                        "Sahip olunan eşyaları ve kuşanılma durumunu döner.",
                        true, "#tab-shop",
                        "// Query: ?category=3 (isteğe bağlı)",
                        null,
                        "[\n  {\n    \"id\": \"3b7d...\",\n    \"catalogItemId\": \"desk_vintage\",\n    \"itemName\": \"Vintage Masa\",\n    \"itemDescription\": \"Ahşap çalışma masası\",\n    \"category\": 1,\n    \"categoryName\": \"Masa\",\n    \"spriteUrl\": \"/sprites/desk_vintage.png\",\n    \"isInteractive\": false,\n    \"gridWidth\": 2,\n    \"gridHeight\": 1,\n    \"isEquipped\": false,\n    \"acquiredAt\": \"2026-10-06T10:00:00Z\"\n  }\n]"),
                    Endpoint("POST", "/api/v1/shop/buy", "Eşya Satın Al",
                        "Bakiyeyi ve seviye şartını denetler, coin düşer ve eşyayı envantere ekler. Başarısızsa 400 ve aynı gövde şekli döner.",
                        true, "#tab-shop",
                        "interface BuyItemRequest {\n  catalogItemId: string;\n}",
                        "{\n  \"catalogItemId\": \"desk_vintage\"\n}",
                        "{\n  \"success\": true,\n  \"message\": \"Satın alındı.\",\n  \"newCoinBalance\": 380,\n  \"inventoryItem\": { \"...\": \"UserInventoryItemDto\" }\n}"),
                    Endpoint("POST", "/api/v1/shop/buy-freeze", "Seri Dondurma Hakkı Satın Al",
                        "Coin karşılığı seri dondurma hakkı satın alır.",
                        true, "#tab-shop", null, null,
                        "{\n  \"success\": true,\n  \"message\": \"Seri dondurma hakkı alındı.\",\n  \"remainingCoins\": 300,\n  \"availableFreezes\": 2\n}")
                }
            },
            new ApiCategoryDoc
            {
                Id = "stats",
                Title = "7. İstatistikler, Seri (Streak) & Wrapped",
                Description = "Odak istatistikleri, günlük seri ve haftalık/aylık özet.",
                Endpoints = new List<ApiEndpointDoc>
                {
                    Endpoint("GET", "/api/v1/stats", "Odak İstatistikleri",
                        "Toplam, günlük, haftalık ve aylık odak dakikalarını ve son 7 günlük dağılımı döner.",
                        true, "#tab-stats", null, null,
                        "{\n  \"totalFocusMinutes\": 1240,\n  \"totalCompletedSessions\": 38,\n  \"todayFocusMinutes\": 50,\n  \"todaySessionCount\": 2,\n  \"thisWeekFocusMinutes\": 310,\n  \"thisMonthFocusMinutes\": 980,\n  \"currentStreak\": 4,\n  \"longestStreak\": 9,\n  \"currentLevel\": 5,\n  \"currentXp\": 2100,\n  \"xpRequiredForCurrentLevel\": 1500,\n  \"xpRequiredForNextLevel\": 2400,\n  \"levelProgressPercentage\": 66.7,\n  \"totalCoins\": 420,\n  \"inventoryItemCount\": 6,\n  \"last7Days\": [ { \"date\": \"2026-10-06T00:00:00Z\", \"dayName\": \"Pzt\", \"minutes\": 50, \"sessionCount\": 2 } ]\n}"),
                    Endpoint("GET", "/api/v1/streak", "Günlük Seri Durumu",
                        "Mevcut seri, dondurma hakları ve sıradaki kilometre taşını döner.",
                        true, "#tab-stats", null, null,
                        "{\n  \"currentStreak\": 4,\n  \"longestStreak\": 9,\n  \"lastActivityDate\": \"2026-10-06T00:00:00Z\",\n  \"freezesAvailable\": 1,\n  \"lastFreezeUsedDate\": null,\n  \"isActiveToday\": true,\n  \"isStreakAtRisk\": false,\n  \"nextMilestoneDays\": 7,\n  \"nextMilestoneBonus\": 50\n}"),
                    Endpoint("GET", "/api/v1/wrapped/weekly", "Haftalık Focus Wrapped",
                        "Son haftanın odak özetini döner (yalnızca odak seansları hesaba katılır).",
                        true, "#tab-stats", null, null,
                        "{\n  \"period\": \"Weekly\",\n  \"startDate\": \"2026-09-30T00:00:00Z\",\n  \"endDate\": \"2026-10-06T00:00:00Z\",\n  \"...\": \"FocusWrappedDto alanları\"\n}"),
                    Endpoint("GET", "/api/v1/wrapped/monthly", "Aylık Focus Wrapped",
                        "Son ayın odak özetini döner.",
                        true, "#tab-stats", null, null,
                        "{\n  \"period\": \"Monthly\",\n  \"startDate\": \"2026-09-06T00:00:00Z\",\n  \"endDate\": \"2026-10-06T00:00:00Z\",\n  \"...\": \"FocusWrappedDto alanları\"\n}")
                }
            },
            new ApiCategoryDoc
            {
                Id = "social",
                Title = "8. Ortak Çalışma Odaları (Study Rooms)",
                Description = "Habbo tarzı ortak çalışma odaları; katılma, masaya oturma, tepki ve ziyaretçi defteri. Canlı olaylar RoomHub üzerinden akar.",
                Endpoints = new List<ApiEndpointDoc>
                {
                    Endpoint("GET", "/api/v1/study-rooms/public", "Genel Odaları Listele",
                        "Herkese açık çalışma odalarını döner.",
                        false, "#tab-social", null, null, "[\n  " + StudyRoomJson.Replace("\n", "\n  ") + "\n]"),
                    Endpoint("GET", "/api/v1/study-rooms/{code}", "Odayı Koduyla Getir",
                        "Oda bilgilerini ve üyelerini döner; yalnızca oda üyeleri erişebilir.",
                        true, "#tab-social", null, null, StudyRoomJson),
                    Endpoint("POST", "/api/v1/study-rooms/create-invite", "Davet Bağlantısı Oluştur",
                        "Kendi odanız için davet kodu ve paylaşım bağlantıları üretir.",
                        true, "#tab-social", null, null,
                        "{\n  \"roomCode\": \"A1B2C3D4\",\n  \"roomName\": \"Benim Çalışma Odam\",\n  \"inviteCode\": \"A1B2C3D4\",\n  \"directLink\": \"https://...\",\n  \"whatsAppLink\": \"https://wa.me/...\",\n  \"telegramLink\": \"https://t.me/...\",\n  \"shareMessage\": \"Benimle birlikte odaklan!\"\n}"),
                    Endpoint("POST", "/api/v1/study-rooms/invite-username", "Kullanıcı Adıyla Davet Et",
                        "Hedef kullanıcıya bildirim olarak oda daveti gönderir.",
                        true, "#tab-social",
                        "interface InviteUsernameRequest {\n  targetUsername: string;\n  targetUserId?: string;\n}",
                        "{\n  \"targetUsername\": \"Misafir#4821\"\n}",
                        "{\n  \"roomCode\": \"A1B2C3D4\",\n  \"...\": \"RoomInvitationResultDto alanları\"\n}"),
                    Endpoint("POST", "/api/v1/study-rooms/join", "Odaya Katıl",
                        "Genel odaya veya geçerli davet koduyla özel odaya katılır. Kapasite doluysa success=false döner.",
                        true, "#tab-social",
                        "interface JoinRoomRequest {\n  roomCode: string;\n  inviteCode?: string;\n}",
                        "{\n  \"roomCode\": \"LIB-001\"\n}",
                        "{\n  \"success\": true,\n  \"message\": \"Odaya katıldınız.\",\n  \"room\": { \"...\": \"StudyRoomDto\" }\n}"),
                    Endpoint("POST", "/api/v1/study-rooms/leave", "Odadan Ayrıl",
                        "Üye olunan odalardan ayrılır.",
                        true, "#tab-social", null, null,
                        "{\n  \"success\": true\n}"),
                    Endpoint("DELETE", "/api/v1/study-rooms/{code}/members/{userId}", "Üyeyi Çıkar",
                        "Oda sahibi bir üyeyi odadan çıkarır.",
                        true, "#tab-social", null, null, null),
                    Endpoint("POST", "/api/v1/study-rooms/sit", "Masaya Otur",
                        "Odadaki bir masaya oturur; masa doluysa success=false döner.",
                        true, "#tab-social",
                        "interface SitAtSeatRequest {\n  roomCode: string;\n  seatIndex: number;\n}",
                        "{\n  \"roomCode\": \"LIB-001\",\n  \"seatIndex\": 3\n}",
                        "{\n  \"success\": true,\n  \"message\": \"Masaya oturdunuz.\",\n  \"seatIndex\": 3\n}"),
                    Endpoint("POST", "/api/v1/study-rooms/reaction", "Tepki Gönder",
                        "Odaya emoji tepkisi yayınlar; hız sınırı uygulanır.",
                        true, "#tab-social",
                        "interface RoomReactionRequest {\n  roomCode: string;\n  reaction: string;\n}",
                        "{\n  \"roomCode\": \"LIB-001\",\n  \"reaction\": \"clap\"\n}",
                        "{\n  \"roomId\": \"9c3a...\",\n  \"userId\": \"7a1d3f92-8021-4b11-a887-32145b9812af\",\n  \"displayName\": \"Misafir#4821\",\n  \"reaction\": \"clap\",\n  \"sentAt\": \"2026-10-06T10:05:00Z\"\n}"),
                    Endpoint("GET", "/api/v1/study-rooms/{code}/guestbook", "Ziyaretçi Defterini Oku",
                        "Odanın ziyaretçi defteri kayıtlarını listeler.",
                        true, "#tab-social", null, null, "[\n  {\n    \"id\": \"6b1e...\",\n    \"studyRoomCode\": \"LIB-001\",\n    \"senderUserId\": \"7a1d3f92-8021-4b11-a887-32145b9812af\",\n    \"senderDisplayName\": \"Misafir#4821\",\n    \"message\": \"Güzel oda!\",\n    \"giftType\": 0,\n    \"createdAt\": \"2026-10-06T10:05:00Z\"\n  }\n]"),
                    Endpoint("POST", "/api/v1/study-rooms/{code}/guestbook", "Deftere Mesaj Yaz",
                        "Ziyaretçi defterine kısa bir mesaj bırakır.",
                        true, "#tab-social",
                        "interface CreateGuestbookEntryRequest {\n  message: string;\n  giftType?: number; // GiftType enum, varsayılan 0 (None)\n}",
                        "{\n  \"message\": \"Güzel oda!\"\n}",
                        "{\n  \"id\": \"6b1e...\",\n  \"studyRoomCode\": \"LIB-001\",\n  \"senderUserId\": \"7a1d3f92-8021-4b11-a887-32145b9812af\",\n  \"senderDisplayName\": \"Misafir#4821\",\n  \"message\": \"Güzel oda!\",\n  \"giftType\": 0,\n  \"createdAt\": \"2026-10-06T10:05:00Z\"\n}"),
                    Endpoint("DELETE", "/api/v1/study-rooms/{code}/guestbook/{id}", "Defter Kaydını Sil",
                        "Kaydı yazan veya oda sahibi kaydı siler.",
                        true, "#tab-social", null, null, null)
                }
            },
            new ApiCategoryDoc
            {
                Id = "coach",
                Title = "9. Masaüstü Yapay Zeka Koç (AI Coach)",
                Description = "Masaüstü terminal tarzı yapay zeka koçu, seans başı niyet, seans sonu değerlendirme ve hafıza.",
                Endpoints = new List<ApiEndpointDoc>
                {
                    Endpoint("POST", "/api/v1/coach/chat", "AI Koç ile Sohbet Et",
                        "Odak alışkanlıklarını ve hafızayı kullanarak yanıt üretir.",
                        true, "#tab-coach",
                        "interface CoachChatRequest {\n  message: string;\n}",
                        "{\n  \"message\": \"Bugün 45 dakika kodlama yapacağım, bana ilham ver.\"\n}",
                        "{\n  \"reply\": \"Harika! Masa lambanı yaktım, derin odaklanma başlasın.\",\n  \"detectedAnomaly\": 0,\n  \"dailyFatigueScore\": 0.2,\n  \"usedMemories\": [],\n  \"generatedNewMemory\": false\n}"),
                    Endpoint("POST", "/api/v1/coach/check-in", "Seans Başı Check-In",
                        "Ruh hali ve enerji seviyesini kaydeder. mood: Energized, Calm, Tired, Stressed, Distracted.",
                        true, "#tab-coach",
                        "interface SessionCheckInRequest {\n  sessionId?: string;\n  mood: 'Energized' | 'Calm' | 'Tired' | 'Stressed' | 'Distracted';\n  energyLevel: number; // 1-5\n  targetIntent?: string;\n}",
                        "{\n  \"mood\": \"Calm\",\n  \"energyLevel\": 4,\n  \"targetIntent\": \"Algoritma vizesi hazırlığı\"\n}",
                        "{\n  \"id\": \"4a9c...\",\n  \"sessionId\": \"5d0c...\",\n  \"userId\": \"7a1d3f92-8021-4b11-a887-32145b9812af\",\n  \"mood\": 2,\n  \"energyLevel\": 4,\n  \"targetIntent\": \"Algoritma vizesi hazırlığı\",\n  \"createdAt\": \"2026-10-06T10:00:00Z\"\n}"),
                    Endpoint("POST", "/api/v1/coach/reflection", "Seans Sonu Değerlendirme",
                        "Odak kalitesini ve seans sonu ruh halini kaydeder.",
                        true, "#tab-coach",
                        "interface SessionReflectionRequest {\n  sessionId?: string;\n  focusQuality: number; // 1-5\n  moodAfter: string; // SessionMoodAfter enum adı\n  distractionNote?: string;\n}",
                        "{\n  \"focusQuality\": 4,\n  \"moodAfter\": \"Satisfied\",\n  \"distractionNote\": \"Bildirimler\"\n}",
                        "{\n  \"id\": \"8e1b...\",\n  \"sessionId\": \"5d0c...\",\n  \"userId\": \"7a1d3f92-8021-4b11-a887-32145b9812af\",\n  \"focusQuality\": 4,\n  \"moodAfter\": 1,\n  \"distractionNote\": \"Bildirimler\",\n  \"createdAt\": \"2026-10-06T10:30:00Z\"\n}"),
                    Endpoint("GET", "/api/v1/coach/anomaly-status", "Yorgunluk & Anomali Durumu",
                        "Günlük yorgunluk skorunu ve önerilen aksiyonu döner. detectedAnomaly: None=0, HighDailyFatigue=1, WeeklyExcessiveLoad=2, AbsenceReturn=3, EmotionalStress=4.",
                        true, "#tab-coach", null, null,
                        "{\n  \"hasAnomaly\": false,\n  \"anomalyType\": 0,\n  \"dailyFatigueScore\": 0.2,\n  \"weeklyFocusMinutes\": 310,\n  \"daysSinceLastSession\": 0,\n  \"statusSummary\": \"Dengeli\",\n  \"recommendedAction\": \"Mevcut ritmi koru.\"\n}"),
                    Endpoint("GET", "/api/v1/coach/memories", "Koç Hafızası",
                        "Yapay zekanın kullanıcı hakkında tuttuğu özetleri listeler.",
                        true, "#tab-coach", null, null,
                        "[\n  {\n    \"id\": \"2c4d...\",\n    \"isGlobal\": false,\n    \"category\": 1,\n    \"content\": \"Gece saatlerinde yağmur ambiyansıyla çalışmayı tercih ediyor.\",\n    \"importance\": 3,\n    \"relevanceScore\": 0.82\n  }\n]")
                }
            },
            new ApiCategoryDoc
            {
                Id = "atmosphere",
                Title = "10. AI Atmosfer, Tema & Müzik Üretimi (Atmosphere AI)",
                Description = "Prompt veya tercihlerle oda teması ve 4 kanallı mikser üretimi, otomatik öneri ve kolektif içgörü.",
                Endpoints = new List<ApiEndpointDoc>
                {
                    Endpoint("POST", "/api/v1/ai-atmosphere/generate", "Prompt veya Tercihlerle Atmosfer Üret",
                        "Serbest metin veya seçili tercihlerden görsel palet ve 4 kanallı mikser üretir.",
                        true, "#tab-atmosphere",
                        "interface GenerateAtmosphereRequest {\n  prompt?: string;\n  focusGoal?: string;\n  moodTarget?: string;\n  preferredAmbience?: string;\n  visualAesthetic?: string;\n  timeOfDay?: string;\n}",
                        "{\n  \"prompt\": \"Yağmurlu bir siberpunk gecesinde derin kodlama odası\",\n  \"visualAesthetic\": \"CyberpunkNeon\"\n}",
                        AtmosphereJson),
                    Endpoint("GET", "/api/v1/ai-atmosphere/auto-recommend", "Otomatik Atmosfer Öner",
                        "Günün saati, yorgunluk skoru ve geçmiş seanslara göre atmosfer sentezler.",
                        true, "#tab-atmosphere", null, null, AtmosphereJson),
                    Endpoint("POST", "/api/v1/ai-atmosphere/apply", "Atmosferi Odaya Uygula",
                        "Duvar/zemin kaplamalarını ve varsayılan seans ayarlarını uygular; yalnızca sahip olunan eşyalar kabul edilir.",
                        true, "#tab-atmosphere",
                        "interface ApplyAtmosphereRequestDto {\n  roomId?: string;\n  wallPaperId?: string;\n  floorId?: string;\n  focusMinutes?: number;\n  breakMinutes?: number;\n  flowShieldEnabled?: boolean;\n  atmosphere?: GeneratedAtmosphereDto;\n}",
                        "{\n  \"wallPaperId\": \"wallpaper_brick_white\",\n  \"focusMinutes\": 45,\n  \"flowShieldEnabled\": true\n}",
                        "{\n  \"success\": true\n}"),
                    Endpoint("POST", "/api/v1/ai-atmosphere/save", "Atmosferi Kütüphaneye Kaydet",
                        "Üretilen atmosferi kalıcı olarak saklar.",
                        true, "#tab-atmosphere",
                        "interface SaveAtmosphereApiRequest {\n  atmosphere: GeneratedAtmosphereDto;\n  customName?: string;\n  name?: string;\n  promptUsed?: string;\n}",
                        "{\n  \"atmosphere\": { \"...\": \"GeneratedAtmosphereDto\" },\n  \"customName\": \"Favori Kodlama Odam\"\n}",
                        "{\n  \"id\": \"0f6a...\",\n  \"name\": \"Favori Kodlama Odam\",\n  \"...\": \"SavedAtmosphereDto alanları\"\n}"),
                    Endpoint("GET", "/api/v1/ai-atmosphere/saved", "Kayıtlı Atmosferleri Listele",
                        "Kullanıcının kütüphanesindeki atmosferleri listeler.",
                        true, "#tab-atmosphere", null, null,
                        "[\n  { \"id\": \"0f6a...\", \"name\": \"Favori Kodlama Odam\", \"...\": \"SavedAtmosphereDto alanları\" }\n]"),
                    Endpoint("DELETE", "/api/v1/ai-atmosphere/saved/{id}", "Kayıtlı Atmosferi Sil",
                        "Kütüphaneden bir atmosferi siler.",
                        true, "#tab-atmosphere", null, null,
                        "{\n  \"success\": true\n}"),
                    Endpoint("POST", "/api/v1/ai-atmosphere/contribute-insight", "Kolektif AI'ye Anonim İçgörü Aktar",
                        "Tamamlanan seansın içgörüsünü kişisel veriden arındırarak ortak hafızaya aktarır.",
                        true, "#tab-atmosphere",
                        "interface ContributeInsightApiRequest {\n  sessionId?: string;\n  plannedDurationMinutes?: number;\n  focusQuality?: number;\n  distractionOrLearningNotes?: string;\n  notes?: string;\n}",
                        "{\n  \"sessionId\": \"5d0c...\",\n  \"focusQuality\": 4\n}",
                        "{\n  \"contributed\": true\n}")
                }
            }
        };
    }

    private static List<SignalRHubDoc> BuildHubs()
    {
        return new List<SignalRHubDoc>
        {
            new SignalRHubDoc
            {
                HubName = "TimerHub",
                Path = "/hubs/timer",
                Description = "Kişisel odak sayacı olayları. Bağlantı JWT ile yapılır ve kullanıcı kendi grubuna otomatik eklenir; istemcinin çağırabileceği hub metodu yoktur, olaylar REST seans uçlarının çağrılmasıyla yayınlanır.",
                ClientEvents = new List<HubEventDoc>
                {
                    new HubEventDoc { EventName = "TimerStarted", Description = "Seans başlatıldığında yayınlanır.", TypeScriptPayload = "FocusSessionDto" },
                    new HubEventDoc { EventName = "TimerPaused", Description = "Seans duraklatıldığında yayınlanır.", TypeScriptPayload = "FocusSessionDto" },
                    new HubEventDoc { EventName = "TimerResumed", Description = "Seans sürdürüldüğünde yayınlanır.", TypeScriptPayload = "FocusSessionDto" },
                    new HubEventDoc { EventName = "TimerExtended", Description = "Seans uzatıldığında yayınlanır.", TypeScriptPayload = "FocusSessionDto" },
                    new HubEventDoc { EventName = "TimerCompleted", Description = "Seans tamamlandığında ödül sonucuyla yayınlanır.", TypeScriptPayload = "interface SessionCompletionResultDto {\n  session: FocusSessionDto;\n  xpEarned: number;\n  coinsEarned: number;\n  newLevel: number;\n  totalXp: number;\n  totalCoins: number;\n  levelUp: boolean;\n  nextKind: string;\n  nextRound: number;\n  isCycleFinished: boolean;\n}" },
                    new HubEventDoc { EventName = "TimerAbandoned", Description = "Seans bırakıldığında yayınlanır.", TypeScriptPayload = "FocusSessionDto" },
                    new HubEventDoc { EventName = "DeskLightToggled", Description = "Masa lambası açıldığında veya kapandığında yayınlanır.", TypeScriptPayload = "boolean // true: lamba yanıyor" }
                },
                ServerMethods = new List<HubMethodDoc>()
            },
            new SignalRHubDoc
            {
                HubName = "RoomHub",
                Path = "/hubs/room",
                Description = "Ortak çalışma odası canlı olayları. Önce JoinRoomGroup ile oda grubuna abone olunur (yalnızca üyeler).",
                ClientEvents = new List<HubEventDoc>
                {
                    new HubEventDoc { EventName = "UserJoinedRoom", Description = "Odaya yeni bir üye katıldığında yayınlanır.", TypeScriptPayload = "RoomMemberDto" },
                    new HubEventDoc { EventName = "UserLeftRoom", Description = "Bir üye odadan ayrıldığında yayınlanır.", TypeScriptPayload = "string // userId" },
                    new HubEventDoc { EventName = "SeatOccupied", Description = "Bir masa doldurulduğunda yayınlanır.", TypeScriptPayload = "(userId: string, seatIndex: number)" },
                    new HubEventDoc { EventName = "SeatVacated", Description = "Bir masa boşaldığında yayınlanır.", TypeScriptPayload = "(userId: string, seatIndex: number)" },
                    new HubEventDoc { EventName = "DeskLightToggled", Description = "Bir üyenin masa lambası değiştiğinde yayınlanır.", TypeScriptPayload = "(userId: string, isLightOn: boolean)" },
                    new HubEventDoc { EventName = "ReactionBroadcasted", Description = "Bir üye tepki gönderdiğinde yayınlanır.", TypeScriptPayload = "interface RoomReactionDto {\n  roomId: string;\n  userId: string;\n  displayName: string;\n  reaction: string;\n  sentAt: string;\n}" },
                    new HubEventDoc { EventName = "CoWorkingBonusUpdated", Description = "Ortak çalışma bonusu değiştiğinde yayınlanır.", TypeScriptPayload = "number // bonus yüzdesi" }
                },
                ServerMethods = new List<HubMethodDoc>
                {
                    new HubMethodDoc { MethodName = "JoinRoomGroup", Description = "Oda grubuna abone olur; üye değilseniz HubException fırlatır.", TypeScriptParameters = "hubConn.invoke('JoinRoomGroup', roomCode: string)" },
                    new HubMethodDoc { MethodName = "Heartbeat", Description = "Üyeliği canlı tutar; uzun süre çağrılmazsa üye temizlenir.", TypeScriptParameters = "hubConn.invoke('Heartbeat', roomCode: string)" },
                    new HubMethodDoc { MethodName = "LeaveRoomGroup", Description = "Oda grubu aboneliğini bırakır.", TypeScriptParameters = "hubConn.invoke('LeaveRoomGroup', roomCode: string)" },
                    new HubMethodDoc { MethodName = "BroadcastReaction", Description = "Odaya tepki yayınlar.", TypeScriptParameters = "hubConn.invoke('BroadcastReaction', roomCode: string, reaction: string)" }
                }
            }
        };
    }
}

public class ApiCategoryDoc
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ApiEndpointDoc> Endpoints { get; set; } = new();
}

public class ApiEndpointDoc
{
    public string Method { get; set; } = "GET";
    public string Path { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequiresAuth { get; set; } = true;
    public string? TestLabTab { get; set; }
    public string? TypeScriptRequest { get; set; }
    public string? JsonRequestBody { get; set; }
    public string? JsonSuccessResponse { get; set; }
}

public class SignalRHubDoc
{
    public string HubName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<HubEventDoc> ClientEvents { get; set; } = new();
    public List<HubMethodDoc> ServerMethods { get; set; } = new();
}

public class HubEventDoc
{
    public string EventName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TypeScriptPayload { get; set; } = string.Empty;
}

public class HubMethodDoc
{
    public string MethodName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TypeScriptParameters { get; set; } = string.Empty;
}
