# Kütüphaneler, Paketler ve Harici Servisler (docs/kutuphaneler-ve-apiler.md)

> Bu doküman; backend, test, altyapı ve harici entegrasyonlarda kullanılacak tüm resmi NuGet paketlerini, kütüphaneleri ve gerekçelerini listeler.

---

## 1. Backend NuGet Paketleri (.NET 9)

### 1.1 Veritabanı ve Vektör Arama
* **`Npgsql.EntityFrameworkCore.PostgreSQL`:** PostgreSQL için resmi EF Core sağlayıcısı.
* **`Pgvector.EntityFrameworkCore`:** PostgreSQL `pgvector` eklentisi entegrasyonu; ajan anıları için vektör benzerlik araması (`CosineDistance`, `L2Distance`).
* **`Microsoft.EntityFrameworkCore.Design`:** Migration ve scaffolding araçları.

### 1.2 Yapay Zekâ ve Ajan Orkestrasyonu
* **`Microsoft.Extensions.AI` / `Microsoft.Extensions.AI.Abstractions`:**
  - .NET'in sağlayıcıdan bağımsız resmi yapay zekâ soyutlaması (`IChatClient`, `IEmbeddingGenerator`).
  - Google Gemini, OpenAI, Claude veya yerel Ollama modelleri tek bir satır kod değiştirmeden DI üzerinden değiştirilebilir.
* **`Microsoft.SemanticKernel`:** Çok adımlı otonom ajan planlaması ve yerel C# fonksiyon araçlarını (function calling) bağlama çatısı.

### 1.3 Gerçek Zamanlı İletişim ve Önbellek
* **`Microsoft.AspNetCore.SignalR`:** Çift yönlü WebSocket omurgası (`TimerHub`, `RoomHub`).
* **`Microsoft.AspNetCore.SignalR.StackExchangeRedis`:** Çoklu API konteynerleri arasında SignalR mesaj senkronizasyonu (Redis Backplane).
* **`StackExchange.Redis`:** Oturum durumu, dağıtık kilit (`RedLock`), hız sınırlama ve hava durumu önbelleği.

### 1.4 Arka Plan Görevleri (Background Jobs)
* **`Hangfire.AspNetCore` + `Hangfire.PostgreSql`:**
  - PostgreSQL üzerinde kalıcı görev kuyruğu.
  - Saatlik seri (streak) denetimleri, günlük yorgunluk ve içgörü analizleri, haftalık Focus Wrapped üretimi, ajan bellek özetleme işleri.
  - Yerleşik dashboard yönetim arayüzü (`Focus.Admin` içine entegre).

### 1.5 Kurumsal Mimari ve CQRS
* **`MediatR`:** CQRS ayrımı; command ve query'leri handler'lara yönlendirir.
* **`FluentValidation.DependencyInjectionExtensions`:** DTO modelleri için güçlü, kurallı ve otomatik doğrulama katmanı.
* **`Mapster`:** AutoMapper'dan 3-4 kat daha hızlı, sıfır bellek tahsisli modern nesne eşleme (mapping).

### 1.6 Güvenlik ve Kimlik Doğrulama
* **`Microsoft.AspNetCore.Authentication.JwtBearer`:** API için JWT doğrulama altyapısı.
* **`Microsoft.AspNetCore.Authentication.Google`:** Google OAuth 2.0.
* **`AspNet.Security.OAuth.GitHub`:** GitHub OAuth.
* **`AspNet.Security.OAuth.Discord`:** Discord OAuth.
* **`AspNet.Security.OAuth.Apple`:** Apple ID oturum açma.

### 1.7 Gözlemlenebilirlik ve Loglama
* **`Serilog.AspNetCore` + `Serilog.Formatting.Compact`:** JSON formatında yapılandırılmış (structured) konsol loglaması.
* **`OpenTelemetry.Extensions.Hosting` + Instrumentation:** ASP.NET Core, EF Core ve Redis metrik ve izleme (trace) enstrümantasyonu.

---

## 2. Test Ekosistemi (Unit, Integration & Architecture)

| Paket | Katman | Kullanım Amacı |
| :--- | :--- | :--- |
| **`xunit` & `xunit.runner.visualstudio`** | Tümü | Endüstri standardı test çatısı. |
| **`NSubstitute`** | Unit | Sade, okunabilir ve hızlı mocklama kütüphanesi. |
| **`FluentAssertions`** | Tümü | `result.Should().BeSuccess()` gibi temiz ve ifade gücü yüksek doğrulama cümleleri. |
| **`Testcontainers.PostgreSql`** | Entegrasyon | Test çalışırken Docker üzerinde saniyeler içinde gerçek PostgreSQL (`pgvector`) konteyneri kaldırır; test bitince otomatik imha eder. Sahte (in-memory) DB riskini sıfırlar. |
| **`Testcontainers.Redis`** | Entegrasyon | Gerçek Redis üzerinde dağıtık kilit ve önbellek testleri. |
| **`WireMock.Net`** | Entegrasyon | Gemini, Open-Meteo veya Pixabay API'lerini HTTP seviyesinde taklit ederek harici servislere bağımlı olmadan deterministik entegrasyon testleri sağlar. |
| **`Microsoft.AspNetCore.Mvc.Testing`** | Entegrasyon | `WebApplicationFactory` ile WebAPI'yi bellek içinde ayağa kaldırıp gerçek HTTP istekleri gönderir. |
| **`NetArchTest.Rules`** | Mimari | Clean Architecture bağımlılık kurallarını derleme testlerinde zorlar (ör. *"Domain projesi hiçbir dış pakete bağımlı olamaz"* testi). |

---

## 3. Harici Servisler ve Bulut Sağlayıcıları

| Servis | Türü | Amaç | Maliyet / Kota |
| :--- | :--- | :--- | :--- |
| **Google Gemini API** | LLM | Ajan koçluğu (Focus Coach), içgörü ve mesaj üretimi (`gemini-flash-latest`) | Saniyede onlarca token; çok ucuz / ücretsiz başlangıç. |
| **Open-Meteo API** | REST API | Kullanıcı konumuna göre anlık hava durumu ve tema eşleme | %100 Ücretsiz, anahtarsız, sınırsız adil kullanım. |
| **Pixabay API** | REST API | Canlı video döngüleri ve poster görselleri içe aktarma | %100 Ücretsiz, ticari lisanslı (Cloudflare User-Agent gerektirir). |
| **Cloudflare R2** | S3 Depolama | Optimize edilmiş WebM/MP4 videoları ve Opus/AAC ses dosyalarını barındırma | **Çıkış trafiği (egress) 0$ tamamen ücretsizdir.** |
| **LemonSqueezy / Paddle** | MoR Ödeme | Pro abonelikler, faturalama, global vergi ve KDV yönetimi | İşlem başına komisyon (~%5 + 50¢). |
