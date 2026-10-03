# Kullanıcı ve Kimlik Doğrulama Sistemi (docs/kullanici-sistemi.md)

> Misafir piksel karakter oluşturma, OAuth 2.0 sosyal girişleri, hesap birleştirme (migration) ve HttpOnly çerez güvenliği.
> İlgili: [api-sozlesmesi.md](api-sozlesmesi.md), [veri-modeli.md](veri-modeli.md).

---

## 1. Sıfır Sürtünmeli Başlangıç (Guest Session Modu)

Kullanıcı siteye girdiği an kayıt formuyla boğuşmaz:

1. **Misafir Karakter ve Oda Oluşturma:**
   - İstemci `POST /api/v1/auth/guest` çağırır.
   - Sunucu anında `IsGuest = true` işaretli bir `User`, başlangıç kıyafetli bir `UserAvatar` ve temel eşyalı bir `PixelRoom` oluşturur.
   - Güvenli `HttpOnly` çerezi (`focus_at`) tarayıcıya bırakılır.
2. **Kalıcı Hesaba Geçiş (Account Claiming / Migration):**
   - Misafir kullanıcı odaklanır, XP kazanır, odasına birkaç mobilya yerleştirir.
   - "Google ile Giriş Yap" dediğinde OAuth doğrulaması tamamlanır.
   - Misafir oturumundaki tüm mobilyalar, oda yerleşimi, envanter, XP ve Focus Coin bakiyesi kullanıcının kalıcı hesabına aktarılır (`IsGuest = false`). Tek bir piksel eşya dahi kaybolmaz.

---

## 2. Kimlik Sağlayıcıları (OAuth 2.0)

| Sağlayıcı | Kitle | Not |
| :--- | :--- | :--- |
| **Google** | Tüm Kullanıcılar | En yaygın, tek tıkla katılım. |
| **GitHub** | Yazılımcılar & Mühendisler | Kod yazarken odaklanan kitle. |
| **Discord** | Öğrenciler & Genç Çalışma Grupları | Odasını arkadaşlarıyla paylaşacak topluluklar. |
| **Apple ID** | Mac & iOS Kullanıcıları | Mobil öncesi ekosistem standardı. |

---

## 3. Token ve Çerez Güvenlik Mimarisi

- **Access Token:** Kısa ömürlü JWT (15 dakika), `HttpOnly; Secure; SameSite=Lax` çerez içinde taşınır. JavaScript tarafından okunamaz (XSS koruması).
- **Refresh Token:** 30 günlük opak değer, veritabanında SHA-256 hash olarak tutulur. Her kullanımda yenilenir (Refresh Token Rotation).
- **CSRF Koruması:** `SameSite=Lax` politikası ve durum değiştiren isteklerde `X-CSRF-Token` başlığı doğrulaması.
- **Admin Paneli Yalıtımı:** Razor Pages yönetim paneli son kullanıcı OAuth'undan tamamen bağımsızdır; iki faktörlü (TOTP) kimlik doğrulamaya sahiptir ve yalnızca yerel ağa (`127.0.0.1:5001`) açıktır.
