using System;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;

// Bridge to the HTML shell (Assets/WebGLTemplates/RocketQuiz, or the Nadide Next.js host): contract v1, see
// BridgeMessages. On the web the answer field is a native <input> overlay, because Unity's in-canvas field does not
// reliably open the iOS keyboard. The page calls the On* methods through SendMessage, so this object must be named
// "WebBridge".
public class WebBridge : MonoBehaviour
{
    public static WebBridge Instance { get; private set; }

    public static event Action<float, float> SafeAreaChanged;

    public event Action<string> AnswerSubmitted;
    // "shared" | "copied" | "cancelled" | "failed", after a share the page took.
    public event Action<string> ShareResult;

    public float SafeTop { get; private set; }
    public float SafeBottom { get; private set; }

    public static bool UsesHtmlInput
    {
        get { return Application.platform == RuntimePlatform.WebGLPlayer; }
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void RQ_Ready();
    [DllImport("__Internal")] private static extern void RQ_ShowAnswer(string placeholder);
    [DllImport("__Internal")] private static extern void RQ_HideAnswer();
    [DllImport("__Internal")] private static extern void RQ_ClearAnswer();
    [DllImport("__Internal")] private static extern int RQ_Share(string json);
    [DllImport("__Internal")] private static extern void RQ_Announce(string text);
    [DllImport("__Internal")] private static extern void RQ_BetaRequired();
#endif

    private void Awake()
    {
        Instance = this;
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.mobileKeyboardSupport = false;
        RQ_Ready();
#endif
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ShowAnswer(string placeholder)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        RQ_ShowAnswer(placeholder);
#endif
    }

    public void HideAnswer()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        RQ_HideAnswer();
#endif
    }

    public void ClearAnswer()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        RQ_ClearAnswer();
#endif
    }

    // Hands the share payload (BridgeMessages.SharePayload) to the page. False when the page has no share function
    // (or outside WebGL): the caller then falls back to its own share text.
    public bool Share(string json)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return RQ_Share(json) == 1;
#else
        return false;
#endif
    }

    // Screen reader line for a stop or the day result (aria-live in the page).
    public void Announce(string text)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        RQ_Announce(text);
#endif
    }

    // The server answered 401 beta_required: the page asks for the access code.
    public void BetaRequired()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        RQ_BetaRequired();
#endif
    }

    // --- Called from JavaScript ---

    public void OnWebAnswer(string text)
    {
        if (AnswerSubmitted != null) AnswerSubmitted(text);
    }

    // "top,bottom" as fractions of the viewport height.
    public void OnWebSafeArea(string csv)
    {
        string[] parts = csv.Split(',');
        if (parts.Length != 2) return;
        float top;
        float bottom;
        if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out top)) return;
        if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out bottom)) return;
        SafeTop = Mathf.Clamp(top, 0f, 0.2f);
        SafeBottom = Mathf.Clamp(bottom, 0f, 0.2f);
        if (SafeAreaChanged != null) SafeAreaChanged(SafeTop, SafeBottom);
    }

    // Hidden tabs are throttled by the browser; pause audio instead of letting it stutter.
    public void OnWebVisibility(string state)
    {
        AudioListener.pause = state == "hidden";
    }

    // prefers-reduced-motion from the page ("1" | "0"): the same flag PlayerPrefs rq.reducedMotion sets at start.
    // Runtime only, not saved; the page sends it again on every load and when the setting changes.
    public void OnWebReducedMotion(string value)
    {
        bool? on = BridgeMessages.Flag(value);
        if (on.HasValue) LaunchDirector.ReducedMotion = on.Value;
    }

    public void OnWebShareResult(string result)
    {
        if (ShareResult != null) ShareResult(result);
    }
}
