# Bokodoro Web İstemcisi

Next.js (App Router, TypeScript, Tailwind CSS) ile yazılmış Bokodoro arayüzü. Sayaç, ses mikseri (Web Audio ile
tarayıcıda sentezlenir) ve ortak çalışma odaları `Focus.WebAPI` ile konuşur.

## Çalıştırma

```bash
cp .env.example .env.local
npm ci
npm run dev
```

Geliştirme sunucusu http://localhost:3000 adresinde açılır. Backend'in (`docker compose up focus-api`) çalışıyor ve
`Cors:AllowedOrigins` içinde bu adresi içeriyor olması gerekir; oturum HttpOnly çerezlerle yürütülür.

## Ortam değişkeni

| Değişken | Açıklama | Varsayılan |
| :--- | :--- | :--- |
| `NEXT_PUBLIC_API_URL` | API taban adresi (`/api/v1` dahil). Derleme zamanında gömülür. | `http://localhost:5000/api/v1` |

## Komutlar

| Komut | Açıklama |
| :--- | :--- |
| `npm run dev` | Geliştirme sunucusu |
| `npm run lint` | ESLint |
| `npm run build` | Üretim derlemesi (`output: "standalone"`) |

## Docker

```bash
docker build -t focus-web --build-arg NEXT_PUBLIC_API_URL=http://localhost:5000/api/v1 .
```

Kök dizindeki `docker-compose.yml` içinde `focus-web` servisi aynı imajı 3000 portunda çalıştırır.
