"use client";

import { useState, useEffect, useRef, Suspense } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";

function RoomContent() {
  const searchParams = useSearchParams();
  const [userName, setUserName] = useState<string>("Misafir Sakin");
  const [userLevel, setUserLevel] = useState<number>(1);
  const [inviteCode, setInviteCode] = useState<string>("BOKO-99");
  const [copiedInvite, setCopiedInvite] = useState(false);

  // Sayaç Durumu
  const [secondsLeft, setSecondsLeft] = useState(25 * 60);
  const [isRunning, setIsRunning] = useState(false);
  const [activeMode, setActiveMode] = useState<"pomodoro" | "shortBreak" | "longBreak">("pomodoro");
  const [roundCount, setRoundCount] = useState(1);

  // 4 Kanallı Ses Mikseri
  const [soundRain, setSoundRain] = useState(40);
  const [soundNoise, setSoundNoise] = useState(25);
  const [soundVinyl, setSoundVinyl] = useState(15);
  const [soundLofi, setSoundLofi] = useState(60);
  const [audioStarted, setAudioStarted] = useState(false);

  // Web Audio Context Referansı
  const audioCtxRef = useRef<AudioContext | null>(null);
  const rainGainRef = useRef<GainNode | null>(null);
  const noiseGainRef = useRef<GainNode | null>(null);
  const vinylGainRef = useRef<GainNode | null>(null);

  // Kullanıcı ve Oda Bilgilerini Yükleme
  useEffect(() => {
    const qGuest = searchParams.get("guest");
    if (qGuest) {
      setUserName(decodeURIComponent(qGuest));
    }

    if (typeof window !== "undefined") {
      const stored = localStorage.getItem("bokodoro_auth");
      if (stored) {
        try {
          const parsed = JSON.parse(stored);
          if (parsed.user?.displayName) setUserName(parsed.user.displayName);
          if (parsed.user?.level) setUserLevel(parsed.user.level);
          if (parsed.room?.inviteCode) setInviteCode(parsed.room.inviteCode);
        } catch {}
      }
    }
  }, [searchParams]);

  // Sayaç Zamanlayıcısı
  useEffect(() => {
    let interval: NodeJS.Timeout | null = null;
    if (isRunning && secondsLeft > 0) {
      interval = setInterval(() => {
        setSecondsLeft((prev) => prev - 1);
      }, 1000);
    } else if (secondsLeft === 0) {
      setIsRunning(false);
      if (activeMode === "pomodoro") {
        setRoundCount((prev) => (prev < 4 ? prev + 1 : 1));
      }
    }
    return () => {
      if (interval) clearInterval(interval);
    };
  }, [isRunning, secondsLeft, activeMode]);

  // Web Audio Sentezleyici Motoru (Tarayıcı İçi Telifsiz Ses Üretimi)
  const initAudioEngine = () => {
    if (audioCtxRef.current) return;
    try {
      const AudioCtx = window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
      const ctx = new AudioCtx();
      audioCtxRef.current = ctx;

      // 1. Kahverengi Gürültü (Brown Noise) Üretimi
      const bufferSize = ctx.sampleRate * 2;
      const brownBuffer = ctx.createBuffer(1, bufferSize, ctx.sampleRate);
      const data = brownBuffer.getChannelData(0);
      let lastOut = 0.0;
      for (let i = 0; i < bufferSize; i++) {
        const white = Math.random() * 2 - 1;
        data[i] = (lastOut + 0.02 * white) / 1.02;
        lastOut = data[i];
        data[i] *= 3.5;
      }
      const noiseSource = ctx.createBufferSource();
      noiseSource.buffer = brownBuffer;
      noiseSource.loop = true;

      const nGain = ctx.createGain();
      nGain.gain.value = soundNoise / 100 * 0.15;
      noiseGainRef.current = nGain;

      noiseSource.connect(nGain);
      nGain.connect(ctx.destination);
      noiseSource.start();

      // 2. Yağmur Sesi Filtresi
      const rainBuffer = ctx.createBuffer(1, bufferSize, ctx.sampleRate);
      const rData = rainBuffer.getChannelData(0);
      for (let i = 0; i < bufferSize; i++) {
        rData[i] = (Math.random() * 2 - 1) * 0.1;
      }
      const rainSource = ctx.createBufferSource();
      rainSource.buffer = rainBuffer;
      rainSource.loop = true;

      const rFilter = ctx.createBiquadFilter();
      rFilter.type = "lowpass";
      rFilter.frequency.value = 800;

      const rGain = ctx.createGain();
      rGain.gain.value = soundRain / 100 * 0.2;
      rainGainRef.current = rGain;

      rainSource.connect(rFilter);
      rFilter.connect(rGain);
      rGain.connect(ctx.destination);
      rainSource.start();

      setAudioStarted(true);
    } catch {}
  };

  // Ses Seviyelerini Güncelleme
  useEffect(() => {
    if (noiseGainRef.current && audioCtxRef.current) {
      noiseGainRef.current.gain.setValueAtTime(
        isRunning ? (soundNoise / 100) * 0.15 : 0,
        audioCtxRef.current.currentTime
      );
    }
  }, [soundNoise, isRunning]);

  useEffect(() => {
    if (rainGainRef.current && audioCtxRef.current) {
      rainGainRef.current.gain.setValueAtTime(
        isRunning ? (soundRain / 100) * 0.2 : 0,
        audioCtxRef.current.currentTime
      );
    }
  }, [soundRain, isRunning]);

  const handleTogglePlay = () => {
    if (!audioStarted) {
      initAudioEngine();
    } else if (audioCtxRef.current && audioCtxRef.current.state === "suspended") {
      audioCtxRef.current.resume();
    }
    setIsRunning(!isRunning);
  };

  const formatTime = (totalSeconds: number) => {
    const mins = Math.floor(totalSeconds / 60);
    const secs = totalSeconds % 60;
    return `${mins.toString().padStart(2, "0")}:${secs.toString().padStart(2, "0")}`;
  };

  const handleModeSwitch = (mode: "pomodoro" | "shortBreak" | "longBreak") => {
    setIsRunning(false);
    setActiveMode(mode);
    if (mode === "pomodoro") setSecondsLeft(25 * 60);
    if (mode === "shortBreak") setSecondsLeft(5 * 60);
    if (mode === "longBreak") setSecondsLeft(15 * 60);
  };

  const copyInvite = () => {
    if (typeof navigator !== "undefined") {
      navigator.clipboard.writeText(
        `${window.location.origin}/room?invite=${inviteCode}`
      );
      setCopiedInvite(true);
      setTimeout(() => setCopiedInvite(false), 2000);
    }
  };

  return (
    <div className="flex-1 flex flex-col justify-between max-w-6xl w-full mx-auto p-4 sm:p-6 lg:p-8">
      {/* 1. ÜST BAR (NAVBAR) */}
      <header className="flex flex-wrap items-center justify-between border-b-2 border-[#262c36] pb-4 mb-6 gap-4">
        <div className="flex items-center gap-3">
          <Link
            href="/"
            className="pixel-btn pixel-btn-secondary text-xs px-2.5 py-1.5 text-[#8b949e] hover:text-[#38ef7d] flex items-center gap-1.5"
            title="Ana Sayfaya Dön"
          >
            <span>&lt;</span>
            <span className="hidden sm:inline">ANA SAYFA</span>
          </Link>

          <div className="w-7 h-7 bg-[#38ef7d] flex items-center justify-center font-bold text-black border-2 border-[#23c761] shadow-[2px_2px_0px_#040608]">
            ■
          </div>
          <span className="text-xl sm:text-2xl font-bold tracking-widest text-[#38ef7d] glow-green">
            BOKODORO
          </span>
          <span className="text-[10px] uppercase tracking-widest px-2 py-0.5 border border-[#30363d] text-[#38ef7d] bg-[#10141a]">
            ODA #{inviteCode}
          </span>
        </div>

        <nav className="flex items-center gap-2 sm:gap-4 text-xs">
          {/* Kullanıcı Profili Rozeti */}
          <div className="px-3 py-1.5 bg-[#10141a] border border-[#30363d] flex items-center gap-2 text-[#e6edf3]">
            <span className="w-2 h-2 bg-[#38ef7d]"></span>
            <span className="font-bold">{userName}</span>
            <span className="text-[10px] px-1.5 py-0.2 bg-[#161b22] text-[#e5a93c] border border-[#30363d]">
              LVL.{userLevel}
            </span>
          </div>

          {/* Davet Linki Kopyala Butonu */}
          <button
            onClick={copyInvite}
            className="pixel-btn pixel-btn-secondary text-xs px-3 py-1.5 text-[#bacbb9] hover:text-white"
          >
            {copiedInvite ? "[ KOPYALANDI! ]" : "[ + ARKADAŞINI ÇAĞIR ]"}
          </button>

          <a
            href="http://localhost:5001/?admin_key=focus_master_key_9971x"
            target="_blank"
            rel="noreferrer"
            className="pixel-btn pixel-btn-secondary text-xs px-3 py-1.5 text-[#8b949e] hover:text-[#38ef7d] hidden md:inline-block"
          >
            Admin
          </a>
        </nav>
      </header>

      {/* 2. MERKEZ ODA & SAYAÇ IZGARASI */}
      <main className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start my-auto">
        {/* SOL: SAYAÇ & ODAK KONTROL PANELİ (7 Sütun) */}
        <section className="lg:col-span-7 flex flex-col gap-6">
          {/* Seans Mod Seçici */}
          <div className="flex gap-2">
            <button
              onClick={() => handleModeSwitch("pomodoro")}
              className={`pixel-btn text-xs px-4 py-2 flex-1 ${
                activeMode === "pomodoro" ? "pixel-btn-primary" : "pixel-btn-secondary"
              }`}
            >
              Odaklanma (25d)
            </button>
            <button
              onClick={() => handleModeSwitch("shortBreak")}
              className={`pixel-btn text-xs px-4 py-2 flex-1 ${
                activeMode === "shortBreak" ? "pixel-btn-amber" : "pixel-btn-secondary"
              }`}
            >
              Kısa Mola (5d)
            </button>
            <button
              onClick={() => handleModeSwitch("longBreak")}
              className={`pixel-btn text-xs px-4 py-2 flex-1 ${
                activeMode === "longBreak" ? "pixel-btn-amber" : "pixel-btn-secondary"
              }`}
            >
              Uzun Mola (15d)
            </button>
          </div>

          {/* Retro LED Zaman Sayacı Kartı */}
          <div className="pixel-box p-6 sm:p-10 flex flex-col items-center justify-center text-center relative overflow-hidden">
            {/* Terminal Pencere Başlığı */}
            <div className="w-full flex items-center justify-between border-b border-[#262c36] pb-3 mb-6 text-xs text-[#8b949e]">
              <span>// SEANS_DURUMU: {isRunning ? "ODAK_DEVAM_EDIYOR" : "BEKLEMEDE"}</span>
              <span className="flex items-center gap-1.5">
                <span
                  className={`w-2 h-2 ${
                    isRunning ? "bg-[#38ef7d] animate-pulse" : "bg-[#e5a93c]"
                  }`}
                ></span>
                {activeMode.toUpperCase()}
              </span>
            </div>

            {/* Büyük Retro Dijital Sayaç */}
            <div className="font-pixel text-7xl sm:text-9xl tracking-wider text-[#38ef7d] glow-green my-2 tabular-nums select-none">
              {formatTime(secondsLeft)}
            </div>

            {/* Seans Döngü Kartuşları (4 Seans İndikatörü) */}
            <div className="flex gap-2 my-4">
              {[1, 2, 3, 4].map((round) => (
                <div
                  key={round}
                  className={`w-6 h-2 border border-[#30363d] ${
                    round <= roundCount ? "bg-[#38ef7d]" : "bg-[#161b22]"
                  }`}
                  title={`Döngü ${round} / 4`}
                />
              ))}
            </div>

            {/* Buton Kontrolleri */}
            <div className="flex gap-4 mt-6 w-full max-w-sm">
              <button
                onClick={handleTogglePlay}
                className={`pixel-btn text-sm py-3 px-6 flex-1 font-bold ${
                  isRunning ? "pixel-btn-amber" : "pixel-btn-primary"
                }`}
              >
                {isRunning ? "[ DURAKLAT ]" : "[ BAŞLAT ]"}
              </button>
              <button
                onClick={() => {
                  setIsRunning(false);
                  handleModeSwitch(activeMode);
                }}
                className="pixel-btn pixel-btn-secondary text-sm py-3 px-5 text-[#8b949e] hover:text-white"
              >
                [ SIFIRLA ]
              </button>
            </div>
          </div>

          {/* 3. HABBO TARZI SESSİZ ORTAK ÇALIŞMA MASALARI */}
          <div className="pixel-box p-4 bg-[#10141a]">
            <div className="flex items-center justify-between border-b border-[#262c36] pb-2 mb-3 text-xs text-[#8b949e]">
              <span>// SESSİZ_MASALAR: ODA #{inviteCode}</span>
              <span className="text-[#38ef7d]">3 KİŞİ ÇALIŞIYOR</span>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
              {/* Masa 1: Sen */}
              <div className="p-3 border border-[#38ef7d] bg-[#06080b] flex flex-col gap-1.5">
                <div className="flex items-center justify-between text-xs">
                  <span className="text-[#38ef7d] font-bold">Masa #01 (Sen)</span>
                  <span className="text-[10px] px-1.5 py-0.5 bg-[#161b22] text-[#38ef7d] border border-[#23c761]">
                    {isRunning ? "Odakta" : "Hazır"}
                  </span>
                </div>
                <div className="text-[11px] text-[#8b949e]">{userName}</div>
                <div className="text-[10px] text-[#38ef7d] font-mono">
                  Kalan: {formatTime(secondsLeft)}
                </div>
              </div>

              {/* Masa 2: Ahmet */}
              <div className="p-3 border border-[#30363d] bg-[#06080b] flex flex-col gap-1.5">
                <div className="flex items-center justify-between text-xs">
                  <span className="text-[#e6edf3] font-bold">Masa #02</span>
                  <span className="text-[10px] px-1.5 py-0.5 bg-[#161b22] text-[#e5a93c] border border-[#30363d]">
                    Odakta
                  </span>
                </div>
                <div className="text-[11px] text-[#8b949e]">Ahmet_Dev</div>
                <div className="text-[10px] text-[#e5a93c] font-mono">Kalan: 16:42</div>
              </div>

              {/* Masa 3: Selin */}
              <div className="p-3 border border-[#30363d] bg-[#06080b] flex flex-col gap-1.5">
                <div className="flex items-center justify-between text-xs">
                  <span className="text-[#e6edf3] font-bold">Masa #03</span>
                  <span className="text-[10px] px-1.5 py-0.5 bg-[#161b22] text-[#e5a93c] border border-[#30363d]">
                    Odakta
                  </span>
                </div>
                <div className="text-[11px] text-[#8b949e]">Selin_UX</div>
                <div className="text-[10px] text-[#e5a93c] font-mono">Kalan: 08:15</div>
              </div>
            </div>
          </div>
        </section>

        {/* SAĞ: İZOMETRİK PİKSEL ODA & SES MİKSERİ (5 Sütun) */}
        <section className="lg:col-span-5 flex flex-col gap-6">
          {/* İzometrik Piksel Oda Temsili (Canlı Sahne) */}
          <div className="pixel-box p-4 flex flex-col">
            <div className="flex items-center justify-between border-b border-[#262c36] pb-2 mb-3 text-xs text-[#8b949e]">
              <span>[PİKSEL ÇALIŞMA ODAN]</span>
              <span className="text-[#38ef7d]">Hava: Yağmurlu 12°C</span>
            </div>

            {/* Piksel Oda Sahnesi */}
            <div className="h-56 bg-[#06080b] border border-[#21262d] relative flex items-center justify-center overflow-hidden">
              {/* Arka Pencere (Yağmur Akışı) */}
              <div className="absolute top-3 left-6 right-6 h-24 border border-[#30363d] bg-[#0a1017] flex items-center justify-center overflow-hidden">
                <div className="absolute inset-0 opacity-40 bg-[radial-gradient(#38ef7d_1px,transparent_1px)] [background-size:8px_8px]"></div>
                <span className="text-[10px] text-[#484f58] tracking-widest">
                  // PENCERE_CAMI_YAGMUR_AKISI
                </span>
              </div>

              {/* Oda İçi Masa, Karakter ve Lambalar */}
              <div className="absolute bottom-3 flex flex-col items-center">
                {/* Masa Lambası ve Lo-Fi Piksel Plak Çalar */}
                <div className="w-56 flex justify-between items-end px-2 mb-[-8px] z-10">
                  {/* Sol: Retro Masa Lambası */}
                  <div className="flex flex-col items-center">
                    <div
                      className={`w-5 h-3 border border-[#23c761] transition-colors ${
                        isRunning
                          ? "bg-[#38ef7d] shadow-[0_0_12px_#38ef7d]"
                          : "bg-[#161b22]"
                      }`}
                    ></div>
                    <div className="w-1 h-5 bg-[#30363d]"></div>
                  </div>

                  {/* Sağ: Piksel Lo-Fi Plak Çalar (Dönen Disk) */}
                  <div className="w-8 h-6 bg-[#161b22] border border-[#30363d] flex items-center justify-center p-0.5">
                    <div
                      className={`w-5 h-5 rounded-full border border-[#484f58] bg-[#0a0d12] flex items-center justify-center ${
                        isRunning ? "animate-spin" : ""
                      }`}
                    >
                      <div className="w-1.5 h-1.5 rounded-full bg-[#e5a93c]"></div>
                    </div>
                  </div>
                </div>

                {/* Piksel Karakter (8-Bit Retro Sprite) */}
                <div className="relative flex flex-col items-center z-20">
                  {/* Saç / Şapka */}
                  <div className="w-6 h-2 bg-[#2d1b0d] border-t border-x border-[#5c3a1e]"></div>
                  {/* Yüz ve Gözler */}
                  <div className="w-6 h-4 bg-[#f8d0b0] border-x border-[#c99f7d] flex items-center justify-center gap-1.5">
                    <div className="w-1 h-1 bg-[#0a0d12]"></div>
                    <div className="w-1 h-1 bg-[#0a0d12]"></div>
                  </div>
                  {/* Retro Kazak */}
                  <div className="w-8 h-4 bg-[#1f3a2c] border-x border-[#38ef7d]/40 flex items-center justify-center">
                    <div className="w-2 h-2 bg-[#e5a93c]/80"></div>
                  </div>
                </div>

                {/* Ahşap Çalışma Masası */}
                <div className="w-60 h-4 bg-[#231a14] border-t-2 border-b-2 border-[#5c3a1e] mt-[-2px] shadow-[0_4px_0_#0a0604] z-30 flex items-center justify-between px-3">
                  <div className="w-1 h-1 bg-[#c99f7d]"></div>
                  <div className="text-[8px] text-[#8b6848] font-mono tracking-widest">
                    // BOKO-DESK #01
                  </div>
                  <div className="w-1 h-1 bg-[#c99f7d]"></div>
                </div>
              </div>

              <div className="absolute top-2 right-2 text-[9px] px-1.5 py-0.5 bg-[#10141a] border border-[#30363d] text-[#38ef7d] flex items-center gap-1 z-30">
                <span className="w-1.5 h-1.5 bg-[#38ef7d] animate-ping"></span>
                CANLI SEANS
              </div>
            </div>
          </div>

          {/* 4 Kanallı Akustik Ses Mikseri */}
          <div className="pixel-box p-4 flex flex-col gap-3">
            <div className="flex items-center justify-between border-b border-[#262c36] pb-2 text-xs text-[#8b949e]">
              <span>[4 KANALLI SES KALKANI]</span>
              <span className={isRunning ? "text-[#38ef7d]" : "text-[#e5a93c]"}>
                {isRunning ? "Kalkan Devrede" : "Mikser Beklemede"}
              </span>
            </div>

            {/* Kanal 1: Yağmur Sesi */}
            <div className="flex flex-col gap-1">
              <div className="flex justify-between text-[11px]">
                <span className="text-[#bacbb9]">🌧️ Yağmur Damlası</span>
                <span className="text-[#38ef7d]">{soundRain}%</span>
              </div>
              <input
                type="range"
                min="0"
                max="100"
                value={soundRain}
                onChange={(e) => setSoundRain(Number(e.target.value))}
                className="accent-[#38ef7d] h-2 bg-[#161b22] cursor-pointer"
              />
            </div>

            {/* Kanal 2: Kahverengi Gürültü (Brown Noise) */}
            <div className="flex flex-col gap-1">
              <div className="flex justify-between text-[11px]">
                <span className="text-[#bacbb9]">🌊 Kahverengi Gürültü</span>
                <span className="text-[#38ef7d]">{soundNoise}%</span>
              </div>
              <input
                type="range"
                min="0"
                max="100"
                value={soundNoise}
                onChange={(e) => setSoundNoise(Number(e.target.value))}
                className="accent-[#38ef7d] h-2 bg-[#161b22] cursor-pointer"
              />
            </div>

            {/* Kanal 3: Vinil Plak Çıtırtısı */}
            <div className="flex flex-col gap-1">
              <div className="flex justify-between text-[11px]">
                <span className="text-[#bacbb9]">📻 Vinil Çıtırtısı</span>
                <span className="text-[#38ef7d]">{soundVinyl}%</span>
              </div>
              <input
                type="range"
                min="0"
                max="100"
                value={soundVinyl}
                onChange={(e) => setSoundVinyl(Number(e.target.value))}
                className="accent-[#38ef7d] h-2 bg-[#161b22] cursor-pointer"
              />
            </div>

            {/* Kanal 4: Lo-Fi Melodi */}
            <div className="flex flex-col gap-1">
              <div className="flex justify-between text-[11px]">
                <span className="text-[#bacbb9]">🎧 Lo-Fi Plak Çalar</span>
                <span className="text-[#38ef7d]">{soundLofi}%</span>
              </div>
              <input
                type="range"
                min="0"
                max="100"
                value={soundLofi}
                onChange={(e) => setSoundLofi(Number(e.target.value))}
                className="accent-[#38ef7d] h-2 bg-[#161b22] cursor-pointer"
              />
            </div>
          </div>
        </section>
      </main>

      {/* 4. ALT BAR (FOOTER) */}
      <footer className="border-t-2 border-[#262c36] pt-4 mt-8 flex flex-col sm:flex-row items-center justify-between text-[11px] text-[#8b949e] gap-2">
        <div>© 2026 BOKODORO. Minimalist Retro Odaklanma Odası.</div>
        <div className="flex items-center gap-3">
          <span className="flex items-center gap-1.5">
            <span className="w-2 h-2 bg-[#38ef7d]"></span> API: Çevrimiçi (5000)
          </span>
          <span className="flex items-center gap-1.5">
            <span className="w-2 h-2 bg-[#38ef7d]"></span> DB: PostgreSQL+Vector (5433)
          </span>
        </div>
      </footer>
    </div>
  );
}

export default function RoomPage() {
  return (
    <Suspense fallback={<div className="p-8 text-[#38ef7d] font-mono">Oda yükleniyor...</div>}>
      <RoomContent />
    </Suspense>
  );
}
