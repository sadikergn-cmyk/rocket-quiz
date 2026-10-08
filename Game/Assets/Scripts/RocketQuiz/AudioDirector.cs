using UnityEngine;

// All sound is synthesised at startup, so the game ships without audio files.
public class AudioDirector : MonoBehaviour
{
    private const int SampleRate = 44100;

    [SerializeField] private float masterVolume = 0.8f;

    private AudioSource padSource;
    private AudioSource rumbleSource;
    private AudioSource[] oneShots;
    private int nextOneShot;

    private AudioClip bell;
    private AudioClip tick;
    private AudioClip thud;
    private AudioClip whoosh;
    private AudioClip buzz;

    private float padTarget = 0.22f;
    private float rumbleTarget;
    private float rumblePitch = 1f;

    private void Awake()
    {
        bell = Bell("Bell", 523.25f, 1.6f);
        tick = Bell("Tick", 1318.5f, 0.12f);
        thud = Sweep("Thud", 130f, 38f, 0.7f);
        whoosh = NoiseSweep("Whoosh", 1.1f);
        buzz = Sweep("Buzz", 160f, 110f, 0.28f);

        padSource = CreateSource("Pad", Pad(), true);
        rumbleSource = CreateSource("Rumble", Rumble(), true);
        padSource.volume = 0f;
        rumbleSource.volume = 0f;
        padSource.Play();
        rumbleSource.Play();

        oneShots = new AudioSource[6];
        for (int i = 0; i < oneShots.Length; i++)
        {
            oneShots[i] = CreateSource("OneShot " + i, null, false);
        }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        padSource.volume = Mathf.MoveTowards(padSource.volume, padTarget * masterVolume, dt * 0.6f);
        rumbleSource.volume = Mathf.Lerp(rumbleSource.volume, rumbleTarget * masterVolume, 1f - Mathf.Exp(-6f * dt));
        rumbleSource.pitch = Mathf.Lerp(rumbleSource.pitch, rumblePitch, 1f - Mathf.Exp(-3f * dt));
    }

    // Music bed under the quiz; ducked before ignition so the launch lands harder.
    public void SetPad(float volume)
    {
        padTarget = volume;
    }

    public void SetRumble(float volume, float pitch)
    {
        rumbleTarget = volume;
        rumblePitch = pitch;
    }

    // Rising semitone ladder: each milestone passed sounds one step higher.
    public void Stinger(int step)
    {
        float pitch = Mathf.Pow(2f, step * 2f / 12f);
        Play(bell, 0.55f, pitch);
        Play(bell, 0.35f, pitch * 1.4983f);
    }

    public void Tick(float progress)
    {
        Play(tick, 0.25f, 0.8f + progress * 0.9f);
    }

    public void Thud(float volume)
    {
        Play(thud, volume, 1f);
    }

    public void Whoosh(float pitch)
    {
        Play(whoosh, 0.5f, pitch);
    }

    public void Wrong()
    {
        Play(buzz, 0.45f, 1f);
    }

    private void Play(AudioClip clip, float volume, float pitch)
    {
        AudioSource source = oneShots[nextOneShot];
        nextOneShot = (nextOneShot + 1) % oneShots.Length;
        source.Stop();
        source.clip = clip;
        source.pitch = pitch;
        source.volume = volume * masterVolume;
        source.Play();
    }

    private AudioSource CreateSource(string sourceName, AudioClip clip, bool loop)
    {
        var go = new GameObject(sourceName);
        go.transform.SetParent(transform, false);
        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        return source;
    }

    private static AudioClip MakeClip(string clipName, float[] samples)
    {
        AudioClip clip = AudioClip.Create(clipName, samples.Length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip Bell(string clipName, float frequency, float duration)
    {
        var samples = new float[(int)(SampleRate * duration)];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / SampleRate;
            float envelope = Mathf.Exp(-t * 5f / duration) * Mathf.Clamp01(t * 200f);
            float phase = 2f * Mathf.PI * frequency * t;
            samples[i] = (Mathf.Sin(phase) * 0.6f + Mathf.Sin(phase * 2f) * 0.25f + Mathf.Sin(phase * 3.01f) * 0.12f) * envelope * 0.7f;
        }
        return MakeClip(clipName, samples);
    }

    private static AudioClip Sweep(string clipName, float startFrequency, float endFrequency, float duration)
    {
        var samples = new float[(int)(SampleRate * duration)];
        float phase = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / samples.Length;
            float frequency = Mathf.Lerp(startFrequency, endFrequency, t);
            phase += 2f * Mathf.PI * frequency / SampleRate;
            float envelope = Mathf.Exp(-t * 4f) * Mathf.Clamp01(t * 400f);
            samples[i] = Mathf.Sin(phase) * envelope * 0.9f;
        }
        return MakeClip(clipName, samples);
    }

    private static AudioClip NoiseSweep(string clipName, float duration)
    {
        var random = new System.Random(7);
        var samples = new float[(int)(SampleRate * duration)];
        float low = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / samples.Length;
            float cutoff = Mathf.Lerp(0.02f, 0.35f, Mathf.Sin(t * Mathf.PI));
            float white = (float)random.NextDouble() * 2f - 1f;
            low += (white - low) * cutoff;
            samples[i] = low * Mathf.Sin(t * Mathf.PI) * 0.9f;
        }
        return MakeClip(clipName, samples);
    }

    private static AudioClip Rumble()
    {
        var random = new System.Random(11);
        int length = SampleRate * 3;
        int fade = SampleRate / 4;
        var raw = new float[length + fade];
        float brown = 0f;
        float low = 0f;
        for (int i = 0; i < raw.Length; i++)
        {
            float white = (float)random.NextDouble() * 2f - 1f;
            brown = Mathf.Clamp(brown + white * 0.04f, -1f, 1f);
            low += (white - low) * 0.12f;
            raw[i] = brown * 0.75f + low * 0.3f;
        }
        // Crossfade the tail into the head so the loop has no click.
        var samples = new float[length];
        for (int i = 0; i < length; i++)
        {
            float value = raw[i];
            if (i < fade)
            {
                float k = (float)i / fade;
                value = raw[i] * k + raw[length + i] * (1f - k);
            }
            samples[i] = value * 0.8f;
        }
        return MakeClip("Rumble", samples);
    }

    private static AudioClip Pad()
    {
        // Frequencies are multiples of 0.25 Hz so a 4 second clip loops seamlessly.
        float[] frequencies = { 110f, 164.75f, 220.25f, 329.5f };
        float[] gains = { 0.30f, 0.22f, 0.16f, 0.08f };
        var samples = new float[SampleRate * 4];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / SampleRate;
            float value = 0f;
            for (int f = 0; f < frequencies.Length; f++)
            {
                value += Mathf.Sin(2f * Mathf.PI * frequencies[f] * t) * gains[f];
            }
            float tremolo = 0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * 0.25f * t);
            samples[i] = value * tremolo * 0.6f;
        }
        return MakeClip("Pad", samples);
    }
}
