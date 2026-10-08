using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Stages the reward: suspense, ignition, and a flight that only reveals how far it goes as it
// happens. Each milestone looks like the end until the next stage lights (escalation).
public class LaunchDirector : MonoBehaviour
{
    [SerializeField] private SpaceJourney journey;
    [SerializeField] private LaunchCameraRig rig;
    [SerializeField] private RocketVisual rocket;
    [SerializeField] private JuiceDirector juice;
    [SerializeField] private AudioDirector audioDirector;
    [SerializeField] private QuizUI ui;
    [SerializeField] private float[] legDuration = { 4.2f, 3.2f, 4.0f, 6.0f, 5.0f };
    [SerializeField] private float fastForward = 3f;

    private static readonly string[] MilestoneName =
    {
        "UZAYIN SINIRI", "YÖRÜNGE", "AY", "SATÜRN", "ANDROMEDA",
    };

    private float idleTime;
    private bool allowFastForward;
    private bool slowMotion;
    private bool onPad = true;

    public bool IsPlaying { get; private set; }

    // Highest milestone reached in the current or last flight (-1 on the pad).
    public int ReachedTier { get; private set; }

    private void Awake()
    {
        ReachedTier = -1;
    }

    private void Update()
    {
        if (!IsPlaying)
        {
            if (onPad) IdleShot();
            return;
        }

        if (slowMotion)
        {
            juice.Speed = 0.3f;
        }
        else
        {
            Pointer pointer = Pointer.current;
            bool held = allowFastForward && pointer != null && pointer.press.isPressed;
            juice.Speed = held ? fastForward : 1f;
        }
    }

    public void ResetToPad()
    {
        StopAllCoroutines();
        IsPlaying = false;
        onPad = true;
        slowMotion = false;
        allowFastForward = false;
        ReachedTier = -1;
        juice.ResetTime();
        journey.Altitude = 0f;
        journey.Warp = 0f;
        rocket.ResetToPad();
        audioDirector.SetRumble(0f, 1f);
        audioDirector.SetPad(0.22f);
        ui.SetVeil(0f);
        ui.SetHud(false);
        IdleShot();
        rig.Snap();
    }

