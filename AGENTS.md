# ANTIGRAVITY KILAVUZU & PROJE ANA REHBERİ (AGENTS.md)

Bu doküman, yapay zekâ asistanı (Antigravity) için projenin mutlak anayasasıdır. Projeye dair tüm kararlar, standartlar, kodlama kuralları ve modül yönlendirmeleri burada tanımlanmıştır.

---

## 1. Proje Özeti ve Temel Amaç

Bu proje; sıradan ve sıkıcı 25/5 kronometrelerin ötesine geçen; **Habbo tarzı izometrik piksel ev/oda (Pixel Room) ve özelleştirilebilir piksel karakter (Avatar)** yaşam döngüsüne sahip, çok kanallı atmosferik ses mikseri, pencere hava durumu senkronu, seviye (XP) ve mobilya/kıyafet ekonomisi ile donatılmış yeni nesil bir derin odaklanma ve ortak çalışma (co-working) ekosistemidir.

### Temel Konsept & Oyunlaştırma:
- **Piksel Ev & Çalışma Alanı:** Kullanıcının kendine ait retro/izometrik piksel bir odası (çalışma masası, koltuğu, yatağı, kitaplığı, plak çaları, penceresi) bulunur.
- **Odaklanma Ritüeli:** Karakter masaya oturur, masa lambası yanar; seçilen müzik, ambiyans ve süreyle odaklanma seansı başlar.
- **Piksel Karakter (Avatar):** Saç, kıyafet, ceket, şapka ve aksesuarları özelleştirilebilir retro piksel karakter.
- **Seviye ve Ekonomi:** Odaklandıkça XP kazanılır, seviye atlanır. Kazanılan odak paraları (Focus Coins) ile kıyafet mağazasından yeni kostümler, dekorasyon mağazasından odaya mobilyalar (neon tabelalar, bitkiler, kahve makinesi) alınır.
- **Sosyal Odaklanma (Habbo Tarzı Ortak Çalışma):** Arkadaşlar birbirinin odasını ziyaret edebilir, aynı odada farklı masalara oturup sessizce birlikte çalışabilirler.

### Temel Teknoloji Yığını:
- **Backend:** C# / .NET 9 (Clean Architecture)
- **Admin Paneli:** ASP.NET Core Razor Pages
- **Frontend:** Next.js (TypeScript, Tailwind CSS, Canvas / Web Audio API) — Harici ekip arkadaşı tarafından geliştirilmektedir.
- **Veritabanı:** PostgreSQL (`pgvector` eklentisi ile vektörel arama destekli)
- **Önbellek & Dağıtık Durum:** Redis
- **Gerçek Zamanlı İletişim:** SignalR (`TimerHub`, `RoomHub`)
- **Yapay Zekâ Orkestrasyonu:** Microsoft.Extensions.AI / Semantic Kernel
- **Konteynerizasyon:** Docker & Docker Compose

---

## 2. Mutlak Kodlama ve Mimari Kuralları

1. **İsimlendirme Standartları:** Çözüm, proje ve namespace adlandırmaları `Focus.*` standardında olacaktır (`Focus.Domain`, `Focus.Application`, `Focus.Infrastructure`, `Focus.WebAPI`, `Focus.Admin`).
2. **Metafor Yasağı:** Projede hayvan, evcil hayvan (pet), arı, kovan, bal gibi metaforlar kesinlikle kullanılmayacaktır. Tüm domain modelleri evrensel, piksel ev ve avatar konseptine uygun adlandırılacaktır (`FocusSession`, `UserAvatar`, `PixelRoom`, `RoomItem`, `CoinLedgerEntry`, `StudyRoom`).
3. **Katmanlı Mimari (Clean Architecture):**
   - Bağımlılıklar daima içe doğru akar: `Domain` <- `Application` <- `Infrastructure` / `WebAPI` / `Admin`.
   - Domain hiçbir dış kütüphaneye veya katmana bağımlı olamaz.
   - CQRS prensibi `MediatR` ile uygulanacaktır.
4. **Clean Code & Sadelik:**
   - Kod temiz, okunabilir ve modüler olacaktır.
   - Gereksiz soyutlamalar, kullanılmayan dosyalar, boş şablonlar ve aşırı mühendislik kesinlikle yasaktır.
5. **Hardcoded Değer Yasağı & Ortam Değişkenleri:**
   - Hiçbir bağlantı dizesi, API anahtarı veya gizli anahtar kod içine gömülmeyecektir.
   - Tüm sırlar `.env` dosyasında tutulacak ve `appsettings.json` / IOptions üzerinden okunacaktır.
6. **Tam İmplementasyon Seviyesi:**
   - Hiçbir iş mantığı sahte (mock / dummy / TODO) bırakılmayacaktır; tüm servisler çalışır ve eksiksiz seviyede yazılacaktır.
7. **Yorum Satırı Standardı:**
   - Yorum satırları kesinlikle minimal tutulacaktır.
   - Yorumlarda emoji, süsleme veya gereksiz semboller kullanılmayacaktır.
   - Yalnızca gerekli durumlarda standart `// mesaj` formatı kullanılacaktır.
