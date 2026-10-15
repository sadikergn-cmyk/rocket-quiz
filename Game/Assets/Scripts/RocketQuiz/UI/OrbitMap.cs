using System.Collections;
using LitMotion;
using TMPro;
using UnityEngine;

// Horizontal solar-system map (Nadide 0017): the Sun on the left, eight stops outward, the Moon
// beside Earth. No distances or scale: only order, names and (optionally) points.
// Used as the tutorial map (Hangar), the flight transfer map (HUD) and the day map (Result).
public class OrbitMap : MonoBehaviour
{
    [SerializeField] private RectTransform track;
    [SerializeField] private PanelGraphic trail;
    [SerializeField] private PanelGraphic sun;
    [SerializeField] private PanelGraphic[] dots;
    [SerializeField] private TMP_Text[] labels;
    [SerializeField] private PanelGraphic moonDot;
    [SerializeField] private TMP_Text moonLabel;
    [SerializeField] private PanelGraphic marker;
    [SerializeField] private PanelGraphic[] dayMarks;

    private MotionHandle motion;

    public event System.Action<StopId> StopPassed;

    private void OnDestroy()
    {
        motion.TryCancel();
    }

    // Horizontal position (0-1 along the track) of a stop; spacing widens outwards like real orbits.
    public static float Position(StopId stop)
    {
        int index = RqStops.MapIndex(stop);
        if (index <= 0) return 0f;
        return 0.1f + 0.9f * Mathf.Pow((index - 1) / 7f, 0.85f);
    }

    public Vector2 LocalPoint(StopId stop)
    {
        Rect r = track.rect;
        Vector2 p = new Vector2(r.xMin + r.width * Position(stop), 0f);
        if (stop == StopId.Moon) p.y = 30f;
        return p;
    }

    public void ResetMap(bool lit)
    {
        motion.TryCancel();
        SetTrail(0f, RqTheme.Ignition);
        for (int i = 0; i < dots.Length; i++)
        {
            StopId stop = (StopId)(i + 1);
            SetDot(dots[i], stop, lit);
            labels[i].color = lit ? RqTheme.TextSecondary : RqTheme.TextMuted;
        }
        SetDot(moonDot, StopId.Moon, lit);
        if (moonLabel != null) moonLabel.color = lit ? RqTheme.TextSecondary : RqTheme.TextMuted;
        if (marker != null) marker.gameObject.SetActive(false);
        if (dayMarks != null)
        {
            for (int i = 0; i < dayMarks.Length; i++) dayMarks[i].gameObject.SetActive(false);
        }
    }

    // Lights the stops one by one as a trail runs out from the Sun; the trap overshoots and snaps back.
    public IEnumerator Run(StopId target, float duration)
    {
        ResetMap(false);
        if (target == StopId.Sun)
        {
            yield break;
        }
        bool trap = target == StopId.Moon;
        float end = trap ? Position(StopId.Mars) + 0.02f : Position(target);
        int lastLit = 0;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 2.2f);
            float reach = end * eased;
            SetTrail(reach, trap ? RqTheme.Ignition : RqStops.Color(target));
            for (int i = lastLit; i < dots.Length; i++)
            {
                StopId stop = (StopId)(i + 1);
                if (Position(stop) > reach + 1e-4f) break;
                Light(stop);
                lastLit = i + 1;
                if (StopPassed != null) StopPassed(stop);
            }
            yield return null;
        }

        if (trap)
        {
            // The trap: the trail collapses back to Earth's orbit and the Moon lights instead.
            float from = end;
            float to = Position(StopId.Earth);
            elapsed = 0f;
            const float back = 0.45f;
            while (elapsed < back)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / back);
                SetTrail(Mathf.Lerp(from, to, t), RqStops.Color(StopId.Moon));
                yield return null;
            }
            for (int i = (int)StopId.Earth; i < dots.Length; i++)
            {
                SetDot(dots[i], (StopId)(i + 1), false);
            }
            Light(StopId.Moon);
            if (StopPassed != null) StopPassed(StopId.Moon);
        }
        ShowMarker(target);
    }

    // Final state without animation (skip / reduced motion).
    public void Show(StopId target)
    {
        ResetMap(false);
        if (target == StopId.Sun) return;
        float reach = target == StopId.Moon ? Position(StopId.Earth) : Position(target);
        SetTrail(reach, RqStops.Color(target));
        for (int i = 0; i < dots.Length; i++)
        {
            StopId stop = (StopId)(i + 1);
            if (Position(stop) <= reach + 1e-4f) Light(stop);
        }
        if (target == StopId.Moon) Light(StopId.Moon);
        ShowMarker(target);
    }

    // Result screen: one mark per question on its stop (the day's "rays").
    public void ShowDay(StopId[] stops)
    {
        ResetMap(true);
        if (dayMarks == null) return;
        for (int i = 0; i < dayMarks.Length; i++)
        {
            bool on = i < stops.Length;
            dayMarks[i].gameObject.SetActive(on);
            if (!on) continue;
            StopId stop = stops[i];
            Vector2 p = LocalPoint(stop);
            p.y = (stop == StopId.Moon ? 30f : 0f) + 20f + 13f * i;
            dayMarks[i].rectTransform.anchoredPosition = p;
            Color c = RqStops.Color(stop);
            dayMarks[i].SetFill(c, c);
        }
    }

    private void Light(StopId stop)
    {
        if (stop == StopId.Moon)
        {
            SetDot(moonDot, stop, true);
            if (moonLabel != null) moonLabel.color = RqStops.Color(stop);
            return;
        }
        int i = (int)stop - 1;
        SetDot(dots[i], stop, true);
        labels[i].color = RqStops.Color(stop);
    }

    private void ShowMarker(StopId stop)
    {
        if (marker == null) return;
        marker.gameObject.SetActive(true);
        marker.rectTransform.anchoredPosition = LocalPoint(stop);
        Color c = RqStops.Color(stop);
        marker.SetStyle(new Color(c.r, c.g, c.b, 0f), new Color(c.r, c.g, c.b, 0f), 999f, 3f, c, 18f, new Color(c.r, c.g, c.b, 0.6f));
    }

    private void SetTrail(float reach, Color color)
    {
        Rect r = track.rect;
        trail.rectTransform.sizeDelta = new Vector2(Mathf.Max(0f, r.width * reach), trail.rectTransform.sizeDelta.y);
        trail.SetStyle(new Color(color.r, color.g, color.b, 0.15f), new Color(color.r, color.g, color.b, 0.95f), 3f, 0f, Color.clear, 10f,
            new Color(color.r, color.g, color.b, 0.35f));
        trail.gameObject.SetActive(reach > 0.001f);
    }

    private static void SetDot(PanelGraphic dot, StopId stop, bool lit)
    {
        if (dot == null) return;
        Color c = RqStops.Color(stop);
        Color fill = lit ? c : new Color(c.r * 0.35f, c.g * 0.35f, c.b * 0.4f, 0.9f);
        dot.SetStyle(fill, fill * 0.8f, 999f, 0f, Color.clear, lit ? 12f : 0f, new Color(c.r, c.g, c.b, lit ? 0.5f : 0f));
    }
}
