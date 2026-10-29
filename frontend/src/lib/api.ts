export const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000/api/v1";

export class ApiError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

export interface UserDto {
  id: string;
  displayName: string;
  email: string | null;
  isGuest: boolean;
  level: number;
  currentXp: number;
  locale: string;
  timeZoneId: string;
  createdAt: string;
}

export interface PixelRoomDto {
  id: string;
  ownerUserId: string;
  name: string;
  roomType: string;
  inviteCode: string | null;
  isPublic: boolean;
  maxVisitors: number;
}

export interface CurrentUserDto {
  user: UserDto;
  room: PixelRoomDto | null;
  totalCoins: number;
}

export type SessionKindName = "Focus" | "Break";

export interface FocusSessionDto {
  id: string;
  kind: SessionKindName;
  status: "Running" | "Paused" | "Completed" | "Abandoned";
  focusMinutes: number;
  breakMinutes: number;
  currentRound: number;
  targetRounds: number;
  remainingSeconds: number;
}

export interface StartSessionRequest {
  kind: SessionKindName;
  focusMinutes: number;
  breakMinutes: number;
  currentRound: number;
  targetRounds: number;
}

export interface SessionCompletionDto {
  session: FocusSessionDto;
  xpEarned: number;
  coinsEarned: number;
  newLevel: number;
  totalXp: number;
  totalCoins: number;
  levelUp: boolean;
}

export interface WeatherReportDto {
  temperature: number;
  description: string;
  weatherCondition: string;
  isDay: boolean;
}

export interface RoomMemberDto {
  userId: string;
  displayName: string;
  level: number;
  seatIndex: number | null;
  isFocusing: boolean;
}

export interface StudyRoomDto {
  code: string;
  name: string;
  currentMemberCount: number;
  maxCapacity: number;
  members: RoomMemberDto[];
}

export interface JoinRoomResultDto {
  success: boolean;
  message: string;
  room: StudyRoomDto | null;
}

export interface RoomInvitationDto {
  roomCode: string;
  inviteCode: string;
}

// Yenileme ve oturum uc noktalari 401 ile tekrar denenmez
const NO_REFRESH_PATHS = ["/auth/refresh", "/auth/guest", "/auth/logout"];

let refreshInFlight: Promise<boolean> | null = null;

function refreshSession(): Promise<boolean> {
  if (!refreshInFlight) {
    refreshInFlight = fetch(`${API_BASE}/auth/refresh`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: "{}",
    })
      .then((res) => res.ok)
      .catch(() => false)
      .finally(() => {
        refreshInFlight = null;
      });
  }
  return refreshInFlight;
}

async function readErrorMessage(res: Response): Promise<string> {
  try {
    const text = await res.text();
    if (text) {
      const parsed = JSON.parse(text) as { message?: string; title?: string };
      return parsed.message ?? parsed.title ?? res.statusText;
    }
  } catch {
    // govde JSON degilse durum metni kullanilir
  }
  return res.statusText || `HTTP ${res.status}`;
}

async function request<T>(path: string, init: RequestInit = {}, canRetry = true): Promise<T> {
  const headers = new Headers(init.headers);
  if (init.body !== undefined && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const res = await fetch(`${API_BASE}${path}`, { ...init, headers, credentials: "include" });

  if (res.status === 401 && canRetry && !NO_REFRESH_PATHS.includes(path)) {
    if (await refreshSession()) {
      return request<T>(path, init, false);
    }
  }

  if (!res.ok) {
    throw new ApiError(res.status, await readErrorMessage(res));
  }

  if (res.status === 204) {
    return undefined as T;
  }

  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: "POST", body: body === undefined ? undefined : JSON.stringify(body) }),
};

export function describeError(err: unknown): string {
  if (err instanceof ApiError) return err.message;
  return "Sunucuya ulaşılamadı. Bağlantını kontrol edip tekrar dene.";
}

export function loginAsGuest(displayName: string) {
  return api.post<{ user: UserDto }>("/auth/guest", {
    displayName,
    locale: navigator.language,
    timeZoneId: Intl.DateTimeFormat().resolvedOptions().timeZone,
  });
}

// Eski surumlerin localStorage'a yazdigi token iceren kaydi temizler
export function clearLegacyAuthStorage() {
  try {
    localStorage.removeItem("bokodoro_auth");
  } catch {
    // depolama erisimi engellenmis olabilir
  }
}
