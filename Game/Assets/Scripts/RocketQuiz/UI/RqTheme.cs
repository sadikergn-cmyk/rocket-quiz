using UnityEngine;

// Design tokens for the "mission control" UI. Builder and runtime views read colours and timings
// from here so the look stays consistent and can be tuned in one place.
public static class RqTheme
{
    // Surfaces: deep navy glass over the 3D scene.
    public static readonly Color PanelTop = new Color(0.055f, 0.08f, 0.14f, 0.88f);
    public static readonly Color PanelBottom = new Color(0.02f, 0.03f, 0.06f, 0.94f);
    public static readonly Color PanelBorder = new Color(0.62f, 0.78f, 1f, 0.22f);
    public static readonly Color Hairline = new Color(0.62f, 0.78f, 1f, 0.14f);

    // Text.
    public static readonly Color TextPrimary = new Color(0.96f, 0.97f, 1f);
    public static readonly Color TextSecondary = new Color(0.70f, 0.77f, 0.88f);
    public static readonly Color TextMuted = new Color(0.50f, 0.57f, 0.68f);

    // Accents. Ignition orange is the only "warm" UI colour, reserved for the launch action.
    public static readonly Color Ignition = new Color(1f, 0.46f, 0.12f);
    public static readonly Color IgnitionDeep = new Color(0.86f, 0.24f, 0.05f);
    public static readonly Color Telemetry = new Color(0.45f, 0.86f, 1f);
    public static readonly Color Success = new Color(0.36f, 0.92f, 0.62f);
    public static readonly Color Danger = new Color(1f, 0.42f, 0.38f);
    public static readonly Color Score = new Color(1f, 0.83f, 0.36f);
    public static readonly Color FuelEmpty = new Color(1f, 1f, 1f, 0.12f);

    // Geometry (reference resolution 1080x1920).
    public const float Gutter = 48f;
    public const float RadiusCard = 36f;
    public const float RadiusControl = 28f;

    // Motion (seconds).
    public const float Fast = 0.16f;
    public const float Medium = 0.28f;
    public const float Slow = 0.45f;
}
