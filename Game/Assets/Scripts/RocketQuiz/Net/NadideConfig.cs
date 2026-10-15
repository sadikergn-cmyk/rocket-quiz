using System;
using UnityEngine;

// Where the Nadide API lives. WebGL builds are served by the Nadide Next.js shell, so the API is on the
// page's own origin (the browser then sends the `nadide_device` cookie by itself). Editor and native
// players talk to a configurable server, `npm run dev` by default.
public static class NadideConfig
{
    public const string DefaultBaseUrl = "http://localhost:3000";
    public const string BaseUrlKey = "rq.nadide.baseUrl";
    public const string BetaCodeKey = "rq.nadide.betaCode";

    // Game language (server `lang`); the RocketQuiz build is Turkish only.
    public static string Lang = "tr";
    public static int TimeoutSeconds = 12;

    // Code-level override (tests, debug); wins over PlayerPrefs and the WebGL origin.
    public static string BaseUrlOverride;

    public static string BaseUrl
    {
        get
        {
            if (!string.IsNullOrEmpty(BaseUrlOverride)) return Clean(BaseUrlOverride);
#if UNITY_WEBGL && !UNITY_EDITOR
            return Origin(Application.absoluteURL);
#else
            return Clean(PlayerPrefs.GetString(BaseUrlKey, DefaultBaseUrl));
#endif
        }
    }

    // Closed-beta access code (server `BETA_ACCESS_CODE`); empty when the server has no gate.
    // Only used by the Editor/native cookie jar: in WebGL the shell's /beta page sets the cookie.
    public static string BetaCode
    {
        get { return PlayerPrefs.GetString(BetaCodeKey, string.Empty).Trim(); }
    }

    // "https://host:port" of a page URL; empty for file:// or unknown (requests then stay relative).
    public static string Origin(string pageUrl)
    {
        Uri uri;
        if (string.IsNullOrEmpty(pageUrl) || !Uri.TryCreate(pageUrl, UriKind.Absolute, out uri)) return string.Empty;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return string.Empty;
        return uri.GetLeftPart(UriPartial.Authority);
    }

    private static string Clean(string url)
    {
        return (url ?? string.Empty).Trim().TrimEnd('/');
    }
}
