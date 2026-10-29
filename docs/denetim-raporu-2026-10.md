# Kod Denetimi ve Düzeltme Raporu (Ekim 2026)

Kapsam: 3–28 Ekim 2026 arasındaki 38 commit'in tamamı (backend, admin paneli, frontend, altyapı).
Yöntem: 6 paralel denetim (güvenlik/auth, oturum/ekonomi, sosyal/SignalR, AI/medya, admin paneli, frontend/altyapı) yaklaşık 150 bulgu üretti.
Bulgular 7 düzeltme paketine ayrıldı ve 44 commit ile kapatıldı (`bcfa0a7..2273d75`).

Son doğrulama (HEAD):
- Backend: 467 birim ve 112 entegrasyon testi geçti, 0 uyarı ve 0 hata ile derlendi.
- Frontend: `eslint` temiz, `next build` başarılı.

Backend doğrulaması .NET 9 SDK (9.0.320) ile, repodaki `net9.0` hedefi değiştirilmeden yapıldı. Frontend'in ana sayfası, misafir girişindeki hata durumu ve oda zamanlayıcısı tarayıcıda (API kapalıyken) kontrol edildi.

## En kritik bulgular ve durumu

| Bulgu | Durum |
| :--- | :--- |
| Admin master anahtarı herkese açık frontend linkinde ve config'de | Kaldırıldı, anahtarlar yalnızca env'den okunuyor |
| Gerçek API anahtarları, JWT imzalama anahtarı ve DB/Redis parolaları `appsettings.json` içinde | Kaldırıldı, en az 32 baytlık JWT anahtarı başlangıçta doğrulanıyor |
| Postgres şeması hiç oluşturulmuyordu (`MigrateAsync` yoktu) | Açılışta migration uygulanıyor |
| Admin analytics API'si anonimdi | Uç kaldırıldı |
| Boş Google ClientId'de token audience kontrol edilmiyordu, misafir hesap ele geçirilebiliyordu | Düzeltildi, claim JWT `sub` üzerinden yapılıyor |
| Break seansıyla sıfır sürede XP/coin kasma, çift tamamlama ve mağazada çift harcama | Ödüller plana göre sınırlandı, günlük limit eklendi, eşzamanlılık token'ları ve unique index'ler eklendi |
| Özel odalara davetsiz katılım, RoomHub/TimerHub yetkisiz dinlenebiliyordu | Davet zorunlu, hub'larda `[Authorize]` ve üyelik kontrolü var |
| Satın alınmadan mobilya/kıyafet kullanımı | Katalog ve envanter sahipliği zorunlu |
| Herkes global AI hafızasını zehirleyebiliyordu, token bütçesi yoktu | Tamamlanmış seans şartı, günlük token bütçesi, rate limit |
| Sahte/mock veriler (hava durumu, medya fallback'leri, hash embedding'ler, admin içgörüleri, frontend arkadaş listesi) | Gerçek veri ya da boş/nötr durum gösteriliyor |
| ApiLab'de XSS (oturma yeri etiketi, inline onclick), tema renklerinde CSS enjeksiyonu | Kaçışlama ve `#RRGGBB` doğrulaması eklendi |
| Frontend: sahte giriş formları, tur sayacı çift artıyordu, AudioContext sızıntısı, token'lar localStorage'da | Sunucuya bağlı zamanlayıcı, HttpOnly çerez, ses motorunun temizlenmesi |

## Paketler

