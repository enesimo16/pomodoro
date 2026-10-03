import json
import sys
import time
import urllib.parse
import urllib.request
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) FocusApiProbe/1.0"
RAIN_CODES = {51, 53, 55, 61, 63, 65, 80, 81, 82}
SNOW_CODES = {71, 73, 75, 85, 86}


def load_env(path: Path) -> dict:
    if not path.exists():
        return {}
    values = {}
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if line and not line.startswith("#") and "=" in line:
            key, value = line.split("=", 1)
            values[key.strip()] = value.strip()
    return values


def request_json(url: str, headers: dict | None = None, body: dict | None = None):
    merged = {"User-Agent": USER_AGENT, **(headers or {})}
    data = None
    if body is not None:
        data = json.dumps(body).encode("utf-8")
        merged["Content-Type"] = "application/json"
    req = urllib.request.Request(url, data=data, headers=merged)
    start = time.perf_counter()
    try:
        with urllib.request.urlopen(req, timeout=20) as response:
            payload = json.loads(response.read().decode("utf-8"))
            return response.status, (time.perf_counter() - start) * 1000, payload, None
    except Exception as error:
        return 0, (time.perf_counter() - start) * 1000, None, str(error)


def header(title: str):
    print(f"\n--- {title} ---")


def report(status, latency, error) -> bool:
    if error:
        print(f"HATA ({latency:.0f}ms): {error}")
        return False
    print(f"Durum: {status} | Sure: {latency:.0f}ms")
    return True


def skip_if_missing(value: str | None, name: str) -> bool:
    if not value:
        print(f"Atlandi: {name} tanimli degil.")
        return True
    return False


def probe_open_meteo(base_url: str):
    header("Open-Meteo (hava durumu)")
    url = f"{base_url}/forecast?latitude=41.0082&longitude=28.9784&current_weather=true"
    status, latency, data, error = request_json(url)
    if not report(status, latency, error):
        return
    weather = data.get("current_weather", {})
    code = weather.get("weathercode")
    theme = "yagmur" if code in RAIN_CODES else "kar" if code in SNOW_CODES else "acik"
    print(f"Istanbul: {weather.get('temperature')}C | kod {code} | tema esleme: {theme}")


def probe_pixabay(key: str | None):
    header("Pixabay (video)")
    if skip_if_missing(key, "ExternalApis__Pixabay__ApiKey"):
        return
    query = urllib.parse.quote("rain window")
    url = f"https://pixabay.com/api/videos/?key={key}&q={query}&per_page=3"
    status, latency, data, error = request_json(url)
    if not report(status, latency, error):
        return
    for hit in data.get("hits", []):
        medium = hit.get("videos", {}).get("medium", {})
        print(f"- #{hit.get('id')} {hit.get('duration')}s {medium.get('width')}x{medium.get('height')} {medium.get('url')}")


def probe_jamendo(client_id: str | None):
    header("Jamendo (muzik + lisans kontrolu)")
    if skip_if_missing(client_id, "ExternalApis__Jamendo__ClientId"):
        return
    url = (
        "https://api.jamendo.com/v3.0/tracks/"
        f"?client_id={client_id}&format=json&tags=lofi&limit=5&audioformat=mp32"
    )
    status, latency, data, error = request_json(url)
    if not report(status, latency, error):
        return
    for track in data.get("results", []):
        license_url = track.get("license_ccurl", "")
        commercial = "TICARI KULLANIM YASAK" if "-nc" in license_url else "ticari uygun"
        print(f"- {track.get('name')} / {track.get('artist_name')} | {license_url} | {commercial}")


def probe_gemini(api_key: str | None, model_id: str | None):
    header("Google Gemini (ajan LLM)")
    if skip_if_missing(api_key, "Ai__Gemini__ApiKey"):
        return
    model = model_id or "gemini-flash-latest"
    url = f"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent"
    body = {"contents": [{"parts": [{"text": "Odaklanmak isteyen birine tek cumlelik sicak bir tavsiye ver."}]}]}
    status, latency, data, error = request_json(url, {"x-goog-api-key": api_key}, body)
    if not report(status, latency, error):
        return
    candidates = data.get("candidates", [])
    if candidates:
        print(f"Model: {model}\nYanit: {candidates[0]['content']['parts'][0]['text'].strip()}")


def probe_freesound(key: str | None):
    header("Freesound (ambiyans)")
    if skip_if_missing(key, "ExternalApis__Freesound__ApiKey"):
        return
    query = urllib.parse.quote("rain window")
    license_filter = urllib.parse.quote('license:"Creative Commons 0"')
    url = (
        f"https://freesound.org/apiv2/search/text/?query={query}&filter={license_filter}"
        f"&fields=id,name,duration,previews&page_size=3&token={key}"
    )
    status, latency, data, error = request_json(url)
    if not report(status, latency, error):
        return
    for result in data.get("results", []):
        print(f"- {result.get('name')} {result.get('duration')}s {result.get('previews', {}).get('preview-hq-mp3')}")


def probe_pexels(key: str | None):
    header("Pexels (video)")
    if skip_if_missing(key, "ExternalApis__Pexels__ApiKey"):
        return
    url = "https://api.pexels.com/videos/search?query=fireplace&per_page=2&orientation=landscape"
    status, latency, data, error = request_json(url, {"Authorization": key})
    if not report(status, latency, error):
        return
    for video in data.get("videos", []):
        files = video.get("video_files", [])
        print(f"- #{video.get('id')} {video.get('duration')}s {files[0].get('link') if files else ''}")


def main():
    env = load_env(Path(__file__).resolve().parent.parent / ".env")
    print("FOCUS - HARICI API YOKLAMA")
    probe_open_meteo(env.get("ExternalApis__OpenMeteo__BaseUrl", "https://api.open-meteo.com/v1"))
    probe_pixabay(env.get("ExternalApis__Pixabay__ApiKey"))
    probe_jamendo(env.get("ExternalApis__Jamendo__ClientId"))
    probe_gemini(env.get("Ai__Gemini__ApiKey"), env.get("Ai__Gemini__ChatModelId"))
    probe_freesound(env.get("ExternalApis__Freesound__ApiKey"))
    probe_pexels(env.get("ExternalApis__Pexels__ApiKey"))
    print("\nTamamlandi.")


if __name__ == "__main__":
    main()
