using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public sealed class NadideResponse
{
    // No HTTP response at all (DNS, refused, timeout, CORS in the browser).
    public bool network;
    public long status;
    public string body;

    public static NadideResponse Offline()
    {
        return new NadideResponse { network = true };
    }
}

// HTTP seam of NadideGameSession; tests swap in a scripted fake.
public interface INadideTransport
{
    // `path` is rooted ("/api/play/next"); `jsonBody` null sends no body. `done` runs on the main thread.
    void Send(string method, string path, string jsonBody, Action<NadideResponse> done);
    void Delay(float seconds, Action then);
}

// UnityWebRequest transport. In WebGL the browser owns cookies (same origin). Elsewhere UnityWebRequest only keeps
// cookies in an in-process cache that an Editor restart loses, so the guest `nadide_device` cookie (set by
// /api/play/start) is kept in a small PlayerPrefs jar and sent back by hand; that is what lets the server
// resume today's game after a restart.
public sealed class NadideTransport : INadideTransport
{
    public void Send(string method, string path, string jsonBody, Action<NadideResponse> done)
    {
        UnityWebRequest request;
        try
        {
            request = new UnityWebRequest(NadideConfig.BaseUrl + path, method);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = NadideConfig.TimeoutSeconds;
            if (jsonBody != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
                request.SetRequestHeader("Content-Type", "application/json");
            }
            request.SetRequestHeader("Accept", "application/json");
#if !UNITY_WEBGL || UNITY_EDITOR
            string cookie = NadideCookieJar.Shared.Header();
            if (cookie != null) request.SetRequestHeader("Cookie", cookie);
#endif
        }
        catch (Exception exception)
        {
            // A malformed base URL counts as "no connection".
            Debug.LogWarning("[nadide] request could not be built: " + exception.Message);
            done(NadideResponse.Offline());
            return;
        }

        request.SendWebRequest().completed += _ =>
        {
            NadideResponse response;
            try
            {
                response = Complete(request);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                response = NadideResponse.Offline();
            }
            finally
            {
                request.Dispose();
            }
            done(response);
        };
    }

    public void Delay(float seconds, Action then)
    {
        NetRunner.After(seconds, then);
    }

    // Drops the guest identity: the next /api/play/start opens a new guest (Editor testing).
    public static void ForgetCookies()
    {
        NadideCookieJar.Shared.Clear();
        UnityWebRequest.ClearCookieCache();
    }

    private static NadideResponse Complete(UnityWebRequest request)
    {
        if (request.result == UnityWebRequest.Result.ConnectionError || request.responseCode == 0)
        {
            return NadideResponse.Offline();
        }
#if !UNITY_WEBGL || UNITY_EDITOR
        Dictionary<string, string> headers = request.GetResponseHeaders();
        if (headers != null)
        {
            foreach (KeyValuePair<string, string> header in headers)
            {
                if (string.Equals(header.Key, "Set-Cookie", StringComparison.OrdinalIgnoreCase))
                {
                    NadideCookieJar.Shared.Absorb(header.Value);
                }
            }
        }
#endif
        return new NadideResponse
        {
            status = request.responseCode,
            body = request.downloadHandler != null ? request.downloadHandler.text : null,
        };
    }
}

// Minimal cookie jar for Editor/native (adapted from the Nadide Unity prototype's CookieJar/BetaAccess).
public sealed class NadideCookieJar
{
    public const string PrefsKey = "rq.nadide.cookies";
    public const string DeviceCookie = "nadide_device";
    public const string BetaCookie = "nadide_beta";
    private const string BetaContext = "nadide-beta-erisim:v1";

    // Set-Cookie values may arrive comma-joined in one header; a cookie starts at the beginning or after a comma.
    private static readonly Regex CookieStart = new Regex(@"(?:^|,)\s*([A-Za-z0-9_\-]+)=([^;,]*)((?:;[^,]*)*)");
    private static readonly Regex MaxAge = new Regex(@";\s*max-age\s*=\s*(-?\d+)", RegexOptions.IgnoreCase);

    private static NadideCookieJar shared;

    private readonly SortedDictionary<string, string> cookies = new SortedDictionary<string, string>(StringComparer.Ordinal);
    private readonly bool persist;

    public NadideCookieJar(bool persist)
    {
        this.persist = persist;
    }

    public static NadideCookieJar Shared
    {
        get
        {
            if (shared == null)
            {
                shared = new NadideCookieJar(true);
                shared.Load(PlayerPrefs.GetString(PrefsKey, string.Empty));
                string code = NadideConfig.BetaCode;
                if (code.Length > 0) shared.Set(BetaCookie, BetaCookieValue(code));
            }
            return shared;
        }
    }

    public string Get(string name)
    {
        string value;
        return cookies.TryGetValue(name, out value) ? value : null;
    }

    public void Set(string name, string value)
    {
        cookies[name] = value;
        Save();
    }

    public void Clear()
    {
        cookies.Clear();
        Save();
    }

    // Applies one or more Set-Cookie values; Max-Age <= 0 deletes the cookie.
    public void Absorb(string setCookie)
    {
        if (string.IsNullOrEmpty(setCookie)) return;
        foreach (Match match in CookieStart.Matches(setCookie))
        {
            string name = match.Groups[1].Value;
            Match age = MaxAge.Match(match.Groups[3].Value);
            if (age.Success && int.Parse(age.Groups[1].Value) <= 0) cookies.Remove(name);
            else cookies[name] = match.Groups[2].Value.Trim();
        }
        Save();
    }

    // Value for the Cookie request header, or null when empty.
    public string Header()
    {
        if (cookies.Count == 0) return null;
        var builder = new StringBuilder();
        foreach (KeyValuePair<string, string> cookie in cookies)
        {
            if (builder.Length > 0) builder.Append("; ");
            builder.Append(cookie.Key).Append('=').Append(cookie.Value);
        }
        return builder.ToString();
    }

    public void Load(string serialized)
    {
        cookies.Clear();
        if (string.IsNullOrEmpty(serialized)) return;
        foreach (string line in serialized.Split('\n'))
        {
            int index = line.IndexOf('=');
            if (index > 0) cookies[line.Substring(0, index)] = line.Substring(index + 1);
        }
    }

    // The gate cookie is HMAC-SHA256(code, context) in base64url (apps/web/src/app/beta/gate.ts).
    public static string BetaCookieValue(string code)
    {
        string key = (code ?? string.Empty).Trim().Normalize(NormalizationForm.FormC);
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
        {
            byte[] digest = hmac.ComputeHash(Encoding.UTF8.GetBytes(BetaContext));
            return Convert.ToBase64String(digest).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }

    private void Save()
    {
        if (!persist) return;
        var builder = new StringBuilder();
        foreach (KeyValuePair<string, string> cookie in cookies)
        {
            builder.Append(cookie.Key).Append('=').Append(cookie.Value).Append('\n');
        }
        PlayerPrefs.SetString(PrefsKey, builder.ToString());
        PlayerPrefs.Save();
    }
}