- **B1 Platform:** sırlar, migration, deterministik seed, CORS, forwarded headers, Hangfire yetkisi, admin anahtar karşılaştırması. Commit'ler: `256118b`, `c1efb8c`.
- **B2 Auth:** Google doğrulaması, misafir claim, refresh rotasyon yarışı, rate limiting, doğrulama hattı. Commit'ler: `3a5917c`, `bb20627`, `21e4a54`, `6b76b96`, `8ebc289`.
- **B3 Ekonomi:** seviye eğrisi, streak freeze, eşzamanlılık, ödül kuralları, eski seans işi, Wrapped, istatistikler. Commit'ler: `603ce56` … `5dfcdb6`.
- **B4 Sosyal:** davetler, hub yetkisi, ziyaretçi defteri, eski üyelerin temizlenmesi, sahiplik, grid ve koltuklar. Commit'ler: `6003d4b`, `aba5715`, `2b7ca0e`, `4345abe`, `47aa649`.
- **B5 AI/Medya:** Ai ayarları, header ile anahtar, zaman aşımları, hafıza zehirlenmesi, token bütçesi, hava durumu, medya, anonimleştirme. Commit'ler: `3eb3f84`, `159e07b`, `257a1da`, `8a402e2`, `42f2211`, `94d180d`, `10b4edf`.
- **B6 Admin:** XSS, boş isim çökmesi, coin/XP doğrulaması, sidebar, ApiDocs, sayfalama, sahte veriler, CSS enjeksiyonu. Commit'ler: `8c6c99b` … `4d889ce`.
- **F1 Frontend:** API istemcisi, misafir akışı, zamanlayıcı, ses, erişilebilirlik, Dockerfile, script'ler. Commit'ler: `bcfa0a7`, `57a7469`, `b742e61`, `1aa6d71`.

## Yeni migration'lar

Açılışta `MigrateAsync` ile otomatik uygulanırlar:

- `FixDeterministicPublicRoomSeed`
- `AddRefreshTokenConcurrencyStamp`
- `HardenSessionEconomy`: önce yinelenen envanter satırlarını siler, kullanıcı başına yalnızca en yeni aktif seansı bırakır.
- `HardenSocialRooms`
- `HardenAiMemory`: mevcut tüm embedding'leri sıfırlar, Gemini anahtarı varsa seeder yeniden üretir.

## İnsan tarafından yapılması gerekenler

1. **Sızan tüm sırları döndürün; git geçmişinde hâlâ duruyorlar:**
   - Postgres ve Redis parolaları
   - JWT imzalama anahtarı
   - Pixabay anahtarı
   - Jamendo client id ve secret
   - Üç admin anahtarı (`focus_master_key_9971x`, `focus_admin_sec_2026`, `adm_key_enes_dev_44`)
2. **`.env` dosyasını doldurun:**
   - `Auth__Jwt__SigningKey` (en az 32 bayt)
   - `AdminSecurity__Keys__0`
   - `Auth__Google__ClientId`: boşsa Google girişi kapalıdır.
   - `Ai__Gemini__ApiKey`
   - `ForwardedHeaders__KnownProxies`: proxy arkasındaysa gerekir.
   - Canlı ortamda `ASPNETCORE_ENVIRONMENT=Production`.
3. **Canlı veritabanında migration öncesi yedek alın ve şu çakışmaları kontrol edin:**
   - `coin_ledger_entries`: aynı (UserId, SessionId, Reason) kombinasyonuna sahip satırlar
   - Sahip başına birden fazla özel oda
   - Aynı (RoomId, SeatIndex) değerine sahip satırlar
4. **Frontend ekibine sözleşme değişikliğini iletin:**
   - `/auth/claim` artık yalnızca `{ idToken }` alıyor ve misafirin kendi token'ını istiyor.
   - `GET /study-rooms/{code}` ve ziyaretçi defteri okuma uçları kimlik doğrulama gerektiriyor.
5. **Henüz canlı ortamda doğrulanmayanlar** (bu makinede Docker, WSL ve Postgres yok). Yeni 5 migration'ın Postgres SQL'i `dotnet ef migrations script` ile üretilip incelendi; ham SQL filtreleri, enum'ların saklanma biçimiyle (`Status` metin, `StudyRoomType` tamsayı) uyumlu. Yine de şu adımlar yapılmadı:
   - Gerçek Postgres/pgvector üzerinde migration'lar
   - Docker imajları
   - Admin panelinin ve frontend'in çalışan bir API'ye bağlı olarak tarayıcıda gezilmesi
6. **Bilinen sınırlar:**
   - Streak, istatistik ve günlük limit günleri UTC'ye göre hesaplanıyor (kullanıcı saat dilimi yalnızca AI tarafında kullanılıyor).
   - Atılan veya bayatlamış üyenin SignalR bağlantısı, kopana kadar grupta kalıyor.
   - Admin coin verme gerekçesi yalnızca uygulama loguna yazılıyor.
