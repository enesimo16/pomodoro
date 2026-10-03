# Algoritmalar ve Matematiksel Modeller (docs/algoritmalar.md)

> Sunucu otoriteli sayaç, akış koruması, yorgunluk skoru, kronotip analizi, XP seviye eğrisi, Focus Coin ekonomisi ve hile önleme.
> Tüm katsayılar `appsettings.json` içinde `Algorithms` bölümünde tutulur; kodda sabit değer bulunmaz.

---

## 0. Temel Kural: Sayaç Sunucu Otoritelidir

Tarayıcı arka plan sekmelerinde zamanlayıcıları yavaşlatır, kullanıcı sistem saatini değiştirebilir ve oyunlaştırma (XP & Coin) ödül taşıdığı için istemciye güvenilemez.

- Sunucu yalnızca **olayları ve zaman damgalarını** saklar: `Started`, `Paused`, `Resumed`, `Extended`, `Completed`, `Abandoned`.
- Kalan süre her zaman `plannedEnd - serverNow` olarak hesaplanır; istemci sadece gösterir.
- Odak süresi = olaylar arasındaki aktif aralıkların toplamı (duraklatmalar düşülür).
- Aktif oturum durumu Redis'te (`session:active:{userId}`), kalıcı kayıt PostgreSQL'de tutulur.

---

## 1. Akış Koruması (Flow Shield)

Kullanıcı derin odaklanma (Flow State) halindeyken 25. dakikada zilin çalması konsantrasyonu baltalar. Sistem kullanıcıyı rahatsız etmeden esnek uzatma sunar:

```
Planlanan süre doldu
   -> Yumuşak ses (Tibet çanağı) + görsel arayüz rozeti:
      "Süre doldu. Akıştaysan devam et."
      [Molaya Geç]   [+10 dk Devam Et]   (60 sn yanıt yoksa)
   -> Yanıt yok: kullanıcı ayarına göre
        flowShield = on  -> otomatik +10 dk, en fazla MaxExtensions kez
        flowShield = off -> mola başlar
   -> Toplam odak MaxContinuousFocusMinutes (varsayılan 90) değerine ulaştı:
        uzatma kapanır, mola önerisi zorunlu hale gelir
```

- Varsayılanlar: `ExtensionMinutes = 10`, `MaxExtensions = 3`, `MaxContinuousFocusMinutes = 90`.

### 1.1 Kişisel Süre Önerisi
Son 14 gündeki tamamlanan oturumlardan:

$$D_{\text{önerilen}} = \text{medyan}\big(D_{\text{planlanan}} + D_{\text{uzatma}}\big) \quad \text{(yalnızca terk edilmeyen oturumlar)}$$

5'in katına yuvarlanır, `[15, 90]` aralığına sınırlanır. En az **10 tamamlanmış oturum** yoksa öneri yapılmaz.

---

## 2. Deneyim Puanı (XP) ve Seviye (Level) Eğrisi

Odaklanma süresi doğrudan karakterin gelişimine (Level) dönüşür:

### 2.1 XP Kazanım Formülü:
- **1 Dakika Net Odak = 10 XP**
- **Tamamlanan Mola Bonusu = 50 XP** (Sağlıklı dinlenme teşvik edilir)
- **Seri Bonusu:** Günlük seri çarpanı XP için de geçerlidir ($M_{\text{seri}}$).

### 2.2 Seviye Atlama Eşiği Formülü:
Seviye $L$'den $L+1$'e geçmek için gereken toplam kümülatif XP:

$$\text{RequiredXP}(L) = 150 \cdot L^{1.6}$$

| Seviye | Gerekli XP | Yaklaşık Net Odak | Kilidi Açılan Ödüller |
| :---: | :---: | :---: | :--- |
| **1** | 0 XP | 0 dk | Başlangıç odası, ahşap masa, temel kıyafetler |
| **5** | ~1,980 XP | ~3.3 saat | Retro Piksel Masa Lambası, 80'ler Vintage Poster |
| **10** | ~6,000 XP | ~10 saat | Piksel Plak Çalar & Çift Hoparlör, Kahve Kupası |
| **25** | ~26,000 XP | ~43 saat | Çatı Katı (Loft) Genişletilmiş Oda Planı, Neon Tabela |
| **50** | ~80,000 XP | ~133 saat | Çift Kişilik Çalışma Masası (Arkadaş Daveti Masası), Özel Hoodie |
| **100** | ~240,000 XP | ~400 saat | Altın Piksel Çalışma Masası, "Büyük Üstat" Avatar Rozeti |

---

## 3. Focus Coin (Odak Parası) Ekonomisi

Kullanıcılar odalarını dekore etmek ve kıyafet satın almak için **Focus Coin** kazanırlar:

$$\text{Coin} = \left\lfloor \frac{\text{netOdakDakikası}}{1} \times M_{\text{seri}} \times M_{\text{oda}} \right\rfloor$$

- **1 Dakika Net Odak = 1 Focus Coin** (25 dakikalık seans = 25 Coin).
- **Mola Bonusu:** Önerilen mola süresine sadık kalınırsa seans sonuna $+10$ Coin eklenir.
- **Terk Edilen Oturum:** 10 dakikanın altındaysa Coin verilmez; 10 dakikanın üzerindeyse net dakika kadar Coin verilir.
- **Günlük Tavan (Anti-Inflation Cap):** `DailyCoinCap = 400 Coin` (~6.6 saat odak). Tavan aşıldığında odak kaydedilir ancak ekonomiyi şişirmemek adına ek Coin verilmez.

### 3.1 Çarpanlar (Multipliers):
- **Seri Çarpanı ($M_{\text{seri}}$):**
  - 3 Gün Seri: $1.10\times$
  - 7 Gün Seri: $1.25\times$
  - 30 Gün Seri: $1.50\times$
- **Birlikte Çalışma Çarpanı ($M_{\text{oda}}$):** Odada bir arkadaşıyla eşzamanlı masaya oturup odaklanıldığında her iki tarafa $+%15$ Coin çarpanı ($1.15\times$).

### 3.2 Coin Defter Modeli (CoinLedger)
Kullanıcının bakiyesi veritabanında sadece tek bir sayı olarak güncellenmez; muhasebe defteri mantığında **yalnızca ekleme yapılan (`append-only`)** `CoinLedgerEntry` tablosuna yazılır:
- `SessionReward` (+Coin)
- `BreakComplianceReward` (+Coin)
- `ItemPurchase` (-Coin, mağazadan kıyafet veya mobilya alma)
- `AdminAdjustment` (+/- Coin)

Bakiye her zaman defter kayıtlarının toplamından doğrulanır. Bu model hileyi, veri tutarsızlığını ve bakiye manipülasyonunu %100 engeller.

---

## 4. Yorgunluk Skoru (Fatigue Score)

Kullanıcının aşırı çalışarak tükenmesini önlemek için 0-100 arasında hesaplanan günlük indeks:

$$F = 100 \cdot \text{clamp}\Big(w_1 \cdot \tfrac{C}{C_{\max}} + w_2 \cdot \tfrac{S}{S_{\max}} + w_3 \cdot \tfrac{T}{T_{\max}} + w_4 \cdot L,\ 0,\ 1\Big)$$

- $C$: En uzun kesintisiz odak (sınır $C_{\max} = 120$ dk).
- $S$: Atlanan / kısaltılan mola sayısı (sınır $S_{\max} = 4$).
- $T$: Günlük toplam odak süresi (sınır $T_{\max} = 360$ dk).
- $L$: Gece 00:00 - 05:00 arası odaklanma oranı (0 - 1).

Ağırlıklar: $w_1 = 0.35,\ w_2 = 0.25,\ w_3 = 0.25,\ w_4 = 0.15$.
- **0 - 39 (Yeşil):** İdeal tempo.
- **40 - 69 (Sarı):** Odadaki asistan ekranda nazik bir su ve esneme uyarısı gösterir.
- **70 - 100 (Kırmızı):** Flow Shield otomatik kapanır; 20 dakikalık dinlenme zorunlu önerilir.

---

## 5. Biyolojik Ritim ve Kronotip Sınıflandırma

Son 28 gündeki oturumlar 24 saatlik dilimlere ayrılarak incelenir:

$$E(h) = \frac{\text{tamamlanan}(h) + 1}{\text{başlatılan}(h) + 2} \cdot \ln\big(1 + \text{odakDakikası}(h)\big)$$

- **Sabah Kuşağı:** Zirve 05:00 - 11:00.
- **Gündüz Kuşağı:** Zirve 11:00 - 18:00.
- **Gece Kuşağı:** Zirve 18:00 - 04:00.

---

## 6. Hile Önleme ve Oyun İçi Güvenlik

| Saldırı / İstismar | Alınan Önlem |
| :--- | :--- |
| İstemci saatini ileri almak | Tüm süre ve ödül hesaplamaları sunucu saatinden (`serverNow`) yapılır. |
| Aynı anda birden fazla oturum açmak | Kullanıcı başına aynı anda tek aktif oturum (Redis dağıtık kilidi). |
| Sayacı açıp masadan uzaklaşmak | Günlük 400 Coin tavanı + Flow Shield'de 3 uzatmadan sonra onay zorunluluğu. |
| Sahte eşya satın alma isteği | Satın alma esnasında `CoinLedger` bakiyesi atomik olarak kontrol edilir ve düşülür. |
