using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace SparklesReborn;

public sealed class RetroAudio : IDisposable
{
    private const int SampleRate = 44100;
    private const int BufferFrames = 2048;
    private const int BufferCount = 4;
    private const uint WAVE_MAPPER = 0xFFFFFFFF;
    private const uint WHDR_DONE = 0x00000001;
    private IntPtr _wave;
    private readonly List<IntPtr> _headers = new();
    private readonly List<IntPtr> _buffers = new();
    private readonly ConcurrentQueue<SynthVoice> _incoming = new();
    private readonly List<SynthVoice> _voices = new();
    private Thread? _thread;
    private volatile bool _running;
    private long _sampleCursor;
    private Biome _biome = Biome.Pasture;
    private volatile float _intensity;
    public float MasterVolume { get; set; } = .34f;
    public bool MusicEnabled { get; set; } = true;

    public RetroAudio()
    {
        try { Start(); } catch { Dispose(); }
    }

    public void SetBiome(Biome biome) => _biome = biome;
    public void SetIntensity(float value) => _intensity = Math.Clamp(value, 0f, 1f);

    public void SfxJump() => Queue(620, .14f, .22f, 420, 0);
    public void SfxPickup() => Queue(1050, .09f, .18f, 1600, 1);
    public void SfxPixie(int streak = 0)
    {
        float lift = Math.Min(1400, Math.Max(0, streak) * 42);
        Queue(1480 + lift, .07f, .16f, 2200 + lift, 0);
    }
    public void SfxJoust() => Queue(170, .12f, .25f, 75, 2);
    public void SfxStomp() => Queue(78, .18f, .32f, 42, 3);
    public void SfxHit() => Queue(110, .22f, .30f, 55, 2);
    public void SfxSecret() { Queue(880, .18f, .25f, 1320, 0); Queue(1320, .22f, .20f, 1760, 1); }
    public void SfxExplosion()
    {
        Queue(56, .65f, .58f, 32, 3);
        Queue(420, .55f, .26f, 70, 2);
        Queue(880, .45f, .18f, 1900, 1);
    }
    public void SfxBoost() => Queue(88, .12f, .11f, 150, 2);
    public void SfxBoss() { Queue(110, .45f, .32f, 82, 2); Queue(55, .65f, .28f, 41, 3); }

    private void Queue(float freq, float life, float volume, float endFreq, int wave)
        => _incoming.Enqueue(new SynthVoice(freq, endFreq, life, volume, wave));

    private void Start()
    {
        WAVEFORMATEX fmt = new()
        {
            wFormatTag = 1,
            nChannels = 2,
            nSamplesPerSec = SampleRate,
            wBitsPerSample = 16,
            nBlockAlign = 4,
            nAvgBytesPerSec = SampleRate * 4,
            cbSize = 0
        };
        if (waveOutOpen(out _wave, WAVE_MAPPER, ref fmt, IntPtr.Zero, IntPtr.Zero, 0) != 0 || _wave == IntPtr.Zero) return;

        int hdrSize = Marshal.SizeOf<WAVEHDR>();
        for (int i = 0; i < BufferCount; i++)
        {
            IntPtr data = Marshal.AllocHGlobal(BufferFrames * 4);
            IntPtr hdrPtr = Marshal.AllocHGlobal(hdrSize);
            WAVEHDR hdr = new() { lpData = data, dwBufferLength = BufferFrames * 4 };
            Marshal.StructureToPtr(hdr, hdrPtr, false);
            waveOutPrepareHeader(_wave, hdrPtr, (uint)hdrSize);
            _buffers.Add(data);
            _headers.Add(hdrPtr);
            Fill(data);
            waveOutWrite(_wave, hdrPtr, (uint)hdrSize);
        }
        _running = true;
        _thread = new Thread(AudioLoop) { IsBackground = true, Name = "Sparkles Retro Synth" };
        _thread.Start();
    }

    private void AudioLoop()
    {
        int hdrSize = Marshal.SizeOf<WAVEHDR>();
        while (_running)
        {
            for (int i = 0; i < _headers.Count; i++)
            {
                WAVEHDR hdr = Marshal.PtrToStructure<WAVEHDR>(_headers[i]);
                if ((hdr.dwFlags & WHDR_DONE) != 0)
                {
                    Fill(_buffers[i]);
                    waveOutWrite(_wave, _headers[i], (uint)hdrSize);
                }
            }
            Thread.Sleep(2);
        }
    }

