# Riskler, Tuzaklar ve Stratejik Öneriler (docs/riskler-ve-oneriler.md)

> Bu doküman; piksel ev ve avatar ekosistemindeki hukuki, teknik, oyun ekonomisi ve pazar risklerini ve bunlara karşı geliştirilen somut önlemleri tanımlar.

---

## 1. Hukuki ve Lisans Riskleri

### 1.1 Piksel Varlık (Sprite / Tileset) Telif Hakları
* **Risk:** İnternetten (itch.io, Pinterest vb.) bulunan piksel eşya, mobilya veya karakter çizimleri telifli olabilir veya "yalnızca ticari olmayan oyunlar için" lisanslanmış olabilir.
* **Önlem:**
  - Projede kullanılan tüm piksel çizimler (mobilyalar, kıyafetler, duvarlar) ya CC0 / ticari serbest lisanslı kütüphanelerden seçilecek ya da ekip bünyesinde özgün olarak çizilecektir.
  - Her çizim için kaynak ve lisans bilgisi `AssetLicense` tablosunda kayıt altına alınacaktır.

### 1.2 "Ticari Olmayan" (Non-Commercial - NC) Müzik Tuzağı
* **Risk:** Platformda ücretli Pro üyelik veya uygulama içi satın alma bulunduğu için sistem ticari sayılır.
* **Bulgu:** Jamendo'daki popüler lofi parçaların tamamı `CC BY-NC-ND` lisanslıdır. Canlıda ticari olarak kullanılması yasal risk doğurur.
* **Önlem:** Yalnızca CC0, CC-BY veya ticari kullanım hakkı kesin olan sesler kütüphaneye dahil edilir.

### 1.3 Tıbbi İddia ve Sağlık Regülasyonları
* **Risk:** Ajanın "doktor" unvanı kullanması tüketici koruma ve sağlık regülasyonları ihlali sayılabilir.
* **Önlem:** Ürün içi dil "Verimlilik Koçu", "Odak Yoldaşı" olarak sınırlandırılmıştır; tıbbi iddia bulunmaz.

---

## 2. Oyun Ekonomisi ve Hile (Anti-Cheat) Riskleri

### 2.1 Focus Coin ve XP Enflasyonu (Bot / Çiftlik Riski)
* **Risk:** Kullanıcıların tarayıcıda sayacı 24 saat açık bırakarak veya otomatik bot komutları çalıştırarak sınırsız para kasması ve mağazadaki tüm eşyaları haksızca açması.
* **Önlem:**
  - **Günlük Tavan (Daily Cap):** Günde en fazla 400 Focus Coin kazanılabilir (~6.6 saat odak). Tavan aşıldığında odak kaydedilir ancak ek para verilmez.
  - **Sunucu Otoriteli Sayaç:** İstemci saatini ileri alarak seans tamamlanamaz; başlangıç ve bitiş sunucu saat damgasıyla doğrulanır.
  - **Coin Defteri (`CoinLedgerEntry`):** Her coin kazanımı ve harcaması muhasebe kaydı olarak tutulur; doğrudan veritabanı alanı manipüle edilemez.

---

## 3. Teknik ve Performans Riskleri

### 3.1 Piksel Canvas Render Yükü ve Pil Tüketimi
* **Risk:** Izometrik mobilya yerleşimi ve animasyonlu karakterlerin düşük donanımlı cihazlarda aşırı CPU/GPU tüketmesi.
* **Önlem:**
  - Statik mobilyalar tek bir arka plan katmanında önceden derlenir (pre-rendered canvas layer); her karede tüm oda baştan çizilmez.
  - `Page Visibility API` ile sekme arka plana geçtiğinde video ve canvas animasyonları durdurulur (30 FPS sınırı).

### 3.2 Tarayıcı Arka Plan Sekme Sapması (Timer Drift)
* **Risk:** Kullanıcı başka sekmeye geçtiğinde JavaScript sayacının yavaşlaması.
* **Önlem:** Sayaç süresi istemci değişkeninde değil, sunucunun döndüğü `plannedEndAt - serverNow` UTC farkı üzerinden anlık hesaplanır.

---

## 4. Stratejik Yol Haritası Önerileri

| Aşama | Yapılması Gereken (DO) | Kesinlikle Yapılmaması Gereken (DON'T) |
| :--- | :--- | :--- |
| **MVP (Faz 1)** | Temel piksel oda, masaya oturma seansı, 4 kanallı mikser, Google/GitHub OAuth, temel kıyafet gardırobu. | Henüz eşya kütüphanesini yüzlerce mobilyayla şişirmek, karmaşık çok oyunculu oda senkronuna girmek. |
| **Büyüme (Faz 2)** | Çift kişilik çalışma masası (arkadaş daveti), mağazadan mobilya satın alma, haftalık Focus Wrapped kartı. | Odalara serbest metin sohbet ekleyerek moderasyon yükü yaratmak (yalnızca sessiz çalışma ve tepkiler). |
| **Monetizasyon (Faz 3)**| Pro üyelik (Çatı katı loft oda planları, özel siberpunk/retro mobilya setleri, LemonSqueezy). | Temel sayacı paralı yapmak veya agresif banner reklamlar basmak. |
