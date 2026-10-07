using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace Focus.Admin.Pages.ApiDocs;

public class IndexModel : PageModel
{
    private readonly IConfiguration _configuration;

    public string WebApiBaseUrl { get; set; } = "http://localhost:5000";
    public List<string> AdminKeys { get; set; } = new();
    public List<ApiCategoryDoc> Categories { get; set; } = new();
    public List<SignalRHubDoc> Hubs { get; set; } = new();

    public IndexModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void OnGet()
    {
        WebApiBaseUrl = _configuration.GetValue<string>("WebApiBaseUrl") ?? "http://localhost:5000";
        AdminKeys = _configuration.GetSection("AdminSecurity:Keys").Get<List<string>>() ?? new List<string>
        {
            "focus_master_key_9971x",
            "focus_admin_sec_2026",
            "adm_key_enes_dev_44"
        };
        Categories = BuildCategories();
        Hubs = BuildHubs();
    }

    private static List<ApiCategoryDoc> BuildCategories()
    {
        return new List<ApiCategoryDoc>
        {
            new ApiCategoryDoc
            {
                Id = "auth",
                Title = "1. Kimlik, Token & Profil (Auth)",
                Description = "Misafir hesabı açma, JWT Token yenileme ve oturum yönetimi.",
                Endpoints = new List<ApiEndpointDoc>
                {
                    new ApiEndpointDoc
                    {
                        Method = "POST",
                        Path = "/api/v1/auth/guest",
                        Title = "Misafir Hesabı Aç",
                        Description = "Kayıt formu doldurmadan tek tıkla misafir piksel karakter ve başlangıç odası oluşturur.",
                        RequiresAuth = false,
                        TestLabTab = "#tab-auth",
                        TypeScriptRequest = "interface CreateGuestRequest {\n  displayName?: string; // İsteğe bağlı rumuz (Örn: 'Misafir Odakçı')\n}",
                        JsonRequestBody = "{\n  \"displayName\": \"Misafir Odakçı\"\n}",
                        JsonSuccessResponse = "{\n  \"success\": true,\n  \"data\": {\n    \"userId\": \"7a1d3f92-8021-4b11-a887-32145b9812af\",\n    \"username\": \"guest_7a1d3f\",\n    \"accessToken\": \"eyJhbGciOiJIUzI1NiIs...\",\n    \"refreshToken\": \"rt_99a81c72...\",\n    \"expiresAt\": \"2026-10-07T12:00:00Z\"\n  }\n}"
                    },
                    new ApiEndpointDoc
                    {
                        Method = "GET",
                        Path = "/api/v1/auth/me",
                        Title = "Profil Bilgisi Getir",
                        Description = "Oturum açmış kullanıcının kimlik, seviye, coin ve profil detaylarını döner.",
                        RequiresAuth = true,
                        TestLabTab = "#tab-auth",
                        TypeScriptRequest = "// Gövde gerekmez (Header: Authorization: Bearer <token>)",
                        JsonRequestBody = null,
                        JsonSuccessResponse = "{\n  \"success\": true,\n  \"data\": {\n    \"userId\": \"7a1d3f92-8021-4b11-a887-32145b9812af\",\n    \"username\": \"guest_7a1d3f\",\n    \"level\": 1,\n    \"currentXp\": 120,\n    \"focusCoins\": 50,\n    \"createdAt\": \"2026-10-06T10:00:00Z\"\n  }\n}"
                    },
                    new ApiEndpointDoc
                    {
                        Method = "POST",
                        Path = "/api/v1/auth/refresh",
                        Title = "Token Yenile (Refresh)",
                        Description = "Süresi dolan Access Token'ı Refresh Token kullanarak güvenle yeniler.",
                        RequiresAuth = false,
                        TestLabTab = "#tab-auth",
                        TypeScriptRequest = "interface RefreshTokenRequest {\n  refreshToken: string;\n}",
                        JsonRequestBody = "{\n  \"refreshToken\": \"rt_99a81c72...\"\n}",
                        JsonSuccessResponse = "{\n  \"success\": true,\n  \"data\": {\n    \"accessToken\": \"eyJhbGciOiJIUzI1NiIs...\",\n    \"refreshToken\": \"rt_new_8812...\",\n    \"expiresAt\": \"2026-10-07T13:00:00Z\"\n  }\n}"
                    },
                    new ApiEndpointDoc
                    {
                        Method = "POST",
                        Path = "/api/v1/auth/logout",
                        Title = "Güvenli Çıkış Yap",
                        Description = "Mevcut refresh token'ı geçersiz kılarak oturumu sonlandırır.",
                        RequiresAuth = true,
                        TestLabTab = "#tab-auth",
                        TypeScriptRequest = "// Gövde gerekmez (Header: Authorization: Bearer <token>)",
                        JsonRequestBody = null,
                        JsonSuccessResponse = "{\n  \"success\": true,\n  \"message\": \"Oturum başarıyla sonlandırıldı.\"\n}"
                    }
                }
            },
            new ApiCategoryDoc
            {
                Id = "avatar",
                Title = "2. Piksel Karakter & Gardırop (Avatar)",
                Description = "Karakter ten rengi, saç stili, kıyafet kuşanma ve gardırop yönetimi.",
                Endpoints = new List<ApiEndpointDoc>
                {
                    new ApiEndpointDoc
                    {
                        Method = "GET",
                        Path = "/api/v1/avatar",
                        Title = "Aktif Avatarı Getir",
                        Description = "Kullanıcının ten rengi, saç rengi ve şu an üzerinde takılı kıyafetleri döner.",
                        RequiresAuth = true,
                        TestLabTab = "#tab-avatar",
                        TypeScriptRequest = "// Gövde gerekmez",
                        JsonRequestBody = null,
                        JsonSuccessResponse = "{\n  \"success\": true,\n  \"data\": {\n    \"skinTone\": \"#ffd2b2\",\n    \"hairStyle\": \"retro_messy\",\n    \"hairColor\": \"#2c1b18\",\n    \"equippedItems\": [\n      { \"slot\": \"Hat\", \"itemId\": \"item_cap_01\", \"name\": \"Piksel Şapka\" },\n      { \"slot\": \"Top\", \"itemId\": \"item_hoodie_01\", \"name\": \"Siyah Hoodie\" }\n    ]\n  }\n}"
                    },
                    new ApiEndpointDoc
                    {
                        Method = "PUT",
                        Path = "/api/v1/avatar",
                        Title = "Avatar Görünümünü Güncelle",
                        Description = "Temel piksel avatar özelliklerini (ten rengi, saç stili, saç rengi) günceller.",
                        RequiresAuth = true,
                        TestLabTab = "#tab-avatar",
                        TypeScriptRequest = "interface UpdateAvatarRequest {\n  skinTone: string;\n  hairStyle: string;\n  hairColor: string;\n}",
                        JsonRequestBody = "{\n  \"skinTone\": \"#f5c59f\",\n  \"hairStyle\": \"curly_fade\",\n  \"hairColor\": \"#1a1a1a\"\n}",
                        JsonSuccessResponse = "{\n  \"success\": true,\n  \"message\": \"Avatar görünümü güncellendi.\"\n}"
                    },
                    new ApiEndpointDoc
                    {
                        Method = "POST",
                        Path = "/api/v1/avatar/wear",
                        Title = "Eşya / Kıyafet Kuşan",
                        Description = "Envanterdeki bir eşyayı ilgili yuvaya (Hat, Top, Bottom, Shoes, Accessory) takar.",
                        RequiresAuth = true,
                        TestLabTab = "#tab-avatar",
                        TypeScriptRequest = "interface WearItemRequest {\n  itemId: string;\n  slot: 'Hat' | 'Top' | 'Bottom' | 'Shoes' | 'Accessory';\n}",
                        JsonRequestBody = "{\n  \"itemId\": \"item_hoodie_01\",\n  \"slot\": \"Top\"\n}",
                        JsonSuccessResponse = "{\n  \"success\": true,\n  \"message\": \"Eşya başarıyla kuşandı.\"\n}"
                    },
                    new ApiEndpointDoc
                    {
                        Method = "POST",
                        Path = "/api/v1/avatar/unwear",
                        Title = "Eşyayı Çıkar",
                        Description = "İlgili yuvadaki eşyayı çıkarıp gardıroba geri gönderir.",
                        RequiresAuth = true,
                        TestLabTab = "#tab-avatar",
                        TypeScriptRequest = "interface UnwearItemRequest {\n  slot: 'Hat' | 'Top' | 'Bottom' | 'Shoes' | 'Accessory';\n}",
                        JsonRequestBody = "{\n  \"slot\": \"Hat\"\n}",
                        JsonSuccessResponse = "{\n  \"success\": true,\n  \"message\": \"Eşya çıkarıldı.\"\n}"
                    }
                }
            },
            new ApiCategoryDoc
            {
                Id = "room",
                Title = "3. İzometrik Piksel Oda & Dekorasyon (Room)",
                Description = "Kişisel çalışma odası, duvar/zemin kaplamaları ve mobilya yerleşimi.",
                Endpoints = new List<ApiEndpointDoc>
                {
                    new ApiEndpointDoc
                    {
                        Method = "GET",
                        Path = "/api/v1/room",
                        Title = "Oda ve Mobilyaları Getir",
                        Description = "Kullanıcının odasını, ızgara boyutunu (grid), duvar/zemin temasını ve mobilyaları döner.",
                        RequiresAuth = true,
                        TestLabTab = "#tab-room",
                        TypeScriptRequest = "// Gövde gerekmez",
                        JsonRequestBody = null,
                        JsonSuccessResponse = "{\n  \"success\": true,\n  \"data\": {\n    \"roomId\": \"room_001\",\n    \"themeName\": \"Gece & Yağmur\",\n    \"gridWidth\": 12,\n    \"gridHeight\": 12,\n    \"placedItems\": [\n      { \"id\": \"pi_01\", \"itemId\": \"desk_vintage\", \"gridX\": 4, \"gridY\": 5, \"rotation\": 0 },\n      { \"id\": \"pi_02\", \"itemId\": \"lamp_neon\", \"gridX\": 4, \"gridY\": 6, \"rotation\": 90 }\n    ]\n  }\n}"
                    },
                    new ApiEndpointDoc
                    {
                        Method = "POST",
                        Path = "/api/v1/room/place-item",
                        Title = "Mobilya Yerleştir",
                        Description = "Envanterdeki bir eşyayı izometrik ızgarada (x, y) koordinatlarına yerleştirir.",
                        RequiresAuth = true,
                        TestLabTab = "#tab-room",
                        TypeScriptRequest = "interface PlaceItemRequest {\n  catalogItemId: string;\n  gridX: number;\n  gridY: number;\n  rotation: number; // 0, 90, 180, 270\n}",
                        JsonRequestBody = "{\n  \"catalogItemId\": \"desk_vintage\",\n  \"gridX\": 4,\n  \"gridY\": 5,\n  \"rotation\": 0\n}",
                        JsonSuccessResponse = "{\n  \"success\": true,\n  \"data\": { \"placedItemId\": \"pi_01\", \"status\": \"Placed\" }\n}"
                    },
                    new ApiEndpointDoc
                    {
                        Method = "POST",
                        Path = "/api/v1/room/remove-item",
                        Title = "Mobilyayı Kaldır",
                        Description = "Odadan mobilyayı kaldırarak kullanıcının envanterine geri aktarır.",
                        RequiresAuth = true,
                        TestLabTab = "#tab-room",
                        TypeScriptRequest = "interface RemoveItemRequest {\n  placedItemId: string;\n}",
                        JsonRequestBody = "{\n  \"placedItemId\": \"pi_01\"\n}",
                        JsonSuccessResponse = "{\n  \"success\": true,\n  \"message\": \"Mobilya envantere kaldırıldı.\"\n}"
                    }
                }
            },
            new ApiCategoryDoc
            {
                Id = "session",
                Title = "4. Odak Sayacı & Flow Shield (Session)",
                Description = "Sunucu otoriteli odak seansı başlatma, Flow Shield kalkanı ve tamamlama.",
                Endpoints = new List<ApiEndpointDoc>
                {
                    new ApiEndpointDoc
                    {
                        Method = "POST",
                        Path = "/api/v1/session/start",
                        Title = "Odaklanma Seansı Başlat",
                        Description = "Masaya oturma ritüelini başlatır, sunucu tarafında sayaç ve kalkan başlatır.",
                        RequiresAuth = true,
                        TestLabTab = "#tab-session",
                        TypeScriptRequest = "interface StartSessionRequest {\n  plannedDurationMinutes: number; // Örn: 25, 45, 60\n  category?: string; // Örn: 'Kodlama', 'Okuma'\n  themeId?: string;\n}",
                        JsonRequestBody = "{\n  \"plannedDurationMinutes\": 45,\n  \"category\": \"Yazılım Geliştirme\"\n}",
                        JsonSuccessResponse = "{\n  \"success\": true,\n  \"data\": {\n    \"sessionId\": \"sess_88192a\",\n    \"startedAt\": \"2026-10-06T10:00:00Z\",\n    \"plannedSeconds\": 2700,\n    \"flowShieldActive\": true\n  }\n}"
                    },
                    new ApiEndpointDoc
                    {
                        Method = "POST",
                        Path = "/api/v1/session/complete",
                        Title = "Seansı Başarıyla Tamamla",
                        Description = "Süresi dolan seansı onaylar; hile kontrolünden sonra XP ve Focus Coin ödüllendirir.",
                        RequiresAuth = true,
                        TestLabTab = "#tab-session",
                        TypeScriptRequest = "interface CompleteSessionRequest {\n  sessionId: string;\n}",
                        JsonRequestBody = "{\n  \"sessionId\": \"sess_88192a\"\n}",
                        JsonSuccessResponse = "{\n  \"success\": true,\n  \"data\": {\n    \"earnedXp\": 180,\n    \"earnedCoins\": 45,\n    \"newLevel\": 2,\n    \"streakBonus\": 15\n  }\n}"
                    },
                    new ApiEndpointDoc
                    {
                        Method = "POST",
                        Path = "/api/v1/session/cancel",
                        Title = "Seansı İptal Et / Terk Et",
                        Description = "Kullanıcı masadan erken kalktığında seansı iptal eder (ödül verilmez).",
                        RequiresAuth = true,
                        TestLabTab = "#tab-session",
                        TypeScriptRequest = "interface CancelSessionRequest {\n  sessionId: string;\n  reason?: string;\n}",
                        JsonRequestBody = "{\n  \"sessionId\": \"sess_88192a\",\n  \"reason\": \"Acil mola\"\n}",
                        JsonSuccessResponse = "{\n  \"success\": true,\n  \"message\": \"Seans iptal edildi.\"\n}"
                    },
                    new ApiEndpointDoc
                    {
                        Method = "POST",
                        Path = "/api/v1/session/shield/break",
                        Title = "Flow Shield Kırıldı Bildirimi",
                        Description = "Kullanıcı başka sekmeye geçtiğinde veya dikkat dağıtıcı uygulama açtığında kalkan kırılır.",
                        RequiresAuth = true,
                        TestLabTab = "#tab-session",
                        TypeScriptRequest = "interface ShieldBreakRequest {\n  sessionId: string;\n  distractionSource?: string;\n}",
                        JsonRequestBody = "{\n  \"sessionId\": \"sess_88192a\",\n  \"distractionSource\": \"Browser Tab Changed\"\n}",
                        JsonSuccessResponse = "{\n  \"success\": true,\n  \"data\": { \"shieldBroken\": true, \"graceSecondsLeft\": 180 }\n}"
                    }
                }
            },
            new ApiCategoryDoc
            {
                Id = "media",
                Title = "5. Medya, Canlı Video & Ambiyans (Media)",
                Description = "Pixabay lisanslı lofi arkaplan videoları, Web Audio gürültü sentezi ve Open-Meteo hava durumu.",
                Endpoints = new List<ApiEndpointDoc>
                {
                    new ApiEndpointDoc
                    {
                        Method = "GET",
                        Path = "/api/v1/media/pixabay/videos",
                        Title = "Pixabay Lo-Fi Arkaplan Videoları",
                        Description = "Oda penceresi için telifsiz, estetik piksel/lofi döngüsel video akışlarını getirir.",
                        RequiresAuth = true,
                        TestLabTab = "#tab-media",
                        TypeScriptRequest = "// Query params: ?query=lofi+room&perPage=5",
                        JsonRequestBody = null,
                        JsonSuccessResponse = "{\n  \"success\": true,\n  \"data\": [\n    {\n      \"id\": 1204,\n      \"title\": \"Rainy Cozy Window\",\n      \"videoUrl\": \"https://cdn.pixabay.com/video/rain_cozy_720p.mp4\",\n      \"duration\": 30\n    }\n  ]\n}"
                    },
                    new ApiEndpointDoc
                    {
                        Method = "GET",
                        Path = "/api/v1/media/weather/sync",
                        Title = "Open-Meteo Hava Durumu Senkronu",
                        Description = "Kullanıcının koordinatlarına göre gerçek zamanlı hava durumunu oda penceresine bağlar.",
                        RequiresAuth = true,
                        TestLabTab = "#tab-media",
                        TypeScriptRequest = "// Query params: ?latitude=41.0082&longitude=28.9784",
                        JsonRequestBody = null,
                        JsonSuccessResponse = "{\n  \"success\": true,\n  \"data\": {\n    \"temperature\": 18.5,\n    \"condition\": \"Rainy\",\n    \"isNight\": true,\n    \"windowRainDrops\": true\n  }\n}"
                    }
                }
            },
            new ApiCategoryDoc
            {
                Id = "shop",
                Title = "6. Mağaza, Katalog & Ekonomi (Shop)",
                Description = "Focus Coin ile mobilya ve kostüm satın alma, envanter listeleme.",
                Endpoints = new List<ApiEndpointDoc>
                {
                    new ApiEndpointDoc
                    {
                        Method = "GET",
                        Path = "/api/v1/shop/catalog",
                        Title = "Mağaza Kataloğu Getir",
                        Description = "Satın alınabilir mobilyalar, kostümler ve oda temalarını listeler.",
                        RequiresAuth = true,
                        TestLabTab = "#tab-shop",
                        TypeScriptRequest = "// Query: ?category=Furniture (opsiyonel)",
                        JsonRequestBody = null,
                        JsonSuccessResponse = "{\n  \"success\": true,\n  \"data\": [\n    {\n      \"id\": \"item_neon_coffee\",\n      \"name\": \"Neon Kahve Tabelası\",\n      \"price\": 120,\n      \"category\": \"Furniture\",\n      \"tier\": \"Rare\"\n    }\n  ]\n}"
                    },
                    new ApiEndpointDoc
                    {
                        Method = "POST",
                        Path = "/api/v1/shop/purchase",
                        Title = "Eşya Satın Al",
                        Description = "Coin bakiyesini kontrol eder, düşer ve eşyayı kullanıcının envanterine ekler.",
                        RequiresAuth = true,
                        TestLabTab = "#tab-shop",
                        TypeScriptRequest = "interface PurchaseItemRequest {\n  catalogItemId: string;\n}",
                        JsonRequestBody = "{\n  \"catalogItemId\": \"item_neon_coffee\"\n}",
                        JsonSuccessResponse = "{\n  \"success\": true,\n  \"data\": {\n    \"remainingCoins\": 380,\n    \"purchasedItemId\": \"item_neon_coffee\"\n  }\n}"
                    },
                    new ApiEndpointDoc
                    {
                        Method = "GET",
                        Path = "/api/v1/shop/inventory",
                        Title = "Kullanıcı Envanteri",
                        Description = "Kullanıcının sahip olduğu tüm eşyaları ve giyilme/yerleştirilme durumlarını döner.",
                        RequiresAuth = true,
                        TestLabTab = "#tab-shop",
                        TypeScriptRequest = "// Gövde gerekmez",
                        JsonRequestBody = null,
                        JsonSuccessResponse = "{\n  \"success\": true,\n  \"data\": [\n    { \"inventoryId\": \"inv_11\", \"catalogItemId\": \"item_neon_coffee\", \"isPlaced\": false }\n  ]\n}"
                    }
                }
            },
            new ApiCategoryDoc
            {
                Id = "coach",
                Title = "7. Masaüstü Yapay Zeka Koç (AI Coach)",
                Description = "Masaüstü terminal / radyo tarzı yapay zeka koçu, pgvector hafızası ve motivasyon.",
                Endpoints = new List<ApiEndpointDoc>
                {
                    new ApiEndpointDoc
                    {
                        Method = "POST",
                        Path = "/api/v1/coach/chat",
                        Title = "AI Koç ile Sohbet Et",
                        Description = "Önceki odak alışkanlıklarını hatırlayan yapay zeka asistanına soru sorar.",
                        RequiresAuth = true,
                        TestLabTab = "#tab-coach",
                        TypeScriptRequest = "interface CoachChatRequest {\n  message: string;\n}",
                        JsonRequestBody = "{\n  \"message\": \"Bugün 45 dakika kodlama yapacağım, bana ilham ver.\"\n}",
                        JsonSuccessResponse = "{\n  \"success\": true,\n  \"data\": {\n    \"reply\": \"Harika! Dünkü 2 saatlik seansındaki ritmini hatırla. Masa lambanı yaktım, derin odaklanma başlasın.\",\n    \"tone\": \"Encouraging\"\n  }\n}"
                    },
                    new ApiEndpointDoc
                    {
                        Method = "GET",
                        Path = "/api/v1/coach/memories",
                        Title = "Kullanıcı Vektörel Hafızası",
                        Description = "Yapay zekanın kullanıcı hakkında pgvector veritabanında tuttuğu özetleri listeler.",
                        RequiresAuth = true,
                        TestLabTab = "#tab-coach",
                        TypeScriptRequest = "// Gövde gerekmez",
                        JsonRequestBody = null,
                        JsonSuccessResponse = "{\n  \"success\": true,\n  \"data\": [\n    { \"topic\": \"Çalışma Tercihi\", \"summary\": \"Gece saatlerinde yağmur ambiyansıyla kod yazmayı tercih ediyor.\" }\n  ]\n}"
                    }
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
                Description = "Kişisel odak sayacı, saniye tikleri (tick), kalkan ve masa lambası canlı olayları.",
                ClientEvents = new List<HubEventDoc>
                {
                    new HubEventDoc
                    {
                        EventName = "ReceiveTick",
                        Description = "Her saniye sunucudan istemciye akan güncel kalan süre bilgisi.",
                        TypeScriptPayload = "interface TickPayload {\n  remainingSeconds: number;\n  elapsedSeconds: number;\n}"
                    },
                    new HubEventDoc
                    {
                        EventName = "ReceiveStateChanged",
                        Description = "Seans durumu değiştiğinde (Running, Paused, FlowShieldBroken) tetiklenir.",
                        TypeScriptPayload = "interface StatePayload {\n  status: 'Running' | 'Paused' | 'ShieldBroken' | 'Completed';\n}"
                    },
                    new HubEventDoc
                    {
                        EventName = "ReceiveCompleted",
                        Description = "Seans bittiğinde tebrik ve ödül verilerini yayınlar.",
                        TypeScriptPayload = "interface CompletedPayload {\n  xpEarned: number;\n  coinsEarned: number;\n  isLevelUp: boolean;\n}"
                    }
                },
                ServerMethods = new List<HubMethodDoc>
                {
                    new HubMethodDoc
                    {
                        MethodName = "StartTimer",
                        Description = "Sayacı sunucu tarafında başlatır.",
                        TypeScriptParameters = "hubConn.invoke('StartTimer', { sessionId: string })"
                    },
                    new HubMethodDoc
                    {
                        MethodName = "PauseTimer",
                        Description = "Sayacı duraklatır.",
                        TypeScriptParameters = "hubConn.invoke('PauseTimer')"
                    }
                }
            },
            new SignalRHubDoc
            {
                HubName = "RoomHub",
                Path = "/hubs/room",
                Description = "Habbo tarzı ortak çalışma odası; çoklu kullanıcı avatarları, hareket ve tepki emojileri.",
                ClientEvents = new List<HubEventDoc>
                {
                    new HubEventDoc
                    {
                        EventName = "UserJoined",
                        Description = "Odaya yeni bir avatar girdiğinde diğer kullanıcılara duyurulur.",
                        TypeScriptPayload = "interface UserJoinedPayload {\n  userId: string;\n  username: string;\n  avatarConfig: object;\n  gridX: number;\n  gridY: number;\n}"
                    },
                    new HubEventDoc
                    {
                        EventName = "UserMoved",
                        Description = "Bir avatar odadaki başka bir masaya veya kareye yürüdüğünde tetiklenir.",
                        TypeScriptPayload = "interface UserMovedPayload {\n  userId: string;\n  targetX: number;\n  targetY: number;\n}"
                    },
                    new HubEventDoc
                    {
                        EventName = "ReceiveChatMessage",
                        Description = "Odadaki kullanıcılar sessiz fısıltı veya emoji mesajı gönderdiğinde gelir.",
                        TypeScriptPayload = "interface ChatPayload {\n  senderId: string;\n  message: string;\n  sentAt: string;\n}"
                    }
                },
                ServerMethods = new List<HubMethodDoc>
                {
                    new HubMethodDoc
                    {
                        MethodName = "JoinRoom",
                        Description = "Odaya katılır ve canlı SignalR grubuna abone olur.",
                        TypeScriptParameters = "hubConn.invoke('JoinRoom', roomCode: string)"
                    },
                    new HubMethodDoc
                    {
                        MethodName = "MoveAvatar",
                        Description = "Avatarın odada yeni bir koordinata yürümesini bildirir.",
                        TypeScriptParameters = "hubConn.invoke('MoveAvatar', { x: number, y: number })"
                    }
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
