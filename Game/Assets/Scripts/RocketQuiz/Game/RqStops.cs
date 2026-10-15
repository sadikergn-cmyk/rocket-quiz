using UnityEngine;

// Nadide's solar-system ladder (decision 0017): eight planets outward from the Sun, the Moon trap,
// and "Güneş'te kaldın" for a timeout. Numbers match the share-card code (0 empty, 1-8, 9 Moon).
public enum StopId
{
    Sun = 0,
    Mercury = 1,
    Venus = 2,
    Earth = 3,
    Mars = 4,
    Jupiter = 5,
    Saturn = 6,
    Uranus = 7,
    Neptune = 8,
    Moon = 9,
}

public static class RqStops
{
    public const int PlanetCount = 8;

    private static readonly string[] Ids =
    {
        "sun", "mercury", "venus", "earth", "mars", "jupiter", "saturn", "uranus", "neptune", "trap_moon",
    };

    private static readonly string[] Names =
    {
        "Güneş", "Merkür", "Venüs", "Dünya", "Mars", "Jüpiter", "Satürn", "Uranüs", "Neptün", "Ay",
    };

    private static readonly int[] Points = { 0, 5, 10, 15, 25, 40, 55, 75, 100, 15 };

    // Our own palette: each planet's real-world hue, tuned for contrast on the navy UI.
    private static readonly Color[] Colors =
    {
        Hex(0xFFB547), Hex(0xA89F94), Hex(0xE8C27A), Hex(0x4FA3FF), Hex(0xE2643B),
        Hex(0xD9A066), Hex(0xF0D58A), Hex(0x7FE0D8), Hex(0x4C6BFF), Hex(0xC9CCD6),
    };

    private static readonly string[] Marks = { "☀️", "🟤", "🟡", "🌍", "🔴", "🟠", "🪐", "🟢", "🔵", "🌙" };

    private static readonly string[] Kickers =
    {
        "SÜRE DOLDU",
        "1. DURAK · GÜNEŞ'İN YANI BAŞI",
        "2. DURAK · ÇOBAN YILDIZI",
        "3. DURAK · MAVİ GEZEGEN",
        "4. DURAK · TOZ FIRTINALARI",
        "5. DURAK · BÜYÜK KIRMIZI LEKE",
        "6. DURAK · ALTIN HALKALAR",
        "7. DURAK · YAN YATMIŞ DEV",
        "8. DURAK · SİSTEMİN KIYISI",
        "TUZAK · DÜNYA'NIN UYDUSU",
    };

    private static readonly string[] Lines =
    {
        "Süre doldu. Sıradaki soru.",
        "Herkes buradan geçti, yine de yoldasın.",
        "Kalabalıktan biraz uzaklaştın.",
        "Görenler var ama kalabalık değil.",
        "Kızıl gezegen. Buraya kadar gelen az.",
        "Dev gezegen. Bunu az kişi yazdı.",
        "Halkaların arasındasın. Bunu az kişi gördü.",
        "Buz devi. Buraya çok az kişi ulaştı.",
        "Son durak. Bunu gören çok az.",
        "Uzaklara gittiğini sandın; Dünya'nın yörüngesinde kaldın.",
    };

    public static string Id(StopId stop) { return Ids[(int)stop]; }
    public static string Name(StopId stop) { return (int)stop == 0 ? "Güneş'te kaldın" : Names[(int)stop]; }
    public static string ShortName(StopId stop) { return Names[(int)stop]; }
    public static int LadderPoints(StopId stop) { return Points[(int)stop]; }
    public static Color Color(StopId stop) { return Colors[(int)stop]; }
    public static string Mark(StopId stop) { return Marks[(int)stop]; }
    public static string Kicker(StopId stop) { return Kickers[(int)stop]; }
    public static string Line(StopId stop) { return Lines[(int)stop]; }

    // Planet order outward from the Sun; the Moon sits beside Earth on the map.
    public static int MapIndex(StopId stop)
    {
        return stop == StopId.Moon ? (int)StopId.Earth : (int)stop;
    }

    public static StopId FromId(string id)
    {
        for (int i = 0; i < Ids.Length; i++)
        {
            if (Ids[i] == id) return (StopId)i;
        }
        return StopId.Sun;
    }

    // Client-side estimate, used only by the offline mock session: the server decides the stop.
    public static StopId FromShare(float percent)
    {
        if (percent >= 20f) return StopId.Mercury;
        if (percent >= 10f) return StopId.Venus;
        if (percent >= 5f) return StopId.Earth;
        if (percent >= 2.5f) return StopId.Mars;
        if (percent >= 1f) return StopId.Jupiter;
        if (percent >= 0.5f) return StopId.Saturn;
        if (percent >= 0.2f) return StopId.Uranus;
        return StopId.Neptune;
    }

    private static Color Hex(int rgb)
    {
        return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
    }
}
