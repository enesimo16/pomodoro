# Ses ve Müzik Mimarisi (docs/sesler.md)

> 4 kanallı ses mikseri, oda içi piksel plak çalar, Web Audio gürültü sentezi, lisans kuralları ve ses optimizasyonu.
> İlgili: [api.md](api.md), [riskler-ve-oneriler.md](riskler-ve-oneriler.md), [veri-modeli.md](veri-modeli.md).

---

## 1. Piksel Plak Çalar ve 4 Kanallı Mikser

Piksel odanın köşesinde veya masanın üzerinde etkileşimli bir **Retro Plak Çalar (Vinyl Audio Station)** yer alır. Kullanıcı plak çaları açtığında 4 bağımsız katmanı karıştırarak kendi akustik atmosferini oluşturur:

```
[Oda İçi Plak Çalar / Mikser]
   ├── Katman 1: Melodik Zemin (Lo-Fi Chillhop, Akustik Piyano, Ambient Drone)
   ├── Katman 2: Oda & Doğa Ambiyansı (Pencereden Yağmur, Şömine, Kafe, Gece Ormanı)
   ├── Katman 3: Prosedürel Gürültü (Web Audio ile sentezlenen Brown, Pink, White Noise)
   └── Katman 4: Taktil Doku Sesleri (Mekanik Klavye, Sayfa Çevirme, Plak Cızırtısı)
```

### 1.1 Prosedürel Gürültü Sentezi (Dosyasız Ses):
Brown ve Pink noise dosyaları sunucudan MP3 olarak indirilmez; doğrudan kullanıcının tarayıcısında Web Audio API (`AudioBufferSourceNode` + filtre) ile sıfır veri tüketimi ve sıfır gecikmeyle sonsuz döngü olarak sentezlenir.

---

## 2. Lisans ve Telif Güvencesi (Kritik Kural)

Üründe ücretli Pro katman bulunduğu için tüm içerikler **ticari lisanslı** olmak zorundadır:

1. **Kabul Edilen Lisanslar:** CC0 (Kamu Malı), CC-BY (Atıf ile ticari uygun), Pixabay Content License, Kendi ürettiğimiz lisanslı parçalar.
2. **Kesinlikle Reddedilenler:** `NC` (Non-Commercial - Ticari Olmayan) ve `ND` (No-Derivatives - Değiştirilemez) içeren lisanslar.
3. **Jamendo Kısıtlaması:** Jamendo'daki popüler lofi parçaların tamamı `CC BY-NC-ND` lisanslı olduğu için canlı prodüksiyonda ticari olarak **kullanılamaz**.
4. **YouTube Audio Library:** Yalnızca YouTube içi videolara izin verdiği için uygulamada kullanılması yasaktır.

---

## 3. Akustik Geçiş Protokolü (Tibet Çanağı & Fade-in)

- **Seans Başlangıcı:** Karakter masaya oturduğunda müzik pat diye çalmaz; 5 saniye içinde sıfırdan fısıltı seviyesine yükselerek (fade-in) beyni sakinleştirir.
- **Seans Sonu:** Tiz ve stres yaratan ziller yerine derin bir **Tibet Çanağı (Singing Bowl)** veya yumuşak bir piyano akoruyla seansın tamamlandığı bildirilir.
