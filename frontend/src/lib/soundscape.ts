export type SoundChannel = "rain" | "noise" | "vinyl" | "lofi";

const MAX_GAIN: Record<SoundChannel, number> = {
  rain: 0.2,
  noise: 0.15,
  vinyl: 0.25,
  lofi: 0.08,
};

const RAMP_SECONDS = 0.05;
const SUSPEND_DELAY_MS = 300;
const CHORD_INTERVAL_MS = 8000;

// Am7 - Fmaj7 - Cmaj7 - G7 alcak akor dongusu (Hz)
const CHORDS: number[][] = [
  [110.0, 130.81, 164.81, 196.0],
  [87.31, 110.0, 130.81, 164.81],
  [130.81, 164.81, 196.0, 246.94],
  [98.0, 123.47, 146.83, 174.61],
];

type AudioContextCtor = typeof AudioContext;

function createNoiseBuffer(ctx: AudioContext, seconds: number, fill: (data: Float32Array) => void) {
  const buffer = ctx.createBuffer(1, Math.floor(ctx.sampleRate * seconds), ctx.sampleRate);
  fill(buffer.getChannelData(0));
  return buffer;
}

function loopSource(ctx: AudioContext, buffer: AudioBuffer) {
  const source = ctx.createBufferSource();
  source.buffer = buffer;
  source.loop = true;
  return source;
}

// Tarayici icinde sentezlenen 4 kanalli ortam sesi (harici ses dosyasi yok)
export class Soundscape {
  private ctx: AudioContext;
  private gains: Record<SoundChannel, GainNode>;
  private levels: Record<SoundChannel, number> = { rain: 0, noise: 0, vinyl: 0, lofi: 0 };
  private sources: AudioScheduledSourceNode[] = [];
  private chordOscillators: OscillatorNode[] = [];
  private chordTimer: ReturnType<typeof setInterval> | null = null;
  private chordIndex = 0;
  private suspendTimer: ReturnType<typeof setTimeout> | null = null;
  private playing = false;
  private disposed = false;

  constructor() {
    const Ctor: AudioContextCtor =
      window.AudioContext ?? (window as unknown as { webkitAudioContext: AudioContextCtor }).webkitAudioContext;
    this.ctx = new Ctor();

    this.gains = {
      rain: this.createChannelGain(),
      noise: this.createChannelGain(),
      vinyl: this.createChannelGain(),
      lofi: this.createChannelGain(),
    };

    this.buildNoise();
    this.buildRain();
    this.buildVinyl();
    this.buildLofi();
  }

  setLevel(channel: SoundChannel, percent: number) {
    this.levels[channel] = percent;
    this.applyGain(channel);
  }

  setPlaying(playing: boolean) {
    if (this.disposed) return;
    this.playing = playing;

    if (this.suspendTimer) {
      clearTimeout(this.suspendTimer);
      this.suspendTimer = null;
    }

    if (playing) {
      void this.ctx.resume();
    } else {
      // Tiklama sesi olmamasi icin once kazanc sifirlanir, sonra baglam askiya alinir
      this.suspendTimer = setTimeout(() => {
        if (!this.playing && !this.disposed) void this.ctx.suspend();
      }, SUSPEND_DELAY_MS);
    }

    (Object.keys(this.gains) as SoundChannel[]).forEach((channel) => this.applyGain(channel));
  }

  dispose() {
    if (this.disposed) return;
    this.disposed = true;

    if (this.suspendTimer) clearTimeout(this.suspendTimer);
    if (this.chordTimer) clearInterval(this.chordTimer);

    this.sources.forEach((source) => {
      try {
        source.stop();
      } catch {
        // kaynak zaten durdurulmus olabilir
      }
      source.disconnect();
    });
    this.sources = [];
    this.chordOscillators = [];

    void this.ctx.close();
  }

  private createChannelGain() {
    const gain = this.ctx.createGain();
    gain.gain.value = 0;
    gain.connect(this.ctx.destination);
    return gain;
  }

