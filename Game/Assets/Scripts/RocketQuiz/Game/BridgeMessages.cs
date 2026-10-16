using System.Collections.Generic;
using System.Globalization;
using System.Text;

// Payloads and texts of the web bridge contract v1 (WebBridge <-> window.RQ in the page). The Nadide Next.js host
// implements the same contract, so these strings must not drift:
//   share(json)      {"date":"YYYY-MM-DD","dayNumber":n,"total":n,"tiers":[TierId|null, ... at most 5]}
//                    TierId is the server's planet ladder id (mercury ... neptune, trap_moon); null = no stop
//                    ("Güneş'te kaldın"). The host writes the share text and the card URL.
//   announce(text)   a short Turkish sentence for screen readers at each stop and at the result. No distances.
//   OnWebShareResult "shared" | "copied" | "cancelled" | "failed"
//   OnWebReducedMotion "1" | "0"
public static class BridgeMessages
{
    public const int MaxShareTiers = 5;

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    // Dative of each stop name ("...'e ulaştın"), indexed by StopId.
    private static readonly string[] Dative =
    {
        "Güneş'e", "Merkür'e", "Venüs'e", "Dünya'ya", "Mars'a", "Jüpiter'e", "Satürn'e", "Uranüs'e", "Neptün'e", "Ay'a",
    };

    // Server tier id of a stop; null for the Sun (no stop reached).
    public static string TierId(StopId stop)
    {
        return stop == StopId.Sun ? null : RqStops.Id(stop);
    }

    public static string SharePayload(string date, int dayNumber, int total, IList<DayRow> rows)
    {
        var json = new StringBuilder();
        json.Append("{\"date\":").Append(Quote(date ?? string.Empty));
        json.Append(",\"dayNumber\":").Append(dayNumber.ToString(Invariant));
        json.Append(",\"total\":").Append(total.ToString(Invariant));
        json.Append(",\"tiers\":[");
        int count = rows == null ? 0 : rows.Count < MaxShareTiers ? rows.Count : MaxShareTiers;
        for (int i = 0; i < count; i++)
        {
            if (i > 0) json.Append(',');
            string tier = TierId(rows[i].Stop);
            json.Append(tier == null ? "null" : Quote(tier));
        }
        json.Append("]}");
        return json.ToString();
    }

    // "Neptün'e ulaştın, 100 puan." / Moon trap / "Güneş'te kaldın".
    public static string StopAnnouncement(StopId stop, int points, bool onlyYou)
    {
        string p = points.ToString(Invariant);
        string text;
        if (stop == StopId.Sun)
        {
            text = "Süre doldu, Güneş'te kaldın, 0 puan.";
        }
        else if (stop == StopId.Moon)
        {
            text = "Ay tuzağı: Dünya'nın yörüngesinde kaldın, " + p + " puan.";
        }
        else
        {
            text = Dative[(int)stop] + " ulaştın, " + p + " puan.";
        }
        return onlyYou && stop != StopId.Sun ? text + " Şu an bunu yazan tek kişi sensin." : text;
    }

    public static string ResultAnnouncement(int total)
    {
        return "Günün yolculuğu bitti, toplam " + total.ToString(Invariant) + " puan. Puanın gün sonunda kesinleşir.";
    }

    // Share button label for the host's share result; null for an unknown value (label unchanged).
    public static string ShareResultLabel(string result)
    {
        switch (result)
        {
            case "shared": return "PAYLAŞILDI";
            case "copied": return "KOPYALANDI";
            case "cancelled": return "PAYLAŞ";
            case "failed": return "PAYLAŞILAMADI";
            default: return null;
        }
    }

    // "1" / "0" (also "true" / "false"); null when the value is neither.
    public static bool? Flag(string value)
    {
        string v = value == null ? string.Empty : value.Trim().ToLowerInvariant();
        if (v == "1" || v == "true") return true;
        if (v == "0" || v == "false") return false;
        return null;
    }

    private static string Quote(string value)
    {
        var s = new StringBuilder("\"");
        foreach (char c in value)
        {
            switch (c)
            {
                case '"': s.Append("\\\""); break;
                case '\\': s.Append("\\\\"); break;
                case '\n': s.Append("\\n"); break;
                case '\r': s.Append("\\r"); break;
                case '\t': s.Append("\\t"); break;
                default:
                    if (c < ' ') s.Append("\\u").Append(((int)c).ToString("x4", Invariant));
                    else s.Append(c);
                    break;
            }
        }
        return s.Append('"').ToString();
    }
}
