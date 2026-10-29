"use client";

import { useCallback, useEffect, useRef, useState, Suspense } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import {
  api,
  ApiError,
  clearLegacyAuthStorage,
  describeError,
  type CurrentUserDto,
  type FocusSessionDto,
  type JoinRoomResultDto,
  type RoomInvitationDto,
  type SessionCompletionDto,
  type StudyRoomDto,
  type WeatherReportDto,
} from "@/lib/api";
import { Soundscape, type SoundChannel } from "@/lib/soundscape";
import { MODES, TARGET_ROUNDS, useFocusTimer, type TimerMode } from "@/lib/useFocusTimer";

type AuthState = "loading" | "authenticated" | "anonymous";

const ROOM_CODE_STORAGE_KEY = "bokodoro_room_code";
const ROOM_REFRESH_MS = 30000;
const MODE_ORDER: TimerMode[] = ["pomodoro", "shortBreak", "longBreak"];

const CHANNELS: { id: SoundChannel; label: string }[] = [
  { id: "rain", label: "Yağmur Damlası" },
  { id: "noise", label: "Kahverengi Gürültü" },
  { id: "vinyl", label: "Vinil Çıtırtısı" },
  { id: "lofi", label: "Lo-Fi Plak Çalar" },
];

function formatTime(totalSeconds: number) {
  const mins = Math.floor(totalSeconds / 60);
  const secs = totalSeconds % 60;
  return `${mins.toString().padStart(2, "0")}:${secs.toString().padStart(2, "0")}`;
}

function readStoredRoomCode() {
  try {
    return localStorage.getItem(ROOM_CODE_STORAGE_KEY);
  } catch {
    return null;
  }
}

function storeRoomCode(code: string | null) {
  try {
    if (code) localStorage.setItem(ROOM_CODE_STORAGE_KEY, code);
    else localStorage.removeItem(ROOM_CODE_STORAGE_KEY);
  } catch {
    // depolama erisimi engellenmis olabilir
  }
}