8. **Test Zorunluluğu:**
   - Yazılan her özellik ve servis için `backend/tests/` klasöründe birebir aynı klasörleme yapısıyla Unit ve Entegrasyon testleri oluşturulacaktır (`Focus.UnitTests`, `Focus.IntegrationTests`).
9. **Docker Tabanlı Çalışma:**
   - Sistem yerel makineye özel bağımlılıklar kurmadan, tamamen `docker-compose` ile ayağa kalkacak şekilde tasarlanacaktır.

---

## 3. Konu Bazlı Dokümantasyon Yönlendirme Tablosu (Agent Routing Index)

| İlgili Konu / Görev | Bakılacak Doküman | Doküman İçeriği |
| :--- | :--- | :--- |
| **Genel Vizyon, Pazar & Rakipler** | `baslamadan-once.md` | Piksel ev vizyonu, Habbo tarzı sosyal çalışma, iş modeli ve faz planı. |
| **Ses Altyapısı, Mikser, Plak Çalar**| `docs/sesler.md` | 4 kanallı ses mikseri, Web Audio gürültü sentezi, oda içi piksel plak çalar. |
| **Görsel Temalar & Piksel Odalar** | `docs/temalar.md` | Piksel oda tasarımları, pencereden hava durumu akışı, pil tasarrufu. |
| **Yapay Zekâ (Oda İçi Asistan / Koç)**| `docs/agent-sistemi.md` | Masaüstü terminal/radyo asistanı, kural motoru, Redis + pgvector hafızası. |
| **Kimlik Doğrulama & Kullanıcı** | `docs/kullanici-sistemi.md` | Misafir modu, OAuth (Google/GitHub/Apple), HttpOnly çerez güvenliği. |
| **Proje Yapısı & Docker Mimarisi** | `docs/proje-sistemi.md` | Katmanlar, SignalR hub'ları (`TimerHub`, `RoomHub`) ve Docker konfigürasyonu. |
| **Algoritmalar & Seviye (XP) Sistemi**| `docs/algoritmalar.md` | Sunucu otoriteli sayaç, Flow Shield, XP eğrisi, Coin formülü ve hile önleme. |
| **Özellikler & Faz Kapsamı** | `docs/ozellikler.md` | Piksel avatar, oda özelleştirme, mağaza, MVP vs Faz 2 vs Pro matrisi. |
| **Kütüphaneler, Paketler & API'ler** | `docs/kutuphaneler-ve-apiler.md` | NuGet bağımlılıkları, test paketleri (Testcontainers), bulut servisleri. |
| **Harici API Entegrasyonları & Anahtarlar**| `docs/api.md` | Pixabay, Open-Meteo, Gemini API alımı, kotalar ve test edilmiş şablonlar. |
| **Veri Tabanı Şeması & Varlık Modeli** | `docs/veri-modeli.md` | Avatar, PixelRoom, RoomItem, Inventory, FocusSession ERD ve kuralları. |
| **Frontend/Backend Ortak API Sözleşmesi**| `docs/api-sozlesmesi.md` | REST uçları (`/avatar`, `/room`, `/shop`), SignalR RoomHub olayları. |
| **Riskler, Tuzaklar ve Öneriler** | `docs/riskler-ve-oneriler.md` | Lisans tuzakları, piksel varlık telifleri, ekonomi enflasyon riski ve önlemler. |
| **Ekstra Özellikler & İnovasyon Havuzu** | `docs/ekstra-ozellikler.md` | Piksel mini oyunlar, oda ziyaretçi defteri, DEHB kalkanı, Focus Wrapped. |

---

## 4. Dizin Yapısı Standardı

```
.
├── AGENTS.md                          # Bu dosya (Yapay zekâ ana kılavuzu)
├── README.md                          # Proje genel tanıtımı
├── baslamadan-once.md                 # Başlangıç ve vizyon rehberi
├── docker-compose.yml                 # Docker orkestrasyonu
├── .env / .env.example                # Ortam değişkenleri
├── docs/                              # Detaylı mimari ve modül rehberleri
├── backend/                           # C# .NET 9 Backend çözümü
│   ├── Focus.sln
│   ├── src/
│   │   ├── Focus.Domain/              # Çekirdek modeller ve kurallar
│   │   ├── Focus.Application/         # İş mantığı, CQRS, servis arayüzleri
│   │   ├── Focus.Infrastructure/      # EF Core, Redis, Semantic Kernel, Harici API'ler
│   │   ├── Focus.WebAPI/              # REST API ve SignalR Hub'ları
│   │   └── Focus.Admin/               # Razor Pages yönetim paneli
│   └── tests/                         # Test projeleri
│       ├── Focus.UnitTests/           # Birim testleri
│       └── Focus.IntegrationTests/    # Entegrasyon testleri
├── frontend/                          # Next.js istemci uygulaması
└── scripts/                           # Otomasyon ve test betikleri
```
