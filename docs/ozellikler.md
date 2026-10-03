# Özellikler Matrisi ve Kapsam Dokümanı (docs/ozellikler.md)

> Bu doküman; platformun MVP (Faz 1), Genişleme (Faz 2), Gelir & Pro (Faz 3) ve Otonom AI (Faz 4) fazlarındaki net sınırlarını ve kullanıcı akışlarını tanımlar.
> Fikir ve inovasyon havuzu için bkz: [ekstra-ozellikler.md](ekstra-ozellikler.md).

---

## 1. Faz Bazlı Özellik Matrisi

| Modül | Faz 1 (MVP) | Faz 2 (Sosyal & Genişleme) | Faz 3 (Pro & Gelir) | Faz 4 (Otonom AI & Mobil) |
| :--- | :--- | :--- | :--- | :--- |
| **Sayaç (Timer)** | Sunucu otoriteli 25/50 dk, duraklat/sürdür, sesli Tibet çanağı uyarısı | Akış Koruması (Flow Shield, +10 dk otomatik uzatma), kişisel süre önerisi | Özel süre belirleme, Ters Pomodoro modu | DEHB Odak Kalkanı modu, biyolojik nabız senkronu |
| **Ses (Soundscape)** | 4 kanallı mikser: 8 küratörlü lofi, 12 ambiyans, Web Audio prosedürel brown/pink gürültü | Kullanıcı özel miks preset'leri kaydetme, hava durumu senkron miksi | Spotify & YouTube Music Companion widget | Prompt ile jeneratif AI ambient üretimi (Stable Audio) |
| **Tema (Visuals)** | 5 HD döngüsel video (Pixabay lisanslı) + Zen OLED siyah modu | 15+ tema (yağmur, kafe, piksel odalar), otomatik hava durumu teması | Pro özel temalar, sirkadiyen dinamik ışık döngüsü | Prompt ile özel tema üretimi (FLUX.1 Schnell) |
| **Kimlik & Profil** | Sıfır sürtünmeli Misafir Modu, Google & GitHub OAuth | Discord, Apple ID girişi, şifresiz sihirli bağlantı (Magic Link) | Cihazlar arası kesintisiz oturum yönetimi | Mobil biyometrik giriş (FaceID / Fingerprint) |
| **Oyunlaştırma (Avatar & Oda)** | Net dakika bazlı Focus Coin & XP, başlangıç piksel odası ve eşyaları, seviye atlama | Kıyafet & mobilya mağazası, Seri takibi (Streak) + Seri Dondurucu (Freeze) | Ortak Piksel Çalışma Odası (Sessiz çalışma masaları, eşzamanlı odak coin bonusu) | Global Sezonluk Etkinlikler & Özel Eşyalar (Topluluk meydan okumaları) |
| **Yapay Zekâ (Masa Terminali)**| Kural tabanlı ergonomi ve mola uyarıları (0 ms gecikme) | Günlük yorgunluk skoru (0-100), kronotip tespiti (Sabah/Gündüz/Gece) | Asenkron kişiselleştirilmiş AI mesajları, haftalık odak karnesi | pgvector RAG hafızalı tam otonom derin verimlilik koçu |
| **Yönetim (Admin)** | Razor Pages: Tema/Ses yükleme & etiketleme, kullanıcı listesi | Aktif oturum monitörü, telif ve lisans denetim paneli | Gelir & abonelik analitiği, AI token maliyet takibi | Otomatik ffmpeg medya işleme hattı izleme |
| **Entegrasyonlar** | Open-Meteo canlı hava durumu | Haftalık estetik paylaşım kartı (Instagram/X Focus Wrapped) | Todoist / Notion çift yönlü senkronizasyonu | Chrome Site Engelleyici, Mobil Uygulama (iOS/Android) |

---

## 2. Üyelik Kademeleri (Free vs. Pro)

```
[Ücretsiz Katman (Free)]
  ├── Sunucu otoriteli Pomodoro & Mola Sayacı
  ├── 5 Canlı Video Tema + Zen Siyah Modu
  ├── 4 Kanallı Mikser (8 Temel Müzik + 12 Ambiyans + Prosedürel Gürültüler)
  ├── 3 Özel Miks Preset'i Kaydetme
  ├── Misafir Modu & Google / GitHub Girişi
  ├── Son 7 Günlük Odaklanma Geçmişi & Temel Coin/XP Cüzdanı
  ├── Haftada 1 Gün Seri Dondurucu (Streak Freeze)
  └── Açık Piksel Çalışma Odalarına Katılım (Sessiz Çalışma)

[Pro Katman (Ücretli / Aylık / Yıllık)]
  ├── Tüm Kütüphane: 30+ Canlı Tema & Tüm Müzik/Ambiyans Parçaları
  ├── Sınırsız Miks Preset'i Kaydetme
  ├── Gelişmiş Akış Koruması (Flow Shield) & Kişisel Süre Önerisi
  ├── Tam Zamanlı AI Koçu (Kişiselleştirilmiş tavsiyeler, yorgunluk analizi)
  ├── Sınırsız Tarihçe & 12 Haftalık Yoğunluk Haritası (Heatmap)
  ├── Kronotip Tespiti & Haftalık Estetik Karne (Focus Wrapped)
  ├── Haftada 2 Gün Seri Dondurucu
  ├── Özel Şifreli Piksel Çalışma Odası Açma & Arkadaşını Odana Davet Etme
  ├── Spotify / YouTube Music Companion Widget'ı
  └── Faz 4'te: Aylık AI Tema & Müzik Üretim Kredisi
```

---

## 3. Temel Kullanıcı Akışları (User Journeys)

### 3.1 İlk Ziyaretçi (Sıfır Sürtünmeli Deneyim):
1. Kullanıcı `focus.app` adresine girer.
2. Form, şifre, kredi kartı sorulmaz. Arka planda `POST /api/v1/auth/guest` çağrılır; misafir hesabı, varsayılan piksel odası ve temel avatarı anında oluşturulur.
3. Sakinleştirici bir yağmur teması ve fısıltılı bir lofi melodisi eşliğinde piksel çalışma masasına oturur ve büyük "Odaklanmaya Başla" butonuna tıklar.
4. Tıklamayla birlikte Web Audio API uyandırılır (`AudioContext.resume()`), 25 dakikalık sayaç sunucu damgasıyla başlar.
5. Seans başarıyla bittiğinde cüzdana ilk Focus Coin ve XP düşer, ekranda nazik bir öneri belirir:  
   *"İlk odak seansını tamamladın ve 25 Focus Coin kazandın! Karakterini ve odanı kaybetmemek için tek tıkla Google ile bağla."*

### 3.2 Sosyal Piksel Oda (Birlikte Odaklanma Deneyimi):
1. Kullanıcı arkadaşına oda davet linki gönderir (`focus.app/room/join?code=XYZ`).
2. Arkadaşı odaya girer, boş bir çalışma masasına veya koltuğa oturur.
3. Biri sayacı başlattığında masasında çalışma lambası yanar (`MemberStatusChanged`).
4. Birlikte çalışılan dakikalarda her ikisi de $+%15$ ortak çalışma coin bonusu kazanır.
5. Seans sonunda kutlama piksel emojisi veya havai fişek fırlatılır (`SendReaction: 🎉`).