    // Slow orbit around the rocket on the pad while the player thinks.
    private void IdleShot()
    {
        idleTime += Time.unscaledDeltaTime;
        float yaw = Mathf.Sin(idleTime * 0.12f) * 28f - 30f;
        Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0.1f, 1f);
        rig.SetShot(forward, 165f, 50f, new Vector3(0f, 33f, 0f), new Vector2(0f, -0.2f), 1.5f);
    }

    public IEnumerator Play(int tier)
    {
        IsPlaying = true;
        onPad = false;
        allowFastForward = false;
        ReachedTier = -1;
        tier = Mathf.Clamp(tier, 0, JourneyMap.TierCount - 1);

        // Suspense: the bed drops out and the camera closes in on the engines.
        ui.HideQuiz();
        audioDirector.SetPad(0f);
        rig.SetShot(new Vector3(-0.45f, 0.28f, 0.85f), 70f, 46f, new Vector3(0f, 14f, 0f), Vector2.zero, 3.5f);
        yield return new WaitForSecondsRealtime(0.45f + 0.08f * tier);

        // Confirmation stamp with a hit stop, so the "correct" lands as an impact.
        ui.Stamp("DOĞRU!", new Color(0.45f, 1f, 0.6f));
        audioDirector.Stinger(0);
        juice.Punch(0.5f);
        rig.AddTrauma(0.3f);
        juice.HitStop(0.06f + 0.015f * tier);
        yield return new WaitForSecondsRealtime(0.55f);

        // A beat of near silence, then ignition.
        yield return new WaitForSecondsRealtime(0.22f);
        Ignite(0.55f, 7f);
        rocket.BurstPadSmoke();
        ui.SetHud(true);
        Vibrate();
        yield return new WaitForSeconds(0.7f);
        allowFastForward = true;

        float altitude = 0f;
        for (int leg = 0; leg <= tier; leg++)
        {
            if (leg > 0)
            {
                yield return Escalate(leg);
            }

            float from = altitude;
            float to = JourneyMap.TierAltitude[leg];
            float duration = legDuration[leg];
            float elapsed = 0f;
            bool marsShown = false;
            bool jupiterShown = false;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.Pow(Mathf.SmoothStep(0f, 1f, u), leg == 0 ? 2.4f : 1.35f);
                altitude = Mathf.Lerp(from, to, eased);
                journey.Altitude = altitude;
                float pace = Mathf.Sin(u * Mathf.PI);
                UpdateFlight(leg, u, pace);

                if (leg == 3 && !marsShown && altitude > JourneyMap.MarsCenter.y - 1500f)
                {
                    marsShown = true;
                    PassBody("MARS", new Color(1f, 0.6f, 0.4f), 2);
                }
                if (leg == 3 && !jupiterShown && altitude > JourneyMap.JupiterCenter.y - 6000f)
                {
                    jupiterShown = true;
                    PassBody("JÜPİTER", new Color(1f, 0.85f, 0.6f), 3);
                }
                yield return null;
            }

            altitude = to;
            journey.Altitude = altitude;
            ReachedTier = leg;

            // Arrival: the engine cuts and the destination is named. Nobody knows yet if this is it.
            rocket.SetThrottle(0f);
            rocket.SetSmoke(0f);
            rocket.SetStreaks(0f);
            journey.Warp = 0f;
            audioDirector.SetRumble(0f, 0.8f);
            audioDirector.Stinger(leg + 1);
            ui.Milestone(MilestoneName[leg], RarityTiers.Tint[leg]);
            juice.Punch(0.25f + 0.08f * leg);
            UpdateShot(leg, 1f);

            if (leg < tier)
            {
                yield return new WaitForSeconds(0.75f);
            }
        }

        allowFastForward = false;
        juice.Speed = 1f;
        yield return new WaitForSecondsRealtime(0.9f);
        IsPlaying = false;
    }

    private void Ignite(float trauma, float fovKick)
    {
        rocket.SetThrottle(1f);
        rocket.SetSmoke(170f * journey.SkyDay);
        rig.AddTrauma(trauma);
        rig.KickFov(fovKick);
        juice.Punch(0.7f);
        audioDirector.Thud(0.9f);
        audioDirector.SetRumble(0.85f, 1f);
    }

    // The "it keeps going" beat between milestones.
    private IEnumerator Escalate(int leg)
    {
        if (leg == 1)
        {
            rocket.SeparateStage1();
            audioDirector.Whoosh(0.8f);
            yield return new WaitForSeconds(0.45f);
            Ignite(0.5f, 9f);
        }
        else if (leg == 2)
        {
            rocket.JettisonFairing();
            audioDirector.Whoosh(1.1f);
            yield return new WaitForSeconds(0.5f);
            Ignite(0.6f, 12f);
        }
        else if (leg == 3)
        {
            rocket.SeparateStage2();
            audioDirector.Whoosh(1.3f);
            yield return new WaitForSeconds(0.55f);
            Ignite(0.7f, 15f);
        }
        else
        {
            // Top tier: time slows right before the jump, then the screen whites out once.
            slowMotion = true;
            audioDirector.Whoosh(0.5f);
            rig.KickFov(-10f);
            yield return new WaitForSecondsRealtime(0.7f);
            slowMotion = false;
            juice.Speed = 1f;
            ui.Flash(Color.white, 0.85f, 0.9f);
            juice.ExposureFlash(1.5f);
            Ignite(0.9f, 22f);
        }
        ui.Stamp(leg == JourneyMap.TierCount - 1 ? "IŞIK HIZI!" : "DEVAM EDİYOR!", RarityTiers.Tint[leg]);
        Vibrate();
    }

    private void PassBody(string bodyName, Color color, int step)
    {
        ui.Milestone(bodyName, color);
        audioDirector.Stinger(step);
        juice.Punch(0.2f);
    }

    private void UpdateFlight(int leg, float u, float pace)
    {
        float skyDay = journey.SkyDay;
        float vacuum = Mathf.InverseLerp(2f, 14f, journey.Altitude);
        rocket.SetVacuum(vacuum);
        rocket.SetThrottle(Mathf.Lerp(1f, 0.35f, Mathf.InverseLerp(0.8f, 1f, u)));
        rocket.SetSmoke(170f * skyDay);
        rocket.SetStreaks(leg >= 2 ? 260f * pace : 60f * pace * skyDay);
        journey.Warp = leg == 4 ? pace : (leg == 3 ? 0.55f * pace * pace : 0f);

        // The airframe shakes hardest in thick air and settles in vacuum.
        rig.SetMinimumTrauma((0.22f * skyDay + 0.07f) * (0.5f + 0.5f * pace) + (leg == 4 ? 0.25f * pace : 0f));
        audioDirector.SetRumble(Mathf.Lerp(0.3f, 0.9f, skyDay) * (0.6f + 0.4f * pace), 0.9f + 0.5f * pace + 0.12f * leg);

        // Cloud decks sit between roughly 1.9 and 5.2 km.
        float metres = journey.AltitudeMetres;
        float veil = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1500f, 3300f, metres))
            * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3600f, 6500f, metres)));
        ui.SetVeil(veil * 0.55f);

        ui.SetAltitude(RarityTiers.FormatDistance(DisplayDistance(leg, journey.Altitude)));
        UpdateShot(leg, u);
    }

    // The space world is compressed, so the readout interpolates real distances on a log scale.
    private static double DisplayDistance(int leg, float altitude)
    {
        float from = leg == 0 ? 0f : JourneyMap.TierAltitude[leg - 1];
        float to = JourneyMap.TierAltitude[leg];
        double fromKm = leg == 0 ? 0.0 : RarityTiers.DistanceKm[leg - 1];
        double toKm = RarityTiers.DistanceKm[leg];
        double p = Mathf.InverseLerp(from, to, altitude);
        if (leg == 0)
        {
            return toKm * p;
        }
        return System.Math.Exp(System.Math.Log(fromKm) + (System.Math.Log(toKm) - System.Math.Log(fromKm)) * p);
    }

    private void UpdateShot(int leg, float u)
    {
        Vector3 position = new Vector3(0f, journey.Altitude, 0f);
        Vector3 focus = rocket.FocusPoint;
        float size = rocket.FocusSize;
        float distance = 60f + 115f * size;
        Vector3 forward;
        float fov = 50f;
        Vector2 frame = new Vector2(0f, 0.04f);

        if (leg == 0)
        {
            // From a low angle looking up at liftoff to looking down on the curve of the Earth.
            float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 0.85f, u));
            forward = Vector3.Slerp(new Vector3(-0.5f, 0.32f, 0.8f), new Vector3(0.5f, -0.22f, 0.84f), t);
        }
        else if (leg == 1)
        {
            forward = Vector3.Slerp(new Vector3(0.5f, -0.22f, 0.84f), new Vector3(0.5f, -0.45f, 0.74f), u);
        }
        else if (leg == 2)
        {
            // Watch the Earth shrink, then swing round to the Moon.
            Vector3 toMoon = (JourneyMap.MoonCenter - position).normalized;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.4f, 0.8f, u));
            forward = Vector3.Slerp(new Vector3(0.25f, -0.85f, 0.45f), toMoon, t);
        }
        else if (leg == 3)
        {
            Vector3 target = JourneyMap.SaturnCenter;
            if (journey.Altitude < JourneyMap.MarsCenter.y + 700f) target = JourneyMap.MarsCenter;
            else if (journey.Altitude < JourneyMap.JupiterCenter.y + 4500f) target = JourneyMap.JupiterCenter;
            forward = (target - position).normalized;
            fov = 56f;
        }
        else
        {
            forward = (JourneyMap.GalaxyCenter - position).normalized;
            fov = 58f;
        }

        rig.SetShot(forward, distance, fov, focus, frame, 1.8f);
    }

    private static void Vibrate()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (Application.isMobilePlatform)
        {
            Handheld.Vibrate();
        }
#endif
    }
}
