# Yapay Zekâ Ajan Sistemi (docs/agent-sistemi.md)

> Oda içi akıllı asistanın (Focus Coach / Terminal) mimarisi, tetikleyici kural motoru, Redis + pgvector çift katmanlı bellek yapısı, maliyet kontrolü ve güvenlik sınırları.
> İlgili: [algoritmalar.md](algoritmalar.md), [veri-modeli.md](veri-modeli.md), [riskler-ve-oneriler.md](riskler-ve-oneriler.md).

---

## 1. Kimlik ve Oda İçi Konumlandırma

- **Rol:** Kullanıcının çalışma masasında yer alan retro bir bilgisayar terminali veya akıllı radyo biçiminde canlanan **Kişisel Odaklanma Koçu**.
- **Karakter & Ton:** Sıcak, bilgece, teşvik edici ve kısa (maksimum 2-3 cümle).
- **Hukuki Sınır:** Tıbbi bir unvan ("Doktor / Hekim") kullanmaz. Teşhis koymaz, reçete yazmaz. Açık feragatname içerir: *"Focus bir tıbbi araç değildir; odaklanma alışkanlıkları koçudur."*

---

## 2. Mimari Kural: Ajan Asla İstek Yolunda Değildir

Canlı testlerde ölçülen 3-11 saniyelik LLM gecikmesi nedeniyle:

- Sayaç başlatma, tamamlama veya masaya oturma işlemleri **asla LLM'i beklemez**.
- Ajan mesajları arka plan görevlerinde (Hangfire) üretilir ve kullanıcıya **SignalR (`TimerHub`)** üzerinden sessizce iletilir.
- Mola ve yorgunluk kararları C# kural motoruyla **0 milisaniyede** hesaplanır; LLM yalnızca mesajın dilini kişiselleştirmek için devreye girer.

---

## 3. Tetikleyici Kural Motoru (Proaktif Uyarılar)

| Tetikleyici Durum | Koşul | Ajan Eylemi | Günlük Sınır |
| :--- | :--- | :--- | :--- |
| **Aşırı Kesintisiz Çalışma** | Masada aralıksız 75+ dakika oturulması | *"75 dakikadır ekrandasın. Lütfen ayağa kalk, bir bardak su iç ve gözlerini dinlendir."* | Seans başına 1 |
| **Yüksek Yorgunluk Skoru** | `FatigueScore >= 70` | Flow Shield'ı kapatır, 20 dakikalık uzun mola önerir. | Günde 2 |
| **Sirkadiyen Düşüş Saati** | Kullanıcının verimsiz olduğu saatte seans açılması | *"Bu saatlerde dikkat dağınıklığın artıyor. Hafif bir okuma göreviyle başlayalım mı?"* | Günde 1 |
| **Geri Dönüş Karşılaması** | 3+ gün aradan sonra masaya ilk oturuş | *"Tekrar hoş geldin! Isınmak için 20 dakikalık hafif bir seans yapalım."* | Olay başına 1 |
| **Haftalık Özet Kartı** | Pazar akşamları | Haftalık XP, tamamlanan seans ve zirve verim saatleri özeti. | Haftada 1 |

*Kullanıcı başına günlük proaktif mesaj tavanı en fazla 4'tür.*

---

## 4. Çift Katmanlı Bellek Mimarisi (Memory System)

```
[Ajan Bellek Mimarisi]
   ├── Kısa Süreli Bellek (Redis, TTL 24 saat)
   │     └── Güncel oturum bağlamı, bugünkü seans notları, anlık yorgunluk skoru.
   │
   └── Uzun Süreli Bellek (PostgreSQL + pgvector)
         └── AgentMemory tablosu: Kullanıcının alışkanlıkları, hedefleri ve kronotip profili
             768 boyutlu vektörel gömme (embedding) olarak saklanır (HNSW indeks).
```

---

## 5. Güvenlik ve Token Maliyet Koruması

1. **Prompt Injection Savunması:** Kullanıcı oturum hedefine ne yazarsa yazsın, bu metin LLM'e sadece `user` rolünde parametre olarak verilir; sistem kuralları (`system prompt`) manipüle edilemez.
2. **Kullanıcı Başına Günlük Token Bütçesi:** `Ai__DailyTokenBudgetPerUser: 20000` token sınırı ve Redis tabanlı hız sınırlaması (Rate Limiting).
