using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Stages the reward for an accepted answer (Nadide flow, short and skippable):
// 1. ascent from the pad, 2. transfer on the orbit map (the trail runs out from the Sun and lights
// each stop it passes), 3. a cut to the arrival station beside the stop's planet.
// A tap jumps straight to the arrival; reduced motion skips the flight. No distances on screen.
public class LaunchDirector : MonoBehaviour
{
    [SerializeField] private SpaceJourney journey;
    [SerializeField] private LaunchCameraRig rig;
    [SerializeField] private RocketVisual rocket;
    [SerializeField] private JuiceDirector juice;
    [SerializeField] private AudioDirector audioDirector;
    [SerializeField] private QuizUI ui;

    private float idleTime;
    private bool onPad = true;
    private bool skipRequested;
    private bool skippable;
    private StopId arrivalStop = StopId.Sun;
    private bool arrived;
    private int flights;

    public static bool ReducedMotion;

    public bool IsPlaying { get; private set; }

    public StopId ReachedStop { get; private set; }

    private void Update()
    {
        if (!IsPlaying)
        {
            if (onPad) IdleShot();
            else if (arrived) ArrivalShot(arrivalStop, 1f);
            return;
        }

        Pointer pointer = Pointer.current;
        if (skippable && pointer != null && pointer.press.wasPressedThisFrame)
        {
            skipRequested = true;
        }
    }

    public void ResetToPad()
    {
        StopAllCoroutines();
        IsPlaying = false;
        onPad = true;
        arrived = false;
        skippable = false;
        skipRequested = false;
        ReachedStop = StopId.Sun;
        juice.ResetTime();
        journey.Altitude = 0f;
        journey.Warp = 0f;
        rocket.ResetToPad();
        audioDirector.SetRumble(0f, 1f);
        audioDirector.SetPad(0.22f);
        ui.SetVeil(0f);
        ui.SetHud(false);
        ui.ClearPops();
        IdleShot();
        rig.Snap();
    }

