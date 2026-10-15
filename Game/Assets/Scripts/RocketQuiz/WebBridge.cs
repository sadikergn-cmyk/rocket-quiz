using System;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;

// Bridge to the HTML shell (Assets/WebGLTemplates/RocketQuiz). On the web the answer field is a
// native <input> overlay, because Unity's in-canvas field does not reliably open the iOS keyboard.
// The page calls the On* methods through SendMessage, so this object must be named "WebBridge".
public class WebBridge : MonoBehaviour
{
    public static WebBridge Instance { get; private set; }

    public static event Action<float, float> SafeAreaChanged;

    public event Action<string> AnswerSubmitted;

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
}
