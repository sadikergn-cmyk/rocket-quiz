using UnityEngine;

// Layout of the scaled space world. 1 unit = 6.371 km (Earth radius = 1000 units).
// Distances between bodies are compressed for cinematic pacing; the rocket travels along +Y.
public static class JourneyMap
{
    public const float KmPerUnit = 6.371f;
    public const float EarthRadius = 1000f;
    public const float AtmosphereRadius = 1016f;

    public static readonly Vector3 EarthCenter = new Vector3(0f, -EarthRadius, 0f);

    // Direction towards the sun. Bodies sit on the -X/+Z side so their lit face looks at the rocket.
    public static readonly Vector3 SunDirection = new Vector3(0.6f, 0.55f, -0.5f).normalized;

    public static readonly Vector3 MoonCenter = new Vector3(-560f, 3950f, 500f);
    public const float MoonRadius = 272f;

    public static readonly Vector3 MarsCenter = new Vector3(-1100f, 14000f, 900f);
    public const float MarsRadius = 530f;

    public static readonly Vector3 JupiterCenter = new Vector3(-7000f, 30000f, 6000f);
    public const float JupiterRadius = 4500f;

    public static readonly Vector3 SaturnCenter = new Vector3(-14000f, 62000f, 12000f);
    public const float SaturnRadius = 3600f;
    public const float SaturnRingRadius = 8200f;

    public static readonly Vector3 GalaxyCenter = new Vector3(-90000f, 700000f, 90000f);
    public const float GalaxySize = 300000f;

    // Altitude (units) where each rarity tier ends its flight.
    public static readonly float[] TierAltitude = { 16f, 160f, 3760f, 50000f, 430000f };

    public const int TierCount = 5;

    public static float ToKilometres(float altitude)
    {
        return altitude * KmPerUnit;
    }
}