    // Slow orbit around the rocket on the pad while the player thinks.
    private void IdleShot()
    {
        idleTime += Time.unscaledDeltaTime;
        float yaw = Mathf.Sin(idleTime * 0.12f) * 28f - 30f;
        Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0.1f, 1f);
        rig.SetShot(forward, 175f, 50f, new Vector3(0f, 36f, 0f), new Vector2(0f, -0.2f), 1.5f);
    }

    // Timeout: "Güneş'te kaldın". The count stops on the pad and nothing lights.
    public IEnumerator Scrub()
    {
        IsPlaying = true;
        ui.HideQuiz();
        audioDirector.SetPad(0.1f);
        audioDirector.Wrong();
        rig.SetShot(new Vector3(-0.45f, 0.2f, 0.85f), 90f, 48f, new Vector3(0f, 20f, 0f), Vector2.zero, 2.5f);
        ui.Stamp("FIRLATMA İPTAL", RqStops.Color(StopId.Sun));
        yield return new WaitForSecondsRealtime(ReducedMotion ? 0.3f : 1.1f);
        ReachedStop = StopId.Sun;
        IsPlaying = false;
    }

    public IEnumerator Play(StopId stop, string roundLabel)
    {
        IsPlaying = true;
        onPad = false;
        arrived = false;
        skipRequested = false;
        skippable = false;
        ReachedStop = StopId.Sun;
        bool repeat = flights > 0;
        flights++;

        ui.HideQuiz();
        ui.SetSkipHint(true);
        ui.SetHudRound(roundLabel);
        ui.HudMap.ResetMap(false);
        audioDirector.SetPad(0f);

        if (ReducedMotion)
        {
            Arrive(stop);
            ui.HudMap.Show(stop);
            ui.SetHud(true);
            yield return new WaitForSecondsRealtime(0.3f);
            Finish(stop);
            yield break;
        }

        // Suspense and the "correct" impact.
        rig.SetShot(new Vector3(-0.45f, 0.28f, 0.85f), 75f, 46f, new Vector3(0f, 14f, 0f), Vector2.zero, 3.5f);
        ui.Stamp("DOĞRU!", RqTheme.Success);
        audioDirector.Stinger(0);
        juice.Punch(0.5f);
        rig.AddTrauma(0.3f);
        juice.HitStop(0.06f);
        yield return new WaitForSecondsRealtime(repeat ? 0.35f : 0.6f);
        skippable = true;

        // 1. Ascent.
        Ignite(0.55f, 7f);
        rocket.BurstPadSmoke();
        ui.SetHud(true);
        ui.SetStatus("KALKIŞ");
        Vibrate();
        yield return Fly(0f, JourneyMap.SpaceAltitude, repeat ? 1.5f : 2.4f, 2.4f, true);
        if (skipRequested) { SkipTo(stop); yield break; }

        if (stop == StopId.Earth)
        {
            ui.SetStatus("YÖRÜNGEYE GİRİŞ");
            rocket.SeparateStage1();
            audioDirector.Whoosh(0.8f);
            yield return Fly(JourneyMap.SpaceAltitude, JourneyMap.OrbitAltitude, 1.1f, 1.3f, false);
            if (skipRequested) { SkipTo(stop); yield break; }
            ui.HudMap.Show(stop);
            Arrive(stop);
            yield return Linger();
            Finish(stop);
            yield break;
        }

        // 2. Transfer on the map while the stack climbs on in the background.
        rocket.SeparateStage1();
        audioDirector.Whoosh(0.8f);
        ui.SetStatus("TRANSFER");
        ui.HudMap.StopPassed += OnStopPassed;
        int index = RqStops.MapIndex(stop == StopId.Moon ? StopId.Mars : stop);
        float mapDuration = Mathf.Lerp(0.8f, 3f, (index - 1) / 7f) * (repeat ? 0.7f : 1f);
        Coroutine climb = StartCoroutine(Fly(JourneyMap.SpaceAltitude, JourneyMap.TransferAltitude, mapDuration + 0.5f, 1.6f, false));
        IEnumerator map = ui.HudMap.Run(stop, mapDuration);
        while (map.MoveNext())
        {
            if (skipRequested) break;
            yield return map.Current;
        }
        ui.HudMap.StopPassed -= OnStopPassed;
        StopCoroutine(climb);
        if (skipRequested) { SkipTo(stop); yield break; }
        if (stop == StopId.Moon)
        {
            ui.Stamp("TUZAK!", RqStops.Color(StopId.Moon));
            audioDirector.Wrong();
            yield return new WaitForSeconds(0.5f);
        }

        // 3. Cut to the arrival station and drift in towards the planet.
        yield return ui.FadeBlack(true, 0.12f);
        StageFor(stop);
        float station = JourneyMap.Station(stop);
        float approach = Mathf.Max(60f, (JourneyMap.Body(stop) - new Vector3(0f, station, 0f)).magnitude * 0.12f);
        journey.Altitude = station - approach;
        ArrivalShot(stop, 0f);
        rig.Snap();
        ui.SetStatus("VARIŞ  ·  " + RqStops.ShortName(stop).ToUpper(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")));
        ui.FadeBlack(false, 0.25f);
        rocket.SetThrottle(0.4f);
        float elapsed = 0f;
        const float drift = 1.4f;
        while (elapsed < drift)
        {
            if (skipRequested) { SkipTo(stop); yield break; }
            elapsed += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, elapsed / drift);
            journey.Altitude = Mathf.Lerp(station - approach, station, u);
            ArrivalShot(stop, u);
            yield return null;
        }
        Arrive(stop);
        yield return Linger();
        Finish(stop);
    }

    private IEnumerator Fly(float from, float to, float duration, float power, bool ascent)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (skipRequested) yield break;
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.Pow(Mathf.SmoothStep(0f, 1f, u), power);
            journey.Altitude = Mathf.Lerp(from, to, eased);
            UpdateFlight(u, ascent);
            yield return null;
        }
        journey.Altitude = to;
    }

    private IEnumerator Linger()
    {
        float elapsed = 0f;
        while (elapsed < 0.5f && !skipRequested)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void OnStopPassed(StopId stop)
    {
        ui.Milestone(RqStops.ShortName(stop).ToUpper(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")), RqStops.Color(stop));
        audioDirector.Stinger(RqStops.MapIndex(stop));
        juice.Punch(0.15f);
    }

    private void SkipTo(StopId stop)
    {
        ui.HudMap.StopPassed -= OnStopPassed;
        ui.ClearPops();
        ui.HudMap.Show(stop);
        ui.FadeBlack(false, 0.15f);
        Arrive(stop);
        Finish(stop);
    }

    // Final arrival state: correct staging, station altitude, engines quiet, camera on the planet.
    private void Arrive(StopId stop)
    {
        StageFor(stop);
        journey.Altitude = JourneyMap.Station(stop);
        journey.Warp = 0f;
        rocket.SetThrottle(0f);
        rocket.SetSmoke(0f);
        rocket.SetStreaks(0f);
        rocket.SetVacuum(1f);
        audioDirector.SetRumble(0f, 0.8f);
        ui.SetVeil(0f);
        ui.SetStatus(RqStops.Kicker(stop));
        arrivalStop = stop;
        ArrivalShot(stop, 1f);
        rig.Snap();
        audioDirector.Stinger(RqStops.MapIndex(stop) + 2);
    }

    private void Finish(StopId stop)
    {
        ui.SetSkipHint(false);
        ReachedStop = stop;
        arrived = true;
        skippable = false;
        IsPlaying = false;
    }

    // Deeper stops shed more hardware: orbit keeps the fairing, planets fly the bare upper stage,
    // the giants get the probe alone with its panels out.
    private void StageFor(StopId stop)
    {
        // Staging changes happen behind the cut, so they snap instead of drifting.
        int target = (int)stop >= (int)StopId.Jupiter && stop != StopId.Moon ? 2 : 1;
        rocket.SnapToStage(target, stop != StopId.Earth);
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

    private void UpdateFlight(float u, bool ascent)
    {
        float skyDay = journey.SkyDay;
        float vacuum = Mathf.InverseLerp(2f, 14f, journey.Altitude);
        float pace = Mathf.Sin(u * Mathf.PI);
        rocket.SetVacuum(vacuum);
        rocket.SetThrottle(1f);
        rocket.SetSmoke(170f * skyDay);
        rocket.SetStreaks(ascent ? 60f * pace * skyDay : 160f * pace);
        journey.Warp = ascent ? 0f : 0.35f * pace;

        rig.SetMinimumTrauma((0.22f * skyDay + 0.07f) * (0.5f + 0.5f * pace));
        audioDirector.SetRumble(Mathf.Lerp(0.3f, 0.9f, skyDay) * (0.6f + 0.4f * pace), 0.9f + 0.5f * pace);

        float metres = journey.AltitudeMetres;
        float veil = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1500f, 3300f, metres))
            * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3600f, 6500f, metres)));
        ui.SetVeil(veil * 0.55f);
        if (ascent && metres > 12000f) ui.SetStatus("MAX-Q");
        if (ascent && metres > 50000f) ui.SetStatus("KADEME AYRILMASI");

        Vector3 forward;
        if (ascent)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 0.9f, u));
            forward = Vector3.Slerp(new Vector3(-0.5f, 0.32f, 0.8f), new Vector3(0.5f, -0.22f, 0.84f), t);
        }
        else
        {
            forward = Vector3.Slerp(new Vector3(0.5f, -0.22f, 0.84f), new Vector3(0.45f, -0.5f, 0.74f), u);
        }
        rig.SetShot(forward, 60f + 115f * rocket.FocusSize, 50f, rocket.FocusPoint, new Vector2(0f, 0.04f), 1.8f);
    }

    // Camera behind the vehicle looking past it at the planet (Earth: looking down from orbit).
    private void ArrivalShot(StopId stop, float u)
    {
        Vector3 position = new Vector3(0f, journey.Altitude, 0f);
        Vector3 forward;
        float fov = 52f;
        if (stop == StopId.Earth)
        {
            forward = new Vector3(0.45f, -0.5f, 0.74f);
        }
        else
        {
            Vector3 toBody = (JourneyMap.Body(stop) - position).normalized;
            forward = Vector3.Slerp(new Vector3(0.3f, 0.1f, 0.95f), toBody, Mathf.Lerp(0.6f, 0.85f, u));
            fov = 56f;
        }
        rig.SetShot(forward, 45f + 110f * rocket.FocusSize, fov, rocket.FocusPoint, new Vector2(0f, 0.1f), 2f);
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