  private applyGain(channel: SoundChannel) {
    const target = this.playing ? (this.levels[channel] / 100) * MAX_GAIN[channel] : 0;
    this.gains[channel].gain.setTargetAtTime(target, this.ctx.currentTime, RAMP_SECONDS);
  }

  private track<T extends AudioScheduledSourceNode>(source: T) {
    this.sources.push(source);
    return source;
  }

  // Kahverengi gurultu
  private buildNoise() {
    const buffer = createNoiseBuffer(this.ctx, 2, (data) => {
      let last = 0;
      for (let i = 0; i < data.length; i++) {
        const white = Math.random() * 2 - 1;
        last = (last + 0.02 * white) / 1.02;
        data[i] = last * 3.5;
      }
    });
    const source = this.track(loopSource(this.ctx, buffer));
    source.connect(this.gains.noise);
    source.start();
  }

  // Yagmur: alcak geciren filtreden gecmis beyaz gurultu
  private buildRain() {
    const buffer = createNoiseBuffer(this.ctx, 2, (data) => {
      for (let i = 0; i < data.length; i++) data[i] = (Math.random() * 2 - 1) * 0.1;
    });
    const source = this.track(loopSource(this.ctx, buffer));
    const filter = this.ctx.createBiquadFilter();
    filter.type = "lowpass";
    filter.frequency.value = 800;
    source.connect(filter);
    filter.connect(this.gains.rain);
    source.start();
  }

  // Vinil: seyrek rastgele darbeler + cok hafif hisirti, bant geciren filtreden
  private buildVinyl() {
    const sampleRate = this.ctx.sampleRate;
    const clicksPerSecond = 7;
    const buffer = createNoiseBuffer(this.ctx, 8, (data) => {
      for (let i = 0; i < data.length; i++) {
        data[i] = (Math.random() * 2 - 1) * 0.015;
        if (Math.random() < clicksPerSecond / sampleRate) {
          const amplitude = (0.4 + Math.random() * 0.6) * (Math.random() < 0.5 ? -1 : 1);
          const tail = 6 + Math.floor(Math.random() * 20);
          for (let k = 0; k < tail && i + k < data.length; k++) {
            data[i + k] += amplitude * Math.exp(-k / 4);
          }
        }
      }
    });
    const source = this.track(loopSource(this.ctx, buffer));
    const bandpass = this.ctx.createBiquadFilter();
    bandpass.type = "bandpass";
    bandpass.frequency.value = 3200;
    bandpass.Q.value = 0.7;
    source.connect(bandpass);
    bandpass.connect(this.gains.vinyl);
    source.start();
  }

  // Lo-Fi: filtrelenmis alcak akor yatagi, yavas LFO ile tel titremesi
  private buildLofi() {
    const filter = this.ctx.createBiquadFilter();
    filter.type = "lowpass";
    filter.frequency.value = 520;
    filter.Q.value = 0.6;
    filter.connect(this.gains.lofi);

    const lfo = this.track(this.ctx.createOscillator());
    lfo.frequency.value = 0.12;
    const lfoDepth = this.ctx.createGain();
    lfoDepth.gain.value = 160;
    lfo.connect(lfoDepth);
    lfoDepth.connect(filter.frequency);
    lfo.start();

    CHORDS[0].forEach((frequency, index) => {
      const osc = this.track(this.ctx.createOscillator());
      osc.type = index % 2 === 0 ? "triangle" : "sine";
      osc.frequency.value = frequency;
      osc.detune.value = (index - 1.5) * 4;
      osc.connect(filter);
      osc.start();
      this.chordOscillators.push(osc);
    });

    this.chordTimer = setInterval(() => {
      this.chordIndex = (this.chordIndex + 1) % CHORDS.length;
      const chord = CHORDS[this.chordIndex];
      this.chordOscillators.forEach((osc, index) => {
        osc.frequency.setTargetAtTime(chord[index], this.ctx.currentTime, 0.8);
      });
    }, CHORD_INTERVAL_MS);
  }
}
