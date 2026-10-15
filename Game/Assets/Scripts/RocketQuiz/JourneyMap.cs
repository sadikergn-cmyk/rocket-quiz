using UnityEngine;

// Layout of the scaled space world. 1 unit = 6.371 km (Earth radius = 1000 units).
// The rocket travels along +Y; every stop has an arrival station (an altitude on that axis) with its
// planet placed beside it, so arrivals are camera cuts rather than one long corridor.
// Internal scale only: no distance is ever shown on screen (Nadide 0020).
public static class JourneyMap
{
    public const float KmPerUnit = 6.371f;
    public const float EarthRadius = 1000f;
    public const float AtmosphereRadius = 1016f;

    public static readonly Vector3 EarthCenter = new Vector3(0f, -EarthRadius, 0f);

    // Direction towards the sun. Bodies sit on the -X/+Z side so their lit face looks at the rocket.
    public static readonly Vector3 SunDirection = new Vector3(0.6f, 0.55f, -0.5f).normalized;

    // Top of the ascent (space) and the parking orbit.
    public const float SpaceAltitude = 16f;
    public const float OrbitAltitude = 160f;
    public const float TransferAltitude = 60f;

    public static readonly Vector3 MoonCenter = new Vector3(-560f, 3950f, 500f);
    public const float MoonRadius = 272f;

    public static readonly Vector3 MercuryCenter = new Vector3(-700f, 90000f, 600f);
    public const float MercuryRadius = 380f;

    public static readonly Vector3 VenusCenter = new Vector3(-1400f, 110000f, 1200f);
    public const float VenusRadius = 950f;

    public static readonly Vector3 MarsCenter = new Vector3(-1100f, 14000f, 900f);
    public const float MarsRadius = 530f;

    public static readonly Vector3 JupiterCenter = new Vector3(-7000f, 30000f, 6000f);
    public const float JupiterRadius = 4500f;

    public static readonly Vector3 SaturnCenter = new Vector3(-14000f, 62000f, 12000f);
    public const float SaturnRadius = 3600f;
    public const float SaturnRingRadius = 8200f;

    public static readonly Vector3 UranusCenter = new Vector3(-5000f, 140000f, 4200f);
    public const float UranusRadius = 1900f;

    public static readonly Vector3 NeptuneCenter = new Vector3(-4800f, 180000f, 4000f);
    public const float NeptuneRadius = 1850f;

    // The Sun hangs far off the Mercury station, towards SunDirection, so it fills that sky.
    public static readonly Vector3 SunCenter = new Vector3(0f, 89600f, 0f) + SunDirection * 30000f;
    public const float SunRadius = 4200f;

    public static float Station(StopId stop)
    {
        switch (stop)
        {
            case StopId.Mercury: return 89600f;
            case StopId.Venus: return 109000f;
            case StopId.Earth: return OrbitAltitude;
            case StopId.Mars: return 13300f;
            case StopId.Jupiter: return 24500f;
            case StopId.Saturn: return 52000f;
            case StopId.Uranus: return 135500f;
            case StopId.Neptune: return 175700f;
            case StopId.Moon: return 3760f;
            default: return 0f;
        }
    }

    public static Vector3 Body(StopId stop)
    {
        switch (stop)
        {
            case StopId.Mercury: return MercuryCenter;
            case StopId.Venus: return VenusCenter;
            case StopId.Mars: return MarsCenter;
            case StopId.Jupiter: return JupiterCenter;
            case StopId.Saturn: return SaturnCenter;
            case StopId.Uranus: return UranusCenter;
            case StopId.Neptune: return NeptuneCenter;
            case StopId.Moon: return MoonCenter;
            default: return EarthCenter;
        }
    }

    public static float ToKilometres(float altitude)
    {
        return altitude * KmPerUnit;
    }
}
