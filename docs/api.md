# Harici API'ler, Lisanslar ve Entegrasyon Rehberi (docs/api.md)

> Bu doküman; platformun entegre olduğu harici API'leri, anahtar alma adımlarını, canlı test edilmiş limit ve kısıtlamalarını ve lisans gerçeklerini detaylandırır.
> İlgili: [sesler.md](sesler.md), [temalar.md](temalar.md), [riskler-ve-oneriler.md](riskler-ve-oneriler.md).

---

## 1. Canlı Test Edilmiş ve Çalışan API'ler

### 1.1 Pixabay Video API (Canlı Arka Planlar)
* **Durum:** ✅ **Çalışıyor** (Canlı test: 72-113 ms yanıt süresi).
* **Kullanım:** 1080p kesintisiz döngüye uygun atmosfer videoları (yağmurlu pencere, kahve dükkanı, şömine, doğa).
* **Lisans:** Pixabay Content License (Ticari kullanıma serbest, atıf zorunlu değil ama önerilir).
* **Kritik Teknik Not:** Python / .NET HTTP isteklerinde standart bir `User-Agent` başlığı (ör. `User-Agent: Mozilla/5.0...`) **zorunludur**. Varsayılan kütüphane User-Agent'ları Cloudflare tarafından `403 Forbidden` ile engellenir. Ayrıca `per_page` parametresi minimum 3 olmalıdır; daha küçük değerler `400 Bad Request` döner.
* **Örnek Çağrı:**
  ```http
  GET https://pixabay.com/api/videos/?key=YOUR_KEY&q=rain+window&video_type=film&per_page=3
  Headers:
    User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) FocusApi/1.0
  ```
* **Kullanım Prensibi:** Kullanıcı doğrudan bu API'ye bağlanmaz; Admin panelinde içerik kütüphanemize aktarım yapılırken kullanılır.

### 1.2 Open-Meteo API (Hava Durumu Senkronizasyonu)
* **Durum:** ✅ **Çalışıyor** (Canlı test: ~370 ms yanıt süresi).
* **Kullanım:** Kullanıcının bulunduğu şehirdeki hava durumunu öğrenip arka plan temasını ve ses mikserini dinamik olarak eşleme (ör. yağmur yağıyorsa "Yağmurlu Kafe" temasını önerme).
* **Lisans & Maliyet:** %100 Ücretsiz, açık kaynak, **API anahtarı dahi gerektirmez**.
* **Örnek Çağrı:**
  ```http
  GET https://api.open-meteo.com/v1/forecast?latitude=41.0082&longitude=28.9784&current_weather=true
  ```
* **Önbellek Kuralı:** Şehir bazında Redis'te 30 dakika önbelleğe alınır; aynı şehirdeki binlerce kullanıcı için harici istek tekrarlanmaz.

### 1.3 Google Gemini API (Ajan LLM Beyni)
* **Durum:** ✅ **Çalışıyor** (Model: `gemini-flash-latest`).
* **Kullanım:** Focus Coach (Verimlilik Koçu) kişiselleştirilmiş motivasyon ve haftalık özet mesajları üretimi.
* **Kritik Mimari Kural:**
  - Canlı testlerimizde yanıt süresi 3 ile 11 saniye arasında ölçülmüştür. Bu nedenle ajan **asla sayaç akışında bekletilmez**; arka plan işinde (Hangfire) çalışıp sonucu SignalR ile iletir.
  - Canlıda `x-goog-api-key` başlığıyla çağrılır.

---

## 2. Kısıtlı veya Dikkat Gerektiren Servisler

### 2.1 Jamendo API (Lisans Tuzağına Dikkat!)
* **Durum:** Teknik olarak 200 OK ile MP3 stream linki dönüyor (~400 ms).
* **Hukuki Engel:** Canlı testlerimizde Jamendo'da `lofi` etiketi altındaki parçaların tamamının **`CC BY-NC-ND 3.0`** lisanslı olduğu tespit edilmiştir (`license_ccurl` alanı).
* **Karar:** `NC` (Non-Commercial) ve `ND` (No-Derivatives) taşıdıkları için, ücretli Pro üyelik sunan Focus platformunda **ticari olarak kullanılamazlar**. Yalnızca yerel test ve prototipleme amacıyla kullanılabilir.

### 2.2 Pexels Video API
* **Durum:** Şu anda geliştirici portalında yeni API anahtarı dağıtımı geçici olarak durdurulmuştur (*"New API key issuance is currently paused"*).
* **Çözüm:** Pixabay Video API aynı kaliteyi sağlamaktadır; Pexels'ten gerekirse site üzerinden manuel kürasyonla içerik indirilebilir.

### 2.3 Freesound API
* **Durum:** Kayıt formunda bazı e-posta adresleri spam koruması nedeniyle reddedilebilmektedir.
* **Çözüm:** CC0 ses efektleri doğrudan siteden elle indirilip R2 depolama alanımıza yüklenecektir.

---

## 3. Özet API Entegrasyon Matrisi

| Servis | Canlı Durum | Görev | Maliyet | Lisans Uygunluğu |
| :--- | :---: | :--- | :--- | :--- |
| **Pixabay Video** | ✅ Aktif | HD Canlı Temalar | Ücretsiz | ✅ Ticari Uygun (Atıf önerilir) |
| **Open-Meteo** | ✅ Aktif | Hava Durumu Senkronu | Ücretsiz | ✅ Ticari Uygun |
| **Google Gemini** | ✅ Aktif | AI Odak Koçu | Düşük / Bütçeli | ✅ Ticari Uygun |
| **Jamendo** | ⚠️ Dikkat | Lo-Fi Akışı | Ücretsiz API | ❌ **Ticari Uygun Değil (NC-ND)** |
| **Pexels** | ⏸️ Duraklatıldı| Video Döngüleri | Ücretsiz | ✅ (Key açılınca kullanılabilir) |
| **Cloudflare R2** | Planlandı | Statik Medya CDN | 0$ Çıkış Bant | ✅ Tamamen Bizim Kontrolümüzde |
