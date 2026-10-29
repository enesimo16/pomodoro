"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  api,
  clearLegacyAuthStorage,
  describeError,
  loginAsGuest,
  type StudyRoomDto,
} from "@/lib/api";

export default function LandingHomePage() {
  const router = useRouter();
  const [authModalOpen, setAuthModalOpen] = useState(false);
  const [guestName, setGuestName] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [authError, setAuthError] = useState<string | null>(null);
  const [publicRooms, setPublicRooms] = useState<StudyRoomDto[] | null | undefined>(undefined);

  useEffect(() => {
    clearLegacyAuthStorage();
  }, []);

  useEffect(() => {
    let cancelled = false;
    api
      .get<StudyRoomDto[]>("/study-rooms/public")
      .then((rooms) => {
        if (!cancelled) setPublicRooms(rooms);
      })
      .catch(() => {
        if (!cancelled) setPublicRooms(null);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (!authModalOpen) return;
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") setAuthModalOpen(false);
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [authModalOpen]);

  const openGuestModal = () => {
    setAuthError(null);
    setAuthModalOpen(true);
  };

  const focusingCount = publicRooms
    ? publicRooms.reduce((sum, room) => sum + room.members.filter((m) => m.isFocusing).length, 0)
    : 0;

  const handleGuestSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setAuthError(null);
    try {
      await loginAsGuest(guestName.trim());
      clearLegacyAuthStorage();

      // Davet baglantisiyla gelindiyse oda ve davet kodu odaya aktarilir
      const incoming = new URLSearchParams(window.location.search);
      const forward = new URLSearchParams();
      for (const key of ["room", "invite"]) {
        const value = incoming.get(key);
        if (value) forward.set(key, value);
      }
      const query = forward.toString();
      router.push(query ? `/room?${query}` : "/room");
    } catch (err) {
      setAuthError(describeError(err));
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="flex-1 flex flex-col min-h-screen">
      {/* 1. ÜST GEZİNME ÇUBUĞU (NAVBAR) */}
      <header className="sticky top-0 z-40 bg-[#0a0d12]/90 backdrop-blur-sm border-b-2 border-[#262c36]">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 h-16 flex items-center justify-between">
          <Link href="/" className="flex items-center gap-3">
            <div className="w-7 h-7 bg-[#38ef7d] flex items-center justify-center font-bold text-black border-2 border-[#23c761] shadow-[2px_2px_0px_#040608]">
              ■
            </div>
            <span className="text-xl font-bold tracking-widest text-[#38ef7d] glow-green">
              BOKODORO
            </span>
          </Link>

          <nav className="hidden md:flex items-center gap-6 text-xs text-[#8b949e]">
            <a href="#nasil-calisir" className="hover:text-[#38ef7d] transition-colors">
              [ NASIL ÇALIŞIR? ]
            </a>
            <a href="#ozellikler" className="hover:text-[#38ef7d] transition-colors">
              [ ÖZELLİKLER ]
            </a>
            <a href="#arkadas-sistemi" className="hover:text-[#38ef7d] transition-colors">
              [ SESSİZ ORTAK ÇALIŞMA ]
            </a>
            <a href="#ekonomi" className="hover:text-[#38ef7d] transition-colors">
              [ EKONOMİ &amp; EŞYALAR ]
            </a>
          </nav>

          <div className="flex items-center gap-3">
            <button
              onClick={openGuestModal}
              className="pixel-btn pixel-btn-primary text-xs px-4 py-2"
            >
              Hemen Başla (Misafir)
            </button>
          </div>
        </div>
      </header>

      {/* 2. HERO BÖLÜMÜ (ANA MANŞET) */}
      <section className="relative overflow-hidden pt-12 pb-16 sm:pt-20 sm:pb-24 border-b-2 border-[#262c36]">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 flex flex-col lg:flex-row items-center gap-12">
          {/* Sol Kolon: Metin & CTA */}
          <div className="flex-1 flex flex-col gap-6 text-left">
            <div className="inline-flex items-center gap-2 px-3 py-1 bg-[#10141a] border border-[#30363d] text-xs text-[#38ef7d] w-fit">
              <span className="w-2 h-2 bg-[#38ef7d] animate-pulse"></span>
              RETRO PİKSEL DERİN ODAKLANMA EKOSİSTEMİ
            </div>

            <h1 className="text-3xl sm:text-5xl font-bold tracking-tight text-[#e6edf3] leading-[1.2]">
              Dikkat Dağınıklığını Yok Eden,{" "}
              <span className="text-[#38ef7d] glow-green">
                Piksel Çalışma Odan
              </span>{" "}
              ve Ses Kalkanın.
            </h1>

            <p className="text-sm sm:text-base text-[#8b949e] leading-relaxed">
              Bokodoro; sıradan ve ruhsuz 25/5 sayaçların ötesine geçer. Habbo tarzı
              özelleştirilebilir izometrik retro çalışma odası, 4 kanallı atmosferik ses
              mikseri, pencere hava durumu senkronu ve arkadaşlarınla sessizce odaklanabileceğin
              ortak çalışma masaları sunar.
            </p>

            <div className="flex flex-wrap items-center gap-4 pt-2">
              <button
                onClick={openGuestModal}
                className="pixel-btn pixel-btn-primary text-sm px-6 py-3.5 flex items-center gap-2"
              >
                <span>[ ODAYA GEÇ &gt; MİSAFİR MODU ]</span>
              </button>
              <Link
                href="/room"
                className="pixel-btn pixel-btn-secondary text-sm px-6 py-3.5 text-[#bacbb9] hover:text-white"
              >
                Canlı Önizleme
              </Link>
            </div>

            <div className="flex items-center gap-6 pt-4 text-xs text-[#8b949e]">
              <div className="flex items-center gap-2">
                <span className="text-[#38ef7d]">✓</span> Sıfır Kayıt / Anında Başla
              </div>
              <div className="flex items-center gap-2">
                <span className="text-[#38ef7d]">✓</span> Reklamsız &amp; Minimal
              </div>
              <div className="flex items-center gap-2">
                <span className="text-[#38ef7d]">✓</span> %100 Web Audio
              </div>
            </div>
          </div>

          {/* Sağ Kolon: Retro İzometrik Önizleme Kartı */}
          <div className="flex-1 w-full max-w-lg">
            <div className="pixel-box p-4 bg-[#10141a]">
              <div className="flex items-center justify-between border-b border-[#262c36] pb-2 mb-3 text-xs text-[#8b949e]">
                <span>{"// CANLI_ODA_ONIZLEMESI.RAW"}</span>
                <span className="text-[#38ef7d]">25:00 ODAK MODU</span>
              </div>

              <div className="h-64 bg-[#06080b] border border-[#21262d] relative flex items-center justify-center overflow-hidden">
                {/* Arka Pencere Yağmur Izgarası */}
                <div className="absolute top-3 left-6 right-6 h-28 border border-[#30363d] bg-[#0a1017] flex items-center justify-center">
                  <div className="absolute inset-0 opacity-40 bg-[radial-gradient(#38ef7d_1px,transparent_1px)] [background-size:8px_8px]"></div>
                  <span className="text-[10px] text-[#484f58] tracking-widest">{"// PENCERE"}</span>
                </div>

                {/* Karakter & Masa Kompozisyonu */}
                <div className="absolute bottom-4 flex flex-col items-center">
                  <div className="w-64 flex justify-between items-end px-3 mb-[-8px] z-10">
                    <div className="flex flex-col items-center">
                      <div className="w-5 h-3 bg-[#38ef7d] border border-[#23c761] shadow-[0_0_12px_#38ef7d]"></div>
                      <div className="w-1 h-5 bg-[#30363d]"></div>
                    </div>
                    <div className="w-8 h-6 bg-[#161b22] border border-[#30363d] flex items-center justify-center p-0.5">
                      <div className="w-5 h-5 rounded-full border border-[#484f58] bg-[#0a0d12] flex items-center justify-center animate-spin">
                        <div className="w-1.5 h-1.5 rounded-full bg-[#e5a93c]"></div>
                      </div>
                    </div>
                  </div>

                  {/* 8-bit Karakter */}
                  <div className="relative flex flex-col items-center z-20">
                    <div className="w-6 h-2 bg-[#2d1b0d] border-t border-x border-[#5c3a1e]"></div>
                    <div className="w-6 h-4 bg-[#f8d0b0] border-x border-[#c99f7d] flex items-center justify-center gap-1.5">
                      <div className="w-1 h-1 bg-[#0a0d12]"></div>
                      <div className="w-1 h-1 bg-[#0a0d12]"></div>
                    </div>
                    <div className="w-8 h-4 bg-[#1f3a2c] border-x border-[#38ef7d]/40 flex items-center justify-center">
                      <div className="w-2 h-2 bg-[#e5a93c]/80"></div>
                    </div>
                  </div>

                  {/* Ahşap Masa */}
                  <div className="w-64 h-4 bg-[#231a14] border-t-2 border-b-2 border-[#5c3a1e] mt-[-2px] shadow-[0_4px_0_#0a0604] z-30 flex items-center justify-between px-3">
                    <div className="w-1 h-1 bg-[#c99f7d]"></div>
                    <div className="text-[8px] text-[#8b6848] font-mono tracking-widest">{"// BOKO-DESK #01"}</div>
                    <div className="w-1 h-1 bg-[#c99f7d]"></div>
                  </div>
                </div>

                {focusingCount > 0 && (
                  <div className="absolute top-2 right-2 text-[9px] px-2 py-0.5 bg-[#10141a] border border-[#30363d] text-[#38ef7d]">
                    ● {focusingCount} KİŞİ ODAKTA
                  </div>
                )}
              </div>

              {/* Alt Bilgi Barı */}
              <div className="grid grid-cols-3 gap-2 mt-3 text-center text-[10px]">
                <div className="p-2 border border-[#262c36] bg-[#0d1117]">
                  <span className="text-[#8b949e] block">Sayaç</span>
                  <span className="text-[#38ef7d] font-bold">25:00 Pomodoro</span>
                </div>
                <div className="p-2 border border-[#262c36] bg-[#0d1117]">
                  <span className="text-[#8b949e] block">Akustik Kalkan</span>
                  <span className="text-[#e5a93c] font-bold">Yağmur + Lo-Fi</span>
                </div>
                <div className="p-2 border border-[#262c36] bg-[#0d1117]">
                  <span className="text-[#8b949e] block">Ödül</span>
                  <span className="text-[#e6edf3] font-bold">XP + Coin</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* 3. NASIL ÇALIŞIR? (3 ADIMDA DÖNGÜ) */}
      <section id="nasil-calisir" className="py-16 border-b-2 border-[#262c36] bg-[#0d1117]">
        <div className="max-w-6xl mx-auto px-4 sm:px-6">
          <div className="text-center max-w-2xl mx-auto mb-12">
            <span className="text-xs uppercase tracking-widest text-[#38ef7d]">RİTÜEL &amp; MEKANİK</span>
            <h2 className="text-2xl sm:text-3xl font-bold text-[#e6edf3] mt-2">
              Bokodoro Döngüsü Nasıl Çalışır?
            </h2>
            <p className="text-xs sm:text-sm text-[#8b949e] mt-2">
              Sadece butona basıp beklemek yok. Odaklanma bir çalışma ritüeline dönüşür.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {/* Adım 1 */}
            <div className="pixel-box p-6 flex flex-col gap-3">
              <div className="text-2xl font-bold text-[#38ef7d] font-pixel"># 01</div>
              <h3 className="text-base font-bold text-[#e6edf3]">Masaya Otur ve Lambayı Yak</h3>
              <p className="text-xs text-[#8b949e] leading-relaxed">
                Karakterin masaya geçer, retro çalışma lamban yanar. 25 dakikalık seans başlarken
                dış dünyayla olan bildirim akışın kilitlenir.
              </p>
            </div>

            {/* Adım 2 */}
            <div className="pixel-box p-6 flex flex-col gap-3">
              <div className="text-2xl font-bold text-[#38ef7d] font-pixel"># 02</div>
              <h3 className="text-base font-bold text-[#e6edf3]">4 Kanallı Kalkanı Aç</h3>
              <p className="text-xs text-[#8b949e] leading-relaxed">
                Pencereden gelen yağmur sesi, arka plan kahverengi gürültüsü, vinil çıtırtısı ve
                lo-fi plak çaları dilediğin seviyede karıştırarak dikkatini sabitle.
              </p>
            </div>

            {/* Adım 3 */}
            <div className="pixel-box p-6 flex flex-col gap-3">
              <div className="text-2xl font-bold text-[#38ef7d] font-pixel"># 03</div>
              <h3 className="text-base font-bold text-[#e6edf3]">XP &amp; Coin Kazan, Odanı Geliştir</h3>
              <p className="text-xs text-[#8b949e] leading-relaxed">
                Her tamamlanan seans sana XP ve Focus Coin kazandırır. Mağazadan yeni plaklar, neon
                tabelalar, retro konsollar ve kıyafetler alarak odanı genişlet.
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* 4. TEMEL ÖZELLİKLER & MİMARİ */}
      <section id="ozellikler" className="py-16 border-b-2 border-[#262c36]">
        <div className="max-w-6xl mx-auto px-4 sm:px-6">
          <div className="text-center max-w-2xl mx-auto mb-12">
            <span className="text-xs uppercase tracking-widest text-[#38ef7d]">SİSTEM MİMARİSİ</span>
            <h2 className="text-2xl sm:text-3xl font-bold text-[#e6edf3] mt-2">
              Neler Yapabilirsin?
            </h2>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
            {/* Kart 1 */}
            <div className="pixel-box p-5 flex flex-col gap-2">
              <div className="text-xl">🪟</div>
              <h4 className="text-sm font-bold text-[#e6edf3]">Canlı Pencere &amp; Hava Senkronu</h4>
              <p className="text-xs text-[#8b949e]">
                Bulunduğun şehrin gerçek hava durumuna göre odanın penceresinde yağmur yağar, sis
                oluşur veya kar birikir.
              </p>
            </div>

            {/* Kart 2 */}
            <div className="pixel-box p-5 flex flex-col gap-2">
              <div className="text-xl">🛡️</div>
              <h4 className="text-sm font-bold text-[#e6edf3]">Flow Shield (DEHB Kalkanı)</h4>
              <p className="text-xs text-[#8b949e]">
                Sekme değiştirdiğinde seni uyarır, hileli seansları engeller ve seans bölünmelerini
                minimuma indirir.
              </p>
            </div>

            {/* Kart 3 */}
            <div className="pixel-box p-5 flex flex-col gap-2">
              <div className="text-xl">📻</div>
              <h4 className="text-sm font-bold text-[#e6edf3]">Lo-Fi Plak Çalar &amp; Mikser</h4>
              <p className="text-xs text-[#8b949e]">
                Jamendo lisanslı binlerce telifsiz lo-fi parçayı odandaki piksel plak çalar üzerinden
                dinle.
              </p>
            </div>

            {/* Kart 4 */}
            <div className="pixel-box p-5 flex flex-col gap-2">
              <div className="text-xl">🤖</div>
              <h4 className="text-sm font-bold text-[#e6edf3]">Yapay Zekâ Koçu &amp; Vektör Bellek</h4>
              <p className="text-xs text-[#8b949e]">
                Hangi saatlerde en verimli olduğunu öğrenen, tükenmişlik durumunu teşhis eden kişisel
                çalışma asistanı.
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* 5. HABBO TARZI ARKADAŞ SİSTEMİ (CO-WORKING) */}
      <section id="arkadas-sistemi" className="py-16 border-b-2 border-[#262c36] bg-[#0d1117]">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 flex flex-col lg:flex-row items-center gap-10">
          <div className="flex-1 flex flex-col gap-4 text-left">
            <span className="text-xs uppercase tracking-widest text-[#38ef7d]">
              SOSYAL ODAKLANMA
            </span>
            <h2 className="text-2xl sm:text-4xl font-bold text-[#e6edf3]">
              Arkadaşlarınla Aynı Odada, Sessizce Yan Yana Çalışın.
            </h2>
            <p className="text-xs sm:text-sm text-[#8b949e] leading-relaxed">
              Kütüphane masası disiplinini dijitale taşıdık. Tek bir davet linkiyle arkadaşını kendi
              piksel odana davet et. Aynı odada yan masaya oturur; sesli sohbet veya dikkat dağıtıcı
              mesajlar yoktur, yalnızca odaklanma durumu ve sessiz piksel reaksiyonlar bulunur.
            </p>
            <ul className="text-xs text-[#bacbb9] space-y-2">
              <li className="flex items-center gap-2">
                <span className="text-[#38ef7d]">■</span> 4-8 Kişilik Sanal Çalışma Masaları
              </li>
              <li className="flex items-center gap-2">
                <span className="text-[#38ef7d]">■</span> Tek Tıkla WhatsApp / Telegram Davet Linki
              </li>
              <li className="flex items-center gap-2">
                <span className="text-[#38ef7d]">■</span> Dikkat Dağıtmayan Sessiz Piksel Emojileri
              </li>
              <li className="flex items-center gap-2">
                <span className="text-[#38ef7d]">■</span> Kim Kaç Dakikadır Çalışıyor Canlı İzleme
              </li>
            </ul>
          </div>

          <div className="flex-1 w-full max-w-md">
            <div className="pixel-box p-4 bg-[#10141a]">
              <div className="flex items-center justify-between border-b border-[#262c36] pb-2 mb-3 text-xs text-[#8b949e]">
                <span>{"// HERKESE_ACIK_ODALAR"}</span>
                <span className="text-[#38ef7d]">
                  {publicRooms ? `${focusingCount} kişi odakta` : ""}
                </span>
              </div>
              <div className="space-y-2">
                {publicRooms && publicRooms.length === 0 && (
                  <div className="p-2.5 border border-[#30363d] bg-[#06080b] text-xs text-[#8b949e]">
                    Şu an halka açık bir oda yok. Misafir olarak girip kendi odanı açabilir ve arkadaşını
                    davet edebilirsin.
                  </div>
                )}
                {publicRooms === null && (
                  <div className="p-2.5 border border-[#30363d] bg-[#06080b] text-xs text-[#8b949e]">
                    Oda listesi şu an alınamıyor.
                  </div>
                )}
                {publicRooms === undefined && (
                  <div className="p-2.5 border border-[#30363d] bg-[#06080b] text-xs text-[#8b949e]">
                    Odalar yükleniyor...
                  </div>
                )}
                {publicRooms?.slice(0, 3).map((room) => (
                  <div
                    key={room.code}
                    className="p-2.5 border border-[#30363d] bg-[#06080b] flex items-center justify-between text-xs"
                  >
                    <div className="flex items-center gap-2.5">
                      <div className="w-5 h-5 bg-[#161b22] border border-[#30363d] text-[#38ef7d] font-bold flex items-center justify-center text-[10px]">
                        #
                      </div>
                      <div>
                        <span className="text-[#e6edf3] font-bold block">{room.name}</span>
                        <span className="text-[10px] text-[#8b949e]">
                          {room.currentMemberCount} / {room.maxCapacity} kişi
                        </span>
                      </div>
                    </div>
                    <button
                      onClick={openGuestModal}
                      className="pixel-btn pixel-btn-primary text-[10px] px-2 py-1"
                    >
                      Otur
                    </button>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* 6. ALT CTA & FOOTER */}
      <section className="py-20 border-b-2 border-[#262c36] text-center bg-[#0a0d12]">
        <div className="max-w-4xl mx-auto px-4">
          <h2 className="text-3xl sm:text-4xl font-bold text-[#e6edf3]">
            Bugün derin odaklanmaya başla.
          </h2>
          <p className="text-xs sm:text-sm text-[#8b949e] mt-3 max-w-xl mx-auto">
            Kredi kartı gerekmez. Kayıt şartı yok. Tek bir tıkla misafir olarak odana geç ve ilk 25
            dakikanı başlat.
          </p>
          <div className="pt-6">
            <button
              onClick={openGuestModal}
              className="pixel-btn pixel-btn-primary text-sm px-8 py-4 text-black font-bold"
            >
              [ HEMEN ODAYA GİR &gt; MİSAFİR ]
            </button>
          </div>
        </div>
      </section>

      {/* FOOTER */}
      <footer className="py-8 bg-[#06080b] text-[11px] text-[#8b949e]">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 flex flex-col sm:flex-row items-center justify-between gap-4">
          <div>© 2026 BOKODORO. Bağımsız geliştirici odak platformu.</div>
          <div className="flex items-center gap-6">
            <Link href="/room" className="hover:text-[#38ef7d]">
              Çalışma Odası
            </Link>
          </div>
        </div>
      </footer>

      {/* ============================================================ */}
      {/* 7. RETRO PİKSEL MİSAFİR GİRİŞ MODALI */}
      {/* ============================================================ */}
      {authModalOpen && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 backdrop-blur-sm p-4"
          onClick={(e) => {
            if (e.target === e.currentTarget) setAuthModalOpen(false);
          }}
        >
          <div
            role="dialog"
            aria-modal="true"
            aria-labelledby="auth-dialog-title"
            className="pixel-box p-6 w-full max-w-md bg-[#10141a] relative"
          >
            <button
              type="button"
              onClick={() => setAuthModalOpen(false)}
              aria-label="Pencereyi kapat"
              className="absolute top-4 right-4 text-xs px-2 py-1 border border-[#30363d] text-[#8b949e] hover:text-white"
            >
              [X]
            </button>

            <div className="border-b border-[#262c36] pb-3 mb-4">
              <h2 id="auth-dialog-title" className="text-base font-bold text-[#38ef7d] block">
                BOKODORO // MİSAFİR GİRİŞİ
              </h2>
              <span className="text-xs text-[#8b949e]">Odaya geçmek için bir takma ad seç.</span>
            </div>

            <form onSubmit={handleGuestSubmit} className="flex flex-col gap-4">
              <div className="p-3 bg-[#06080b] border border-[#21262d] text-xs text-[#8b949e] leading-relaxed">
                <strong className="text-[#38ef7d]">Sıfır Sürtünme:</strong> Şifre veya e-posta gerekmez.
                Sadece bir takma ad seçin, hemen odanıza geçin.
              </div>

              <div className="flex flex-col gap-1">
                <label htmlFor="guest-name" className="text-xs text-[#8b949e]">
                  Karakter / Takma Ad:
                </label>
                <input
                  id="guest-name"
                  type="text"
                  required
                  autoFocus
                  maxLength={32}
                  placeholder="ör. PikselYolcusu"
                  value={guestName}
                  onChange={(e) => setGuestName(e.target.value)}
                  className="p-2.5 bg-[#06080b] border border-[#30363d] text-xs text-[#38ef7d] focus:border-[#38ef7d] outline-none font-mono"
                />
              </div>

              {authError && (
                <div role="alert" className="p-3 border border-[#f85149] bg-[#1a0d0d] text-xs text-[#ff7b72]">
                  {authError}
                </div>
              )}

              <button
                type="submit"
                disabled={isLoading}
                className="pixel-btn pixel-btn-primary text-xs py-3 text-black font-bold mt-2 disabled:opacity-60"
              >
                {isLoading ? "Odaya Bağlanılıyor..." : "[ ODAYA GİRİŞ YAP ]"}
              </button>

              <p className="text-[11px] text-[#484f58]">Google ile giriş yakında.</p>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
