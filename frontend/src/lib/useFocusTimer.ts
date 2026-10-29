import { useCallback, useEffect, useRef, useState } from "react";
import { api, describeError, type FocusSessionDto, type SessionCompletionDto } from "@/lib/api";

export type TimerMode = "pomodoro" | "shortBreak" | "longBreak";
export type TimerPhase = "idle" | "running" | "paused";

export const TARGET_ROUNDS = 4;

export const MODES: Record<TimerMode, { label: string; minutes: number; kind: "Focus" | "Break" }> = {
  pomodoro: { label: "Odaklanma", minutes: 25, kind: "Focus" },
  shortBreak: { label: "Kısa Mola", minutes: 5, kind: "Break" },
  longBreak: { label: "Uzun Mola", minutes: 15, kind: "Break" },
};

const FOCUS_MINUTES = MODES.pomodoro.minutes;
const TICK_MS = 250;

interface TimerOptions {
  canSync: boolean;
  onReward: (result: SessionCompletionDto) => void;
  onSyncError: (message: string) => void;
}

// Bitis zamani damgasina dayali sayac; sunucu seansi mumkunse paralel olarak izlenir
export function useFocusTimer(options: TimerOptions) {
  const [mode, setMode] = useState<TimerMode>("pomodoro");
  const [secondsLeft, setSecondsLeft] = useState(FOCUS_MINUTES * 60);
  const [phase, setPhase] = useState<TimerPhase>("idle");
  const [round, setRound] = useState(1);
  const [announcement, setAnnouncement] = useState("");

  const endAtRef = useRef<number | null>(null);
  const sessionIdRef = useRef<string | null>(null);
  const syncQueueRef = useRef<Promise<void>>(Promise.resolve());
  const optionsRef = useRef(options);

  useEffect(() => {
    optionsRef.current = options;
  });

  // Sunucu cagrilari sirayla calisir; hata sayaci durdurmaz
  const enqueueSync = useCallback((operation: () => Promise<void>) => {
    if (!optionsRef.current.canSync) return;
    syncQueueRef.current = syncQueueRef.current
      .then(operation)
      .catch((err) => optionsRef.current.onSyncError(describeError(err)));
  }, []);

  const abandonActiveSession = useCallback(() => {
    enqueueSync(async () => {
      const id = sessionIdRef.current;
      sessionIdRef.current = null;
      if (id) await api.post(`/sessions/${id}/abandon`);
    });
  }, [enqueueSync]);

  const finish = useCallback(() => {
    // Bitis zamani damgasi tek seferde tuketilir, boylece tamamlama bir kez islenir
    if (endAtRef.current === null) return;
    endAtRef.current = null;

    const wasFocus = mode === "pomodoro";
    const nextMode: TimerMode = wasFocus ? (round >= TARGET_ROUNDS ? "longBreak" : "shortBreak") : "pomodoro";
    const nextRound = wasFocus ? round : round >= TARGET_ROUNDS ? 1 : round + 1;

    setMode(nextMode);
    setRound(nextRound);
    setSecondsLeft(MODES[nextMode].minutes * 60);
    setPhase("idle");
    setAnnouncement(`${MODES[mode].label} tamamlandı. Sıradaki: ${MODES[nextMode].label}.`);

    enqueueSync(async () => {
      const id = sessionIdRef.current;
      sessionIdRef.current = null;
      if (!id) return;
      const result = await api.post<SessionCompletionDto>(`/sessions/${id}/complete`);
      optionsRef.current.onReward(result);
    });
  }, [mode, round, enqueueSync]);

  useEffect(() => {
    if (phase !== "running") return;

    const tick = () => {
      const endAt = endAtRef.current;
      if (endAt === null) return;
      const remaining = Math.max(0, Math.ceil((endAt - Date.now()) / 1000));
      if (remaining <= 0) {
        finish();
      } else {
        setSecondsLeft(remaining);
      }
    };

    const interval = setInterval(tick, TICK_MS);
    document.addEventListener("visibilitychange", tick);
    return () => {
      clearInterval(interval);
      document.removeEventListener("visibilitychange", tick);
    };
  }, [phase, finish]);

  const start = useCallback(() => {
    if (phase === "running") return;

    if (phase === "paused" && secondsLeft > 0) {
      endAtRef.current = Date.now() + secondsLeft * 1000;
      setPhase("running");
      setAnnouncement("Sayaç devam ediyor.");
      enqueueSync(async () => {
        const id = sessionIdRef.current;
        if (id) await api.post(`/sessions/${id}/resume`);
      });
      return;
    }

    const config = MODES[mode];
    const total = config.minutes * 60;
    endAtRef.current = Date.now() + total * 1000;
    sessionIdRef.current = null;
    setSecondsLeft(total);
    setPhase("running");
    setAnnouncement(`${config.label} başladı.`);

    enqueueSync(async () => {
      const session = await api.post<FocusSessionDto>("/sessions", {
        kind: config.kind,
        focusMinutes: FOCUS_MINUTES,
        breakMinutes: config.kind === "Break" ? config.minutes : MODES.shortBreak.minutes,
        currentRound: round,
        targetRounds: TARGET_ROUNDS,
      });
      sessionIdRef.current = session.id;
    });
  }, [phase, secondsLeft, mode, round, enqueueSync]);

  const pause = useCallback(() => {
    if (phase !== "running" || endAtRef.current === null) return;
    const remaining = Math.max(0, Math.ceil((endAtRef.current - Date.now()) / 1000));
    endAtRef.current = null;
    setSecondsLeft(remaining);
    setPhase("paused");
    setAnnouncement("Sayaç duraklatıldı.");
    enqueueSync(async () => {
      const id = sessionIdRef.current;
      if (id) await api.post(`/sessions/${id}/pause`);
    });
  }, [phase, enqueueSync]);

  const switchMode = useCallback(
    (nextMode: TimerMode) => {
      if (phase !== "idle") abandonActiveSession();
      endAtRef.current = null;
      setMode(nextMode);
      setSecondsLeft(MODES[nextMode].minutes * 60);
      setPhase("idle");
      setAnnouncement(`${MODES[nextMode].label} seçildi.`);
    },
    [phase, abandonActiveSession],
  );

  const reset = useCallback(() => switchMode(mode), [switchMode, mode]);

  // Sayfa yenilendiginde sunucudaki aktif seansi geri yukler
  const restore = useCallback((session: FocusSessionDto) => {
    const restoredMode: TimerMode =
      session.kind === "Focus" ? "pomodoro" : session.breakMinutes >= MODES.longBreak.minutes ? "longBreak" : "shortBreak";
    sessionIdRef.current = session.id;
    setMode(restoredMode);
    setRound(Math.min(Math.max(session.currentRound, 1), TARGET_ROUNDS));
    setSecondsLeft(session.remainingSeconds);
    if (session.status === "Running") {
      endAtRef.current = Date.now() + session.remainingSeconds * 1000;
      setPhase("running");
    } else {
      endAtRef.current = null;
      setPhase("paused");
    }
  }, []);

  return { mode, secondsLeft, phase, round, announcement, start, pause, reset, switchMode, restore };
}
