# Temalar ve Piksel Oda Atmosferi (docs/temalar.md)

> Bu doküman; piksel oda stillerini, oda içi pencere hava durumu senkronizasyonunu, video döngülerini ve pil dostu görsel optimizasyonları tanımlar.

---

## 1. Piksel Oda Temaları ve Stilleri

Kullanıcının seçebileceği veya seviye atladıkça açabileceği temel oda şablonları:

1. **Retro Lo-Fi Stüdyo (Başlangıç):** Ahşap zemin, tuğla duvar, masa lambası, piksel saksı bitkisi, plak çalar ve büyük bir pencere.
2. **Yağmurlu Çatı Katı (Loft):** Eğimli tavan pencereleri, yumuşak sarı ışıklar, kahve köşesi ve kitap rafları.
3. **Siberpunk Neon Oda:** Koyu mor ve camgöbeği (cyan) tonlar, neon duvar tabelaları, çoklu ekranlı çalışma masası.
4. **Japon Zen Odası:** Tatami zemin, shoji sürgülü kapılar, bonsai ağacı ve alçak ahşap çalışma masası.
5. **Karanlık Minimalist (Zen OLED):** Yalnızca karakter, masa ve tek bir odak lambası; dikkat dağıtıcı tüm mobilyalar gizli.

---

## 2. Canlı Pencere Mekanizması (Live Window System)

Piksel odanın en can alıcı noktası arkadaki **büyük penceredir**:

- **Hava Durumu Senkronu:** Open-Meteo API'den kullanıcının yerel hava durumu çekilir (30 dk Redis önbelleği).
- **Görsel Akış:**
  - Dışarıda yağmur varsa: Piksel pencere camından yağmur damlaları süzülür, sokak lambalarının ışığı buğulanır.
  - Dışarıda kar varsa: Pencere pervazında piksel karlar birikir.
  - Gece olduğunda: Odanın penceresinden yıldızlar ve ay görünür, oda içi masa lambası otomatik aydınlanır.
- **Canlı Video Desteği (Pixabay):** Pencerenin arkasına gerçek canlı video döngüleri (ör. yağmurlu orman, Tokyo caddesi) gömülebilir.

---

## 3. Performans ve Pil Optimizasyonu (Frontend Ekibi İçin)

1. **Piksel Sanatı Render'ı:** Next.js Canvas / WebGL üzerinde `image-rendering: pixelated;` ile keskin ve sıfır bulanıklıkla çizilir.
2. **Page Visibility API:** Kullanıcı başka sekmeye geçtiğinde veya tarayıcıyı küçülttüğünde oda içi animasyonlar ve pencere videosu otomatik olarak `pause` moduna geçer.
3. **Kare Hızı (FPS) Kısıtlaması:** Piksel animasyonlar için 30 FPS fazlasıyla yeterlidir; ekran kartını ve bataryayı yormaz.
