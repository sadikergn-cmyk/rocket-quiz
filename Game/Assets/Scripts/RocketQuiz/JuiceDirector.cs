using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Central place for game-feel effects: hit stop, time scale, lens pulses.
public class JuiceDirector : MonoBehaviour
{
    [SerializeField] private Volume volume;
    [SerializeField] private float pulseDecay = 3.5f;

    private ChromaticAberration chromatic;
    private Bloom bloom;
    private LensDistortion distortion;
    private ColorAdjustments colorAdjustments;

    private float baseBloom;
    private float chromaticPulse;
    private float bloomPulse;
    private float distortionPulse;
    private float exposurePulse;
    private float speed = 1f;
    private bool frozen;

    // Player-facing accessibility switch for bright pulses.
    public static bool FlashEnabled = true;

    // Playback speed of the launch (fast-forward or slow motion); hit stop overrides it briefly.
    public float Speed
    {
        get { return speed; }
        set
        {
            speed = value;
            ApplyTimeScale();
        }
    }

    private void Awake()
    {
        if (volume != null)
        {
            VolumeProfile profile = volume.profile;
            profile.TryGet(out chromatic);
            profile.TryGet(out bloom);
            profile.TryGet(out distortion);
            profile.TryGet(out colorAdjustments);
            if (bloom != null) baseBloom = bloom.intensity.value;
        }
    }

    private void Update()
    {
        float k = Mathf.Exp(-pulseDecay * Time.unscaledDeltaTime);
        chromaticPulse *= k;
        bloomPulse *= k;
        distortionPulse *= k;
        exposurePulse *= k;

        if (chromatic != null) chromatic.intensity.value = Mathf.Clamp01(chromaticPulse);
        if (bloom != null) bloom.intensity.value = baseBloom + bloomPulse;
        if (distortion != null) distortion.intensity.value = Mathf.Clamp(-distortionPulse, -0.6f, 0f);
        if (colorAdjustments != null) colorAdjustments.postExposure.value = FlashEnabled ? exposurePulse : 0f;
    }

    public void Punch(float strength)
    {
        chromaticPulse = Mathf.Max(chromaticPulse, strength);
        bloomPulse = Mathf.Max(bloomPulse, strength * 2.5f);
        distortionPulse = Mathf.Max(distortionPulse, strength * 0.45f);
    }

    public void ExposureFlash(float stops)
    {
        exposurePulse = Mathf.Max(exposurePulse, stops);
    }

    public void HitStop(float seconds)
    {
        StartCoroutine(HitStopRoutine(seconds));
    }

    public void ResetTime()
    {
        StopAllCoroutines();
        frozen = false;
        speed = 1f;
        ApplyTimeScale();
    }

    private IEnumerator HitStopRoutine(float seconds)
    {
        frozen = true;
        ApplyTimeScale();
        yield return new WaitForSecondsRealtime(seconds);
        frozen = false;
        ApplyTimeScale();
    }

    private void ApplyTimeScale()
    {
        Time.timeScale = frozen ? 0f : speed;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