    private void Fill(IntPtr data)
    {
        while (_incoming.TryDequeue(out var v)) _voices.Add(v);
        short[] pcm = new short[BufferFrames * 2];
        int root = _biome switch
        {
            Biome.Pasture or Biome.Orchard => 62,
            Biome.Forest or Biome.Weald => 57,
            Biome.Marsh => 50,
            Biome.Mountain or Biome.Ice => 55,
            Biome.Coast => 64,
            Biome.Haunted or Biome.Crypt => 48,
            Biome.Cave => 45,
            Biome.Fortress => 43,
            Biome.Night => 46,
            Biome.Nightmare => 41,
            _ => 57
        };
        int bpm = _biome is Biome.Nightmare or Biome.Fortress ? 142 : _biome is Biome.Haunted or Biome.Crypt ? 108 : 126;
        bpm += (int)(42 * _intensity);
        double eighth = SampleRate * 60.0 / bpm / 2.0;
        int[] pattern = _biome is Biome.Haunted or Biome.Crypt or Biome.Nightmare
            ? new[] { 0, 3, 7, 10, 7, 3, -2, 3, 0, 7, 8, 7, 3, 0, -2, -5 }
            : new[] { 0, 4, 7, 12, 7, 4, 9, 7, 0, 4, 7, 14, 12, 9, 7, 4 };

        for (int f = 0; f < BufferFrames; f++, _sampleCursor++)
        {
            double t = _sampleCursor / (double)SampleRate;
            double sample = 0;
            if (MusicEnabled)
            {
                int step = (int)(_sampleCursor / eighth) % pattern.Length;
                double freq = Midi(root + pattern[step]);
                double bass = Midi(root - 24 + (step % 4 == 0 ? 0 : 7));
                double phase = t * freq;
                double lead = 2.0 * (phase - Math.Floor(phase + .5)); // saw
                double sq = Math.Sin(t * bass * Math.PI * 2) >= 0 ? 1 : -1;
                sample += lead * .08 + sq * .055;
                if (_intensity > .45f)
                {
                    double octave = Math.Sin(t * freq * 2 * Math.PI * 2);
                    sample += octave * (.018 + _intensity * .028);
                }

                long within = (long)(_sampleCursor % (long)eighth);
                if (within < SampleRate * .025)
                {
                    double env = 1.0 - within / (SampleRate * .025);
                    sample += Math.Sin(t * (72 + env * 55) * Math.PI * 2) * env * .12;
                }
                if (step % 2 == 1 && within < SampleRate * .016)
                {
                    uint h = (uint)(_sampleCursor * 1664525 + 1013904223);
                    double noise = ((h >> 8) & 0xFFFF) / 32768.0 - 1.0;
                    sample += noise * .035 * (1.0 - within / (SampleRate * .016));
                }
            }

            for (int i = _voices.Count - 1; i >= 0; i--)
            {
                SynthVoice v = _voices[i];
                float p = v.Age / v.Life;
                if (p >= 1) { _voices.RemoveAt(i); continue; }
                double freq = v.Frequency + (v.EndFrequency - v.Frequency) * p;
                double ph = v.Phase;
                double wave = v.Wave switch
                {
                    0 => Math.Sin(ph),
                    1 => Math.Sin(ph) >= 0 ? 1 : -1,
                    2 => 2.0 * (ph / (Math.PI * 2) - Math.Floor(ph / (Math.PI * 2) + .5)),
                    _ => (((uint)(_sampleCursor * 1103515245 + i * 12345) >> 9) & 0x7FFF) / 16384.0 - 1.0
                };
                double env = Math.Pow(1 - p, v.Wave == 3 ? 2.2 : 1.15);
                sample += wave * v.Volume * env;
                v.Phase += (float)(freq * Math.PI * 2 / SampleRate);
                v.Age += 1f / SampleRate;
                _voices[i] = v;
            }

            sample = Math.Tanh(sample * 1.55) * MasterVolume;
            short s = (short)Math.Clamp(sample * short.MaxValue, short.MinValue, short.MaxValue);
            pcm[f * 2] = s;
            pcm[f * 2 + 1] = s;
        }
        Marshal.Copy(pcm, 0, data, pcm.Length);
    }

    private static double Midi(int note) => 440.0 * Math.Pow(2, (note - 69) / 12.0);

    public void Dispose()
    {
        _running = false;
        try { _thread?.Join(120); } catch { }
        if (_wave != IntPtr.Zero)
        {
            try { waveOutReset(_wave); } catch { }
            int hs = Marshal.SizeOf<WAVEHDR>();
            foreach (IntPtr h in _headers) { try { waveOutUnprepareHeader(_wave, h, (uint)hs); } catch { } }
            try { waveOutClose(_wave); } catch { }
            _wave = IntPtr.Zero;
        }
        foreach (IntPtr p in _headers) if (p != IntPtr.Zero) Marshal.FreeHGlobal(p);
        foreach (IntPtr p in _buffers) if (p != IntPtr.Zero) Marshal.FreeHGlobal(p);
        _headers.Clear(); _buffers.Clear();
    }

    private struct SynthVoice
    {
        public float Frequency, EndFrequency, Life, Volume, Age, Phase;
        public int Wave;
        public SynthVoice(float frequency, float endFrequency, float life, float volume, int wave)
        { Frequency = frequency; EndFrequency = endFrequency; Life = life; Volume = volume; Wave = wave; Age = Phase = 0; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WAVEFORMATEX
    {
        public ushort wFormatTag, nChannels;
        public uint nSamplesPerSec, nAvgBytesPerSec;
        public ushort nBlockAlign, wBitsPerSample, cbSize;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct WAVEHDR
    {
        public IntPtr lpData;
        public uint dwBufferLength, dwBytesRecorded;
        public IntPtr dwUser;
        public uint dwFlags, dwLoops;
        public IntPtr lpNext, reserved;
    }
    [DllImport("winmm.dll")] private static extern int waveOutOpen(out IntPtr hwo, uint device, ref WAVEFORMATEX fmt, IntPtr callback, IntPtr instance, uint flags);
    [DllImport("winmm.dll")] private static extern int waveOutPrepareHeader(IntPtr hwo, IntPtr header, uint size);
    [DllImport("winmm.dll")] private static extern int waveOutUnprepareHeader(IntPtr hwo, IntPtr header, uint size);
    [DllImport("winmm.dll")] private static extern int waveOutWrite(IntPtr hwo, IntPtr header, uint size);
    [DllImport("winmm.dll")] private static extern int waveOutReset(IntPtr hwo);
    [DllImport("winmm.dll")] private static extern int waveOutClose(IntPtr hwo);
}
