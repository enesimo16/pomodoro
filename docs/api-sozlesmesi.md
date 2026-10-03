# API Sözleşmesi (docs/api-sozlesmesi.md)

> Backend ile frontend arasındaki ortak REST ve SignalR sözleşmesi.
> Habbo tarzı piksel oda, avatar özelleştirme, mağaza, mobilya yerleşimi ve ortak çalışma uçlarını tanımlar.

---

## 1. Genel Kurallar

| Konu | Kural |
| :--- | :--- |
| Taban Yol | `/api/v1` |
| Format | JSON, `camelCase`, zamanlar ISO 8601 UTC |
| Kimlik | Çerez (`focus_at`), `credentials: "include"` |
| CSRF | `POST/PUT/PATCH/DELETE` isteklerinde `X-CSRF-Token` başlığı |
| Hata Formatı | RFC 9457 `ProblemDetails` |

---

## 2. REST Uç Noktaları

### 2.1 Kimlik & Misafir
| Metot | Yol | Açıklama |
| :--- | :--- | :--- |
| POST | `/auth/guest` | Tek tıkla misafir piksel karakter ve başlangıç odası oluşturur |
| GET | `/auth/external/{provider}` | Google / GitHub OAuth başlatma |
| POST | `/auth/logout` | Çıkış yapma |

### 2.2 Avatar (Piksel Karakter)
| Metot | Yol | Gövde / Açıklama |
| :--- | :--- | :--- |
| GET | `/avatar` | Kullanıcının aktif avatar özellikleri ve giydiği kıyafetler |
| PATCH | `/avatar` | `{ skinTone, hairStyle, hairColor, topItemId?, bottomItemId?, hatItemId?, glassesItemId? }` |

### 2.3 Piksel Oda & Mobilya Yerleşimi
| Metot | Yol | Gövde / Açıklama |
| :--- | :--- | :--- |
| GET | `/room` | Kullanıcının kendi odası, duvar/zemin tasarımı ve yerleştirilmiş mobilyalar |
| GET | `/room/{id}` | Başka bir kullanıcının veya arkadaşının odasını ziyaret etme |
| POST | `/room/items` | Odaya mobilya yerleştir: `{ catalogItemId, gridX, gridY, rotation }` |
| PUT | `/room/items/{id}` | Mobilyanın yerini veya yönünü değiştir: `{ gridX, gridY, rotation }` |
| DELETE | `/room/items/{id}` | Mobilyayı odadan kaldırıp envantere geri koy |
| PATCH | `/room/theme` | Duvar kağıdı ve zemin kaplamasını değiştir: `{ wallpaperCatalogId, floorCatalogId }` |

### 2.4 Mağaza & Envanter (Shop & Inventory)
| Metot | Yol | Gövde / Açıklama |
| :--- | :--- | :--- |
| GET | `/shop/catalog` | Mağazadaki mobilya ve kıyafetler (`category`, `tier` filtreli) |
| POST | `/shop/buy` | `{ catalogItemId }` (Focus Coin bakiyesinden düşer, envantere ekler) |
| GET | `/inventory` | Kullanıcının sahip olduğu tüm eşyalar ve kıyafetler |
| GET | `/coins/balance` | Mevcut Coin bakiyesi, XP ve Seviye bilgisi |

### 2.5 Odaklanma Seansı (Masaya Oturma & Sayaç)
| Metot | Yol | Gövde / Açıklama |
| :--- | :--- | :--- |
| GET | `/sessions/active` | Aktif seans durumu |
| POST | `/sessions` | Masaya otur ve seansı başlat: `{ plannedMinutes, themeId?, mixPresetId? }` |
| POST | `/sessions/{id}/pause` | Seansı duraklat |
| POST | `/sessions/{id}/resume` | Seansı sürdür |
| POST | `/sessions/{id}/extend` | Flow Shield uzatması: `{ minutes: 10 }` |
| POST | `/sessions/{id}/complete` | Seansı tamamla (XP ve Coin ödülü döner) |
| POST | `/sessions/{id}/abandon` | Masadan kalk ve seansı iptal et |

### 2.6 Ses & Plak Çalar
| Metot | Yol | Açıklama |
| :--- | :--- | :--- |
| GET | `/sounds` | Plak çalar müzik ve doğa sesleri kataloğu |
| GET | `/mixes` | Kullanıcının kayıtlı 4 kanallı miks preset'leri |
| POST | `/mixes` | Yeni miks preset'i kaydet |

---

## 3. Gerçek Zamanlı İletişim (SignalR)

### 3.1 `TimerHub` (`/hubs/timer`)
Kişisel seans olayları:
- Sunucu -> İstemci: `SessionChanged(activeSession)`
- Sunucu -> İstemci: `SessionCompleted(xpEarned, coinsEarned, levelUp)`
- Sunucu -> İstemci: `DeskLightToggled(isOn)`

### 3.2 `RoomHub` (`/hubs/room`)
Habbo tarzı ortak çalışma ve oda etkileşimleri:
| Yön | Olay / Metot | Açıklama |
| :--- | :--- | :--- |
| İstemci -> Sunucu | `JoinRoom(roomId)` | Odaya avatar olarak giriş yap |
| İstemci -> Sunucu | `SitAtDesk(deskId)` | Odadaki bir masaya otur |
| İstemci -> Sunucu | `LeaveDesk()` | Masadan kalk |
| İstemci -> Sunucu | `SendReaction(emoji)` | Odadaki arkadaşlara kahve veya kalp emojisi fırlat |
| Sunucu -> İstemci | `UserEnteredRoom` | Odaya yeni bir avatar girdi |
| Sunucu -> İstemci | `UserLeftRoom` | Avatar odadan ayrıldı |
| Sunucu -> İstemci | `UserStatusChanged` | Avatar masaya oturdu / odaklanıyor / mola verdi |
| Sunucu -> İstemci | `RoomItemUpdated` | Ev sahibi bir mobilyanın yerini değiştirdi |
| Sunucu -> İstemci | `ReactionReceived` | Bir arkadaşından tepki geldi |
