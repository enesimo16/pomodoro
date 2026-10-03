# Focus — Proje Başlangıç & Mimari Dokümantasyonu (baslamadan-once.md)

> **Tarih:** Ekim 2026  
> **Proje:** Focus (Habbo Tarzı Piksel Yaşam ve Ortak Odaklanma Ekosistemi)  
> **Vizyon:** Sıradan bir kronometre olmanın ötesine geçen; kullanıcının kendi özelleştirilebilir piksel odasında (Pixel Loft) ve piksel karakteriyle (Avatar) yaşadığı, masaya oturduğunda odaklanma seansının başladığı, arkadaşlarını odasına davet edip birlikte çalışabildiği, seviye (XP) ve mobilya/kıyafet ekonomisine sahip yeni nesil bir derin odaklanma platformu.

---

## 1. Proje Özeti ve Felsefesi

Piyasada milyonlarca klasik Pomodoro sayacı bulunmaktadır. Bunların %95'i yalnızca 25 dakika geri sayan ve birkaç temel istatistik gösteren yüzeysel araçlardır. Bu uygulamaların en büyük sorunu **yüksek terk edilme (churn) oranıdır**: Kullanıcı 1 hafta sonra sıkılır, monotonlaşır ve uygulamayı bırakır.

**Focus'un Devrimsel Yaklaşımı (Habbo Tarzı Piksel Yaşam):**
1. **Piksel Ev & Çalışma Ritüeli:** Kullanıcının retro/izometrik piksel bir odası vardır. Odada çalışma masası, ergonomik sandalye, yatak, kitaplık, plak çalar ve dışarıyı gösteren bir pencere yer alır.
2. **Doğal Odaklanma:** Karakter çalışma masasına oturduğunda masa lambası yanar, seçilen lo-fi müzik ve ambiyans başlar, sayaç işlemeye başlar.
3. **Piksel Avatar ve Özelleştirme:** Kullanıcı karakterinin saçını, ten rengini, kapüşonlusunu, gözlüğünü ve ayakkabılarını dilediğince tasarlar.
4. **Seviye ve Ekonomi (XP & Focus Coins):** Odaklanılan her dakika XP ve Odak Parası (Focus Coins) kazandırır. Bu paralarla mağazadan odaya yeni eşyalar (retro arcade makinesi, neon tabelalar, kahve barı, bitkiler) ve karaktere havalı kıyafetler satın alınır.
5. **Sosyal Birlikte Çalışma (Habbo Tarzı Ortak Odalar):** Kullanıcı arkadaşını kendi piksel evine davet edebilir. İki avatar aynı odada farklı masalara oturup sessizce birlikte çalışır; birbirlerinin çalışma ışığını ve odaklandığını canlı olarak görürler.

---

## 2. Pazardaki Rakipler ve Focus'un Farkı

| Rakip | Güçlü Yönü | Zayıf / Boşta Kalan Yönü | Focus'un Üstünlüğü |
| :--- | :--- | :--- | :--- |
| **Pomofocus.io** | Sadeliği ve hızlı açılması. | Kuru, sıkıcı, hiçbir motivasyon ve estetik bağ kurmuyor. | Yaşayan, kişiselleştirilebilen izometrik bir piksel dünya sunar. |
| **Forest** | Ağaç dikme oyunlaştırmasıyla lider. | Sadece statik ağaç diktiriyor; etkileşim yok, sosyal alan yok. | Kullanıcı kendi odasını kurar, mobilya döşer, karakterini giydirir, arkadaşını evine davet eder. |
| **LifeAt / Lofi.co** | Canlı video arka planları ve lo-fi sesleri. | Sadece "arka plan videosu"; oyunlaştırma, seviye ve avatar aidiyeti yok. | Kendi piksel evini inşa etme ve seviye atlama hissiyle terk edilme oranını sıfırlar. |
| **Habbo / Club Penguin**| Güçlü piksel nostaljisi ve sosyal oda kültürü. | Bir üretkenlik ve çalışma aracı değil, eski tip sohbet oyunu. | Habbo'nun efsanevi oda ve avatar mekaniğini **derin odaklanma ve bilimsel üretkenlikle** birleştirir. |

---

## 3. Temel Sistemler ve Modüller

### 3.1 Piksel Oda & Eşya Yerleşimi (Room Decorator)
- Izometrik karo (isometric grid) tabanlı oda yerleşimi.
- Masa, sandalye, lamba, halı, duvar kağıdı, zemin kaplaması, kitaplık, müzik çalar.
- **Canlı Pencere:** Odadaki pencere, kullanıcının gerçek dünyadaki hava durumuyla senkronize çalışır (dışarıda yağmur yağıyorsa pencerede piksel yağmur akar).

### 3.2 Karakter & Gardırop (Wardrobe Studio)
- Piksel karakter katmanları: Ten, Saç, Üst Giyim (T-shirt, hoodie, ceket), Alt Giyim, Şapka/Bandana, Gözlük, Ayakkabı.
- Başlangıçta temel kıyafet seti ücretsiz; seviye atladıkça ve odaklandıkça nadir kıyafetler mağazadan alınır.

### 3.3 Akustik & Plak Çalar (Vinyl Audio Station)
- Odadaki piksel plak çalar üzerinden 4 kanallı ses mikseri yönetilir (Lo-Fi müzik, doğa sesleri, Web Audio gürültü sentezleri).

### 3.4 Ortak Çalışma (Co-Working Rooms)
- Kullanıcı arkadaşına oda davet bağlantısı atar (`focus.app/room/join?code=XYZ`).
- Arkadaşının avatarı odaya gelir, boş masaya oturur.
- İkisi de odaklandığında odada eşzamanlı çalışma bonusu (XP & Coin çarpanı) kazanırlar.
- Sessiz hesap verebilirlik (Silent Accountability): Serbest sohbet yerine kahve ikram etme veya odaklanma emojileri.

---

## 4. Para Kazanma (Monetizasyon) Modeli

1. **Freemium Tabanı:** Temel oda, başlangıç kıyafetleri ve temel mobilyalar, standart sayaç ve 4 kanallı ses mikseri tamamen ücretsiz.
2. **Focus Pro:**
   - Genişletilmiş Çatı Katı / Loft Oda Planları (2 katlı piksel odalar).
   - Özel tasarım estetik mobilya koleksiyonları (Siberpunk, Japon Zen Bahçesi, Retro 80'ler Arcade).
   - Özel animasyonlu kıyafetler ve aksesuarlar.
   - Sınırsız oda davet hakkı ve gelişmiş haftalık Focus Wrapped analitiği.