function RoomContent() {
  const searchParams = useSearchParams();
  const roomParam = searchParams.get("room");
  const inviteParam = searchParams.get("invite");

  const [authState, setAuthState] = useState<AuthState>("loading");
  const [apiReachable, setApiReachable] = useState<boolean | null>(null);
  const [profile, setProfile] = useState<CurrentUserDto | null>(null);
  const [userLevel, setUserLevel] = useState(1);
  const [studyRoom, setStudyRoom] = useState<StudyRoomDto | null>(null);
  const [roomNotice, setRoomNotice] = useState<string | null>(null);
  const [inviteStatus, setInviteStatus] = useState<"idle" | "busy" | "copied">("idle");
  const [syncError, setSyncError] = useState<string | null>(null);
  const [reward, setReward] = useState<SessionCompletionDto | null>(null);
  const [weather, setWeather] = useState<WeatherReportDto | null>(null);
  const [weatherDenied, setWeatherDenied] = useState(false);

  const [levels, setLevels] = useState<Record<SoundChannel, number>>({
    rain: 40,
    noise: 25,
    vinyl: 15,
    lofi: 60,
  });
  const soundscapeRef = useRef<Soundscape | null>(null);

  const timer = useFocusTimer({
    canSync: authState === "authenticated",
    onReward: (result) => {
      setReward(result);
      setUserLevel(result.newLevel);
    },
    onSyncError: (message) =>
      setSyncError(`Sunucu ile senkronize edilemedi (${message}). Sayaç yerel olarak çalışmaya devam ediyor.`),
  });
  const { phase, restore } = timer;
  const isRunning = phase === "running";

  // Oturum, aktif seans ve davet baglantisi
  useEffect(() => {
    clearLegacyAuthStorage();
    let cancelled = false;

    const load = async () => {
      let me: CurrentUserDto;
      try {
        me = await api.get<CurrentUserDto>("/auth/me");
      } catch (err) {
        if (cancelled) return;
        setApiReachable(err instanceof ApiError);
        setAuthState("anonymous");
        return;
      }
      if (cancelled) return;

      setApiReachable(true);
      setProfile(me);
      setUserLevel(me.user.level);
      setAuthState("authenticated");

      try {
        const active = await api.get<FocusSessionDto | undefined>("/sessions/active");
        if (!cancelled && active) restore(active);
      } catch {
        // aktif seans okunamazsa sayac sifirdan baslar
      }

      const codeToJoin = roomParam ?? readStoredRoomCode();
      if (!codeToJoin) return;

      try {
        if (roomParam) {
          const joined = await api.post<JoinRoomResultDto>("/study-rooms/join", {
            roomCode: roomParam,
            inviteCode: inviteParam ?? undefined,
          });
          if (cancelled) return;
          if (joined.success && joined.room) {
            setStudyRoom(joined.room);
            storeRoomCode(joined.room.code);
          } else {
            setRoomNotice(joined.message);
          }
        } else {
          const room = await api.get<StudyRoomDto>(`/study-rooms/${encodeURIComponent(codeToJoin)}`);
          if (!cancelled) setStudyRoom(room);
        }
      } catch (err) {
        if (cancelled) return;
        if (!roomParam) storeRoomCode(null);
        setRoomNotice(describeError(err));
      }
    };

    void load();
    return () => {
      cancelled = true;
    };
  }, [roomParam, inviteParam, restore]);

  // Ortak oda varligi duzenli olarak yenilenir
  const studyRoomCode = studyRoom?.code ?? null;
  useEffect(() => {
    if (!studyRoomCode) return;
    const interval = setInterval(() => {
      api
        .get<StudyRoomDto>(`/study-rooms/${encodeURIComponent(studyRoomCode)}`)
        .then(setStudyRoom)
        .catch(() => {
          // gecici hata sonraki turda tekrar denenir
        });
    }, ROOM_REFRESH_MS);
    return () => clearInterval(interval);
  }, [studyRoomCode]);

  // Hava durumu: konum izni daha once verildiyse otomatik, aksi halde kullanici istegiyle
  const loadWeather = useCallback(() => {
    if (!("geolocation" in navigator)) {
      setWeatherDenied(true);
      return;
    }
    navigator.geolocation.getCurrentPosition(
      (position) => {
        const latitude = position.coords.latitude.toFixed(2);
        const longitude = position.coords.longitude.toFixed(2);
        api
          .get<WeatherReportDto>(`/media/weather?latitude=${latitude}&longitude=${longitude}`)
          .then(setWeather)
          .catch(() => setWeatherDenied(true));
      },
      () => setWeatherDenied(true),
      { maximumAge: 600000, timeout: 10000 },
    );
  }, []);

  useEffect(() => {
    if (!navigator.permissions) return;
    navigator.permissions
      .query({ name: "geolocation" })
      .then((status) => {
        if (status.state === "granted") loadWeather();
      })
      .catch(() => {
        // izin durumu okunamazsa dugme ile istenir
      });
  }, [loadWeather]);

  // Ses motoru: sayfa kapaninca kaynaklar durdurulur ve baglam kapatilir
  useEffect(() => {
    return () => {
      soundscapeRef.current?.dispose();
      soundscapeRef.current = null;
    };
  }, []);

  useEffect(() => {
    const engine = soundscapeRef.current;
    if (!engine) return;
    (Object.keys(levels) as SoundChannel[]).forEach((channel) => engine.setLevel(channel, levels[channel]));
  }, [levels]);

  useEffect(() => {
    soundscapeRef.current?.setPlaying(isRunning);
  }, [isRunning]);

  const handleTogglePlay = () => {
    if (isRunning) {
      timer.pause();
      return;
    }

    if (!soundscapeRef.current) {
      try {
        const engine = new Soundscape();
        (Object.keys(levels) as SoundChannel[]).forEach((channel) => engine.setLevel(channel, levels[channel]));
        soundscapeRef.current = engine;
      } catch {
        // ses desteklenmiyorsa sayac sessiz calisir
      }
    }
    setReward(null);
    timer.start();
  };

  const copyInvite = async () => {
    setInviteStatus("busy");
    setRoomNotice(null);
    try {
      const invitation = await api.post<RoomInvitationDto>("/study-rooms/create-invite");
      const link = `${window.location.origin}/room?room=${encodeURIComponent(invitation.roomCode)}&invite=${encodeURIComponent(invitation.inviteCode)}`;
      await navigator.clipboard.writeText(link);
      setInviteStatus("copied");
      setTimeout(() => setInviteStatus("idle"), 2000);

      storeRoomCode(invitation.roomCode);
      api
        .get<StudyRoomDto>(`/study-rooms/${encodeURIComponent(invitation.roomCode)}`)
        .then(setStudyRoom)
        .catch(() => setRoomNotice("Oda bilgisi yenilenemedi."));
    } catch (err) {
      setInviteStatus("idle");
      setRoomNotice(
        err instanceof ApiError || err instanceof TypeError
          ? `Davet bağlantısı hazırlanamadı: ${describeError(err)}`
          : "Davet bağlantısı panoya kopyalanamadı.",
      );
    }
  };

  const userName = profile?.user.displayName ?? "Misafir";
  const roomLabel = studyRoom?.code ?? profile?.room?.inviteCode ?? null;
  const focusingCount = studyRoom ? studyRoom.members.filter((m) => m.isFocusing).length : 0;
  const preserved = new URLSearchParams();
  if (roomParam) preserved.set("room", roomParam);
  if (inviteParam) preserved.set("invite", inviteParam);
  const loginHref = preserved.toString() ? `/?${preserved.toString()}` : "/";

  return (
    <div className="flex-1 flex flex-col justify-between max-w-6xl w-full mx-auto p-4 sm:p-6 lg:p-8">
      <p role="status" aria-live="polite" className="sr-only">
        {timer.announcement}
      </p>

      {/* 1. ÜST BAR (NAVBAR) */}
      <header className="flex flex-wrap items-center justify-between border-b-2 border-[#262c36] pb-4 mb-6 gap-4">
        <div className="flex items-center gap-3">
          <Link
            href="/"
            className="pixel-btn pixel-btn-secondary text-xs px-2.5 py-1.5 text-[#8b949e] hover:text-[#38ef7d] flex items-center gap-1.5"
            title="Ana Sayfaya Dön"
          >
            <span aria-hidden="true">&lt;</span>
            <span className="hidden sm:inline">ANA SAYFA</span>
            <span className="sr-only sm:hidden">Ana sayfa</span>
          </Link>

          <div
            aria-hidden="true"
            className="w-7 h-7 bg-[#38ef7d] flex items-center justify-center font-bold text-black border-2 border-[#23c761] shadow-[2px_2px_0px_#040608]"
          >
            ■
          </div>
          <span className="text-xl sm:text-2xl font-bold tracking-widest text-[#38ef7d] glow-green">
            BOKODORO
          </span>
          {roomLabel && (
            <span className="text-[10px] uppercase tracking-widest px-2 py-0.5 border border-[#30363d] text-[#38ef7d] bg-[#10141a]">
              ODA #{roomLabel}
            </span>
          )}
        </div>

        <nav className="flex items-center gap-2 sm:gap-4 text-xs">
          <div className="px-3 py-1.5 bg-[#10141a] border border-[#30363d] flex items-center gap-2 text-[#e6edf3]">
            <span aria-hidden="true" className="w-2 h-2 bg-[#38ef7d]"></span>
            <span className="font-bold">{userName}</span>
            {authState === "authenticated" && (
              <span className="text-[10px] px-1.5 py-0.2 bg-[#161b22] text-[#e5a93c] border border-[#30363d]">
                LVL.{userLevel}
              </span>
            )}
          </div>

          <button
            type="button"
            onClick={copyInvite}
            disabled={authState !== "authenticated" || inviteStatus === "busy"}
            title={authState !== "authenticated" ? "Davet için önce misafir olarak giriş yap" : undefined}
            className="pixel-btn pixel-btn-secondary text-xs px-3 py-1.5 text-[#bacbb9] hover:text-white disabled:opacity-50"
          >
            {inviteStatus === "copied" ? "[ KOPYALANDI! ]" : "[ + ARKADAŞINI ÇAĞIR ]"}
          </button>
        </nav>
      </header>

      {/* Bildirimler */}
      <div className="flex flex-col gap-3 mb-6 empty:hidden">
        {authState === "anonymous" && (
          <div role="status" className="p-3 border border-[#e5a93c] bg-[#1a150a] text-xs text-[#e5a93c]">
            {apiReachable === false
              ? "Sunucuya ulaşılamıyor. Sayaç yerel çalışıyor, seanslar kaydedilmiyor."
              : "Oturum açık değil. Sayaç yerel çalışıyor, seanslar kaydedilmiyor."}{" "}
            <Link href={loginHref} className="underline hover:text-white">
              Misafir olarak giriş yap
            </Link>
          </div>
        )}
        {syncError && (
          <div role="status" className="p-3 border border-[#e5a93c] bg-[#1a150a] text-xs text-[#e5a93c] flex items-start justify-between gap-3">
            <span>{syncError}</span>
            <button type="button" onClick={() => setSyncError(null)} aria-label="Uyarıyı kapat" className="text-[#8b949e] hover:text-white">
              [X]
            </button>
          </div>
        )}
        {roomNotice && (
          <div role="status" className="p-3 border border-[#30363d] bg-[#10141a] text-xs text-[#bacbb9] flex items-start justify-between gap-3">
            <span>{roomNotice}</span>
            <button type="button" onClick={() => setRoomNotice(null)} aria-label="Bildirimi kapat" className="text-[#8b949e] hover:text-white">
              [X]
            </button>
          </div>
        )}
        {reward && (reward.xpEarned > 0 || reward.coinsEarned > 0) && (
          <div role="status" className="p-3 border border-[#23c761] bg-[#0d1a12] text-xs text-[#38ef7d]">
            Seans tamamlandı: +{reward.xpEarned} XP / +{reward.coinsEarned} Coin
            {reward.levelUp ? ` - Seviye atladın! LVL.${reward.newLevel}` : ""}
          </div>
        )}
      </div>

      {/* 2. MERKEZ ODA & SAYAÇ IZGARASI */}
      <main className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start my-auto">
        {/* SOL: SAYAÇ & ODAK KONTROL PANELİ (7 Sütun) */}
        <section className="lg:col-span-7 flex flex-col gap-6">
          <div role="tablist" aria-label="Seans türü" className="flex gap-2">
            {MODE_ORDER.map((id) => (
              <button
                key={id}
                type="button"
                role="tab"
                id={`tab-${id}`}
                aria-selected={timer.mode === id}
                aria-controls="timer-panel"
                onClick={() => timer.switchMode(id)}
                className={`pixel-btn text-xs px-4 py-2 flex-1 ${
                  timer.mode === id
                    ? id === "pomodoro"
                      ? "pixel-btn-primary"
                      : "pixel-btn-amber"
                    : "pixel-btn-secondary"
                }`}
              >
                {MODES[id].label} ({MODES[id].minutes}d)
              </button>
            ))}
          </div>

          {/* Retro LED Zaman Sayacı Kartı */}
          <div
            id="timer-panel"
            role="tabpanel"
            aria-labelledby={`tab-${timer.mode}`}
            className="pixel-box p-6 sm:p-10 flex flex-col items-center justify-center text-center relative overflow-hidden"
          >
            <div className="w-full flex items-center justify-between border-b border-[#262c36] pb-3 mb-6 text-xs text-[#8b949e]">
              <span>
                {"// SEANS_DURUMU: "}
                {isRunning ? "ODAK_DEVAM_EDIYOR" : phase === "paused" ? "DURAKLATILDI" : "BEKLEMEDE"}
              </span>
              <span className="flex items-center gap-1.5">
                <span
                  aria-hidden="true"
                  className={`w-2 h-2 ${isRunning ? "bg-[#38ef7d] animate-pulse" : "bg-[#e5a93c]"}`}
                ></span>
                {MODES[timer.mode].label.toUpperCase()}
              </span>
            </div>

            <div
              role="timer"
              aria-live="off"
              aria-label={`Kalan süre ${formatTime(timer.secondsLeft)}`}
              className="font-pixel text-7xl sm:text-9xl tracking-wider text-[#38ef7d] glow-green my-2 tabular-nums select-none"
            >
              {formatTime(timer.secondsLeft)}
            </div>

            <div className="flex gap-2 my-4" role="img" aria-label={`Döngü ${timer.round} / ${TARGET_ROUNDS}`}>
              {Array.from({ length: TARGET_ROUNDS }, (_, index) => index + 1).map((round) => (
                <div
                  key={round}
                  className={`w-6 h-2 border border-[#30363d] ${round <= timer.round ? "bg-[#38ef7d]" : "bg-[#161b22]"}`}
                  title={`Döngü ${round} / ${TARGET_ROUNDS}`}
                />
              ))}
            </div>

            <div className="flex gap-4 mt-6 w-full max-w-sm">
              <button
                type="button"
                onClick={handleTogglePlay}
                className={`pixel-btn text-sm py-3 px-6 flex-1 font-bold ${
                  isRunning ? "pixel-btn-amber" : "pixel-btn-primary"
                }`}
              >
                {isRunning ? "[ DURAKLAT ]" : phase === "paused" ? "[ DEVAM ET ]" : "[ BAŞLAT ]"}
              </button>
              <button
                type="button"
                onClick={timer.reset}
                className="pixel-btn pixel-btn-secondary text-sm py-3 px-5 text-[#8b949e] hover:text-white"
              >
                [ SIFIRLA ]
              </button>
            </div>
          </div>

          {/* 3. SESSİZ ORTAK ÇALIŞMA MASALARI */}
          <div className="pixel-box p-4 bg-[#10141a]">
            <div className="flex items-center justify-between border-b border-[#262c36] pb-2 mb-3 text-xs text-[#8b949e]">
              <span>{"// SESSİZ_MASALAR"}{studyRoom ? `: ODA #${studyRoom.code}` : ""}</span>
              {studyRoom && <span className="text-[#38ef7d]">{focusingCount} KİŞİ ÇALIŞIYOR</span>}
            </div>

            {studyRoom ? (
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                {studyRoom.members.map((member) => {
                  const isSelf = member.userId === profile?.user.id;
                  const focusing = isSelf ? isRunning && timer.mode === "pomodoro" : member.isFocusing;
                  return (
                    <div
                      key={member.userId}
                      className={`p-3 border bg-[#06080b] flex flex-col gap-1.5 ${
                        isSelf ? "border-[#38ef7d]" : "border-[#30363d]"
                      }`}
                    >
                      <div className="flex items-center justify-between text-xs">
                        <span className={`font-bold ${isSelf ? "text-[#38ef7d]" : "text-[#e6edf3]"}`}>
                          {member.seatIndex !== null
                            ? `Masa #${String(member.seatIndex).padStart(2, "0")}`
                            : "Masasız"}
                          {isSelf ? " (Sen)" : ""}
                        </span>
                        <span className="text-[10px] px-1.5 py-0.5 bg-[#161b22] text-[#38ef7d] border border-[#23c761]">
                          {focusing ? "Odakta" : "Hazır"}
                        </span>
                      </div>
                      <div className="text-[11px] text-[#8b949e]">{member.displayName}</div>
                      {isSelf && (
                        <div className="text-[10px] text-[#38ef7d] font-mono">
                          Kalan: {formatTime(timer.secondsLeft)}
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            ) : (
              <p className="text-xs text-[#8b949e]">
                Henüz ortak bir odada değilsin. &quot;Arkadaşını çağır&quot; ile davet bağlantısı oluşturup
                birlikte sessizce çalışabilirsin.
              </p>
            )}
          </div>
        </section>

        {/* SAĞ: İZOMETRİK PİKSEL ODA & SES MİKSERİ (5 Sütun) */}
        <section className="lg:col-span-5 flex flex-col gap-6">
          <div className="pixel-box p-4 flex flex-col">
            <div className="flex items-center justify-between border-b border-[#262c36] pb-2 mb-3 text-xs text-[#8b949e]">
              <span>[PİKSEL ÇALIŞMA ODAN]</span>
              {weather ? (
                <span className="text-[#38ef7d]">
                  Hava: {weather.description} {Math.round(weather.temperature)}°C
                </span>
              ) : weatherDenied ? (
                <span className="text-[#484f58]">Hava bilgisi alınamadı</span>
              ) : (
                <button
                  type="button"
                  onClick={loadWeather}
                  className="text-[#38ef7d] underline hover:text-white"
                >
                  Hava için konum paylaş
                </button>
              )}
            </div>

            <div className="h-56 bg-[#06080b] border border-[#21262d] relative flex items-center justify-center overflow-hidden">
              <div className="absolute top-3 left-6 right-6 h-24 border border-[#30363d] bg-[#0a1017] flex items-center justify-center overflow-hidden">
                <div className="absolute inset-0 opacity-40 bg-[radial-gradient(#38ef7d_1px,transparent_1px)] [background-size:8px_8px]"></div>
                <span className="text-[10px] text-[#484f58] tracking-widest">{"// PENCERE"}</span>
              </div>

              <div className="absolute bottom-3 flex flex-col items-center">
                <div className="w-56 flex justify-between items-end px-2 mb-[-8px] z-10">
                  <div className="flex flex-col items-center">
                    <div
                      className={`w-5 h-3 border border-[#23c761] transition-colors ${
                        isRunning ? "bg-[#38ef7d] shadow-[0_0_12px_#38ef7d]" : "bg-[#161b22]"
                      }`}
                    ></div>
                    <div className="w-1 h-5 bg-[#30363d]"></div>
                  </div>

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

                <div className="w-60 h-4 bg-[#231a14] border-t-2 border-b-2 border-[#5c3a1e] mt-[-2px] shadow-[0_4px_0_#0a0604] z-30 flex items-center justify-between px-3">
                  <div className="w-1 h-1 bg-[#c99f7d]"></div>
                  <div className="text-[8px] text-[#8b6848] font-mono tracking-widest">{"// BOKO-DESK #01"}</div>
                  <div className="w-1 h-1 bg-[#c99f7d]"></div>
                </div>
              </div>

              {isRunning && (
                <div className="absolute top-2 right-2 text-[9px] px-1.5 py-0.5 bg-[#10141a] border border-[#30363d] text-[#38ef7d] flex items-center gap-1 z-30">
                  <span aria-hidden="true" className="w-1.5 h-1.5 bg-[#38ef7d] animate-ping"></span>
                  SEANS AKTİF
                </div>
              )}
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

            {CHANNELS.map((channel) => (
              <div key={channel.id} className="flex flex-col gap-1">
                <div className="flex justify-between text-[11px]">
                  <label htmlFor={`slider-${channel.id}`} className="text-[#bacbb9]">
                    {channel.label}
                  </label>
                  <span aria-hidden="true" className="text-[#38ef7d]">
                    {levels[channel.id]}%
                  </span>
                </div>
                <input
                  id={`slider-${channel.id}`}
                  type="range"
                  min="0"
                  max="100"
                  value={levels[channel.id]}
                  aria-label={`${channel.label} ses seviyesi`}
                  aria-valuetext={`yüzde ${levels[channel.id]}`}
                  onChange={(e) => setLevels((prev) => ({ ...prev, [channel.id]: Number(e.target.value) }))}
                  className="accent-[#38ef7d] h-2 bg-[#161b22] cursor-pointer"
                />
              </div>
            ))}
          </div>
        </section>
      </main>

      {/* 4. ALT BAR (FOOTER) */}
      <footer className="border-t-2 border-[#262c36] pt-4 mt-8 flex flex-col sm:flex-row items-center justify-between text-[11px] text-[#8b949e] gap-2">
        <div>© 2026 BOKODORO. Minimalist Retro Odaklanma Odası.</div>
        {apiReachable !== null && (
          <span className="flex items-center gap-1.5">
            <span
              aria-hidden="true"
              className={`w-2 h-2 ${apiReachable ? "bg-[#38ef7d]" : "bg-[#f85149]"}`}
            ></span>
            {apiReachable ? "Sunucu: bağlı" : "Sunucu: bağlantı yok"}
          </span>
        )}
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
