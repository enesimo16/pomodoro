# Proje Mimarisi ve Sistem Tasarımı (docs/proje-sistemi.md)

> C# .NET 9 Clean Architecture yapısı, piksel ev ve avatar modülleri, Redis anahtar tasarımı, SignalR Hub'ları ve Docker altyapısı.

---

## 1. Dizin ve Çözüm Mimarisi (Clean Architecture)

```
.
├── backend/
│   ├── Focus.sln
│   ├── Directory.Build.props          # Ortak derleme kuralları (Nullable, TreatWarningsAsErrors)
│   ├── global.json                    # .NET SDK sürüm sabitleme
│   ├── src/
│   │   ├── Focus.Domain/              # Sıfır Bağımlılıklı Çekirdek
│   │   │   ├── Common/                # Entity, AggregateRoot, IDomainEvent, DomainException
│   │   │   ├── Users/                 # User, UserPreferences, Streak
│   │   │   ├── Avatars/               # UserAvatar, HairStyle, ClothesSlot
│   │   │   ├── Rooms/                 # PixelRoom, RoomItem, RoomVisitor
│   │   │   ├── Catalog/               # ItemCatalog, UserInventory, ItemCategory
│   │   │   ├── Economy/               # CoinLedgerEntry, TransactionReason
│   │   │   ├── Sessions/              # FocusSession, SessionEvent, SessionStatus
│   │   │   └── Content/               # Theme, SoundTrack, SoundMixPreset, AssetLicense
│   │   │
│   │   ├── Focus.Application/         # İş Kuralları & CQRS (MediatR)
│   │   │   ├── Avatars/               # GetAvatarQuery, UpdateAvatarCommand
│   │   │   ├── Rooms/                 # GetRoomQuery, PlaceItemCommand, MoveItemCommand
│   │   │   ├── Shop/                  # GetCatalogQuery, BuyItemCommand
│   │   │   ├── Sessions/              # StartSessionCommand, CompleteSessionCommand
│   │   │   ├── Common/                # IAppDbContext, ICurrentUser, IClock, ICacheService
│   │   │   └── Behaviors/             # ValidationBehavior, LoggingBehavior
│   │   │
│   │   ├── Focus.Infrastructure/      # EF Core, Redis, Semantic Kernel, Dış Servisler
│   │   │   ├── Persistence/           # AppDbContext, Configurations/, Migrations/
│   │   │   ├── Identity/              # TokenService, GoogleAuthClient
│   │   │   ├── Caching/               # RedisCacheService
│   │   │   └── ExternalServices/      # GeminiChatClient, OpenMeteoClient
│   │   │
│   │   ├── Focus.WebAPI/              # RESTful Minimal API & SignalR Hub'ları
│   │   │   ├── Endpoints/             # AvatarEndpoints, RoomEndpoints, ShopEndpoints, SessionEndpoints
│   │   │   ├── Hubs/                  # TimerHub, RoomHub
│   │   │   └── Program.cs
│   │   │
│   │   └── Focus.Admin/               # Razor Pages Yönetim Paneli
│   │       ├── Pages/
│   │       │   ├── Catalog/           # Mağazaya piksel mobilya ve kıyafet ekleme
│   │       │   ├── Content/           # Ses ve tema yükleme
│   │       │   └── Users/             # Kullanıcı ve ekonomi izleme
│   │       └── Program.cs
│   │
│   └── tests/
│       ├── Focus.UnitTests/           # Domain & Application birim testleri
│       └── Focus.IntegrationTests/    # API & Altyapı entegrasyon testleri (Testcontainers)
│
├── frontend/                          # Next.js (TypeScript, Tailwind, Canvas / Web Audio API)
├── docs/                              # Modüler mimari dokümanları
└── docker-compose.yml                 # PostgreSQL (pgvector), Redis, API, Admin
```

---

## 2. Gerçek Zamanlı İletişim: SignalR Mimarisi

1. **`TimerHub` (`/hubs/timer`):**  
   Kullanıcının masaya oturması, masa lambasının açılması, seansın geri sayımı ve seans bitiş kutlamaları.
2. **`RoomHub` (`/hubs/room`):**  
   Habbo tarzı canlı oda etkileşimleri; arkadaşın odaya girişi, boş bir masaya oturması, çalışma durumunun güncellenmesi ve mobilya yerleşimi senkronu.

---

## 3. Redis Anahtar Tasarımı

| Anahtar Formatı | Tür | TTL | Kullanım Amacı |
| :--- | :--- | :--- | :--- |
| `session:active:{userId}` | Hash | Oturum sonu + 1 sa | Aktif çalışma seansı ve masa durumu |
| `room:{roomId}:visitors` | Set | 24 saat | Odada o an bulunan canlı avatar listesi |
| `lock:session:{userId}` | String | 5 sn | Eşzamanlı seans başlatmayı engelleyen dağıtık kilit |
| `user:{userId}:avatar` | String (JSON)| 1 saat | Sık okunan avatar görsel katmanları önbelleği |
| `catalog:active` | String (JSON)| 1 saat | Mağaza kataloğu önbelleği |
| `weather:{cityKey}` | String | 30 dk | Open-Meteo hava durumu sonucu (pencere senkronu) |

---

## 4. Docker Konfigürasyonu (docker-compose.yml)

- **PostgreSQL (`pgvector:pg16`):** Port 5432, sağlık kontrolü `pg_isready`.
- **Redis (`redis:7-alpine`):** Port 6379, parola korumalı, AOF veri kalıcılığı açık.
- **focus-api:** .NET 9 Web API (Konteyner içi port 8080, dış port 5000).
- **focus-admin:** Razor Pages yönetim paneli (Konteyner içi port 8080, dış port 5001 - localhost korumalı).
