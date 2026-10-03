# Focus — Habbo Tarzı Piksel Yaşam & Ortak Odaklanma Platformu

Focus; klasik sayaçların ötesine geçen; **Habbo tarzı izometrik piksel ev/oda (Pixel Room)** ve **özelleştirilebilir retro piksel karakter (Avatar)** yaşam döngüsüne sahip, çok kanallı atmosferik ses mikseri, pencere hava durumu senkronu, seviye (XP) ve mobilya/kıyafet ekonomisi ile donatılmış yeni nesil bir derin odaklanma ve ortak çalışma ekosistemidir.

---

## 🎮 Konsept: Piksel Evinde Odaklan & Yaşa

- **Piksel Ev & Çalışma Masası:** Her kullanıcının retro piksel bir stüdyo dairesi vardır. Masaya oturduğunuz an çalışma lambası yanar ve odaklanma seansı başlar.
- **Karakter & Gardırop:** Piksel karakterinizi saçından ayakkabısına kadar dilediğiniz gibi tasarlayın.
- **Seviye ve Ekonomi:** Odaklandığınız her dakika XP ve Odak Parası (Focus Coins) kazandırır. Kazandığınız paralarla mağazadan odanıza mobilyalar (plak çalar, neon tabela, kahve barı, bitkiler) ve karakterinize retro kıyafetler satın alın.
- **Arkadaşını Odana Davet Et:** Habbo tarzı sosyal çalışma odaları. Arkadaşınızı evinize davet edin, yan yana masalara oturup eşzamanlı odaklanarak bonus kazanın.

---

## 🏗️ Mimari ve Dizin Yapısı

Proje kurumsal **Clean Architecture** prensiplerine uygun olarak katmanlara ayrılmıştır:

```
.
├── backend/                           # C# .NET 9 Katmanlı Mimari
│   ├── Focus.sln                      # Çözüm dosyası
│   ├── src/
│   │   ├── Focus.Domain/              # Avatar, PixelRoom, RoomItem, FocusSession varlıkları
│   │   ├── Focus.Application/         # CQRS (MediatR), seviye motoru, servis arayüzleri
│   │   ├── Focus.Infrastructure/      # EF Core, PostgreSQL (pgvector), Redis, Semantic Kernel
│   │   ├── Focus.WebAPI/              # RESTful API, SignalR Hub'ları (TimerHub, RoomHub)
│   │   └── Focus.Admin/               # Razor Pages yönetim paneli (Mağaza eşya yönetimi)
│   └── tests/
│       ├── Focus.UnitTests/           # Domain & Application birim testleri
│       └── Focus.IntegrationTests/    # API & Altyapı entegrasyon testleri
│
├── frontend/                          # Next.js (TypeScript, Tailwind CSS, Canvas / Web Audio API)
├── docs/                              # Modüler mimari dokümanları
│   ├── agent-sistemi.md               # Oda içi akıllı asistan mimarisi ve kural motoru
│   ├── algoritmalar.md                # Sunucu otoriteli sayaç, XP eğrisi, Coin formülü
│   ├── api.md                         # Harici API'ler, lisanslar ve entegrasyon kılavuzu
│   ├── api-sozlesmesi.md              # REST & SignalR ortak sözleşmesi (/avatar, /room, /shop)
│   ├── ekstra-ozellikler.md           # Rakipleri ayrıştıracak inovatif özellikler havuzu
│   ├── kullanici-sistemi.md           # OAuth 2.0, Guest Session, token ve güvenlik mimarisi
│   ├── kutuphaneler-ve-apiler.md      # NuGet paketleri, test stack (Testcontainers) ve bulut servisleri
│   ├── ozellikler.md                  # Faz matrisi, Free vs Pro ayrımı ve kullanıcı yolculukları
│   ├── proje-sistemi.md               # Katmanlar, Redis anahtar tasarımı, SignalR ve Docker standartları
│   ├── riskler-ve-oneriler.md         # Hukuki, teknik ve ürün riskleri ile stratejik öneriler
│   ├── sesler.md                      # 4 kanallı mikser, piksel plak çalar ve Web Audio gürültü sentezi
│   ├── temalar.md                     # Piksel oda tasarımları, video döngüleri ve pil tasarruf mimarisi
│   └── veri-modeli.md                 # Avatar, PixelRoom, ItemCatalog, FocusSession ERD diyagramı
│
├── scripts/                           # Otomasyon ve test betikleri (Python)
├── AGENTS.md                          # Antigravity yapay zekâ anayasası
├── baslamadan-once.md                 # Başlangıç ve vizyon rehberi
├── docker-compose.yml                 # PostgreSQL, Redis, API ve Admin orkestrasyonu
├── .env / .env.example                # Ortam değişkenleri şablonu
├── .gitignore                         # Git hariç tutma kuralları
└── .dockerignore                      # Docker imajı hariç tutma kuralları
```

---

## 🛠️ Teknoloji Yığını

- **Backend:** C# / .NET 9 Web API
- **Admin Paneli:** ASP.NET Core Razor Pages
- **Frontend:** Next.js (React, TypeScript, Tailwind CSS, Canvas / Web Audio API)
- **Veritabanı:** PostgreSQL (Vektör destekli: `pgvector:pg16`)
- **Önbellek & Dağıtık Durum:** Redis
- **Gerçek Zamanlı İletişim:** SignalR
- **Yapay Zekâ:** Microsoft.Extensions.AI + Google Gemini Flash
- **Konteynerizasyon:** Docker & Docker Compose

---

## 🚀 Hızlı Başlangıç

### 1. Ortam Değişkenlerini Hazırlayın
`.env.example` dosyasını kopyalayarak `.env` dosyanızı oluşturun:
```bash
cp .env.example .env
```

### 2. Docker ile Tüm Sistemi Ayağa Kaldırın
```bash
docker compose up -d --build
```
- **Web API:** `http://localhost:5000`
- **Admin Paneli:** `http://localhost:5001`
- **PostgreSQL (pgvector):** `localhost:5432`
- **Redis:** `localhost:6379`

### 3. Yerel Olarak (Lokalde) Çalıştırma
```bash
# Backend çözümünü derleyin
dotnet build backend/Focus.sln

# API'yi başlatın
dotnet run --project backend/src/Focus.WebAPI

# Testleri çalıştırın
dotnet test backend/Focus.sln
```
