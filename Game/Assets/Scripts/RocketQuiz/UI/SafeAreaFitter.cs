using UnityEngine;

// Keeps this RectTransform inside the device safe area (notch, home indicator). On the web the
// browser reports insets through WebBridge, because Screen.safeArea is the whole canvas there.
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rect;
    private Rect applied;
    private float webTop;
    private float webBottom;

    private void Awake()
    {
        rect = (RectTransform)transform;
    }

    private void OnEnable()
    {
        WebBridge.SafeAreaChanged += OnWebSafeArea;
        if (WebBridge.Instance != null)
        {
            webTop = WebBridge.Instance.SafeTop;
            webBottom = WebBridge.Instance.SafeBottom;
        }
        Apply();
    }

    private void OnDisable()
    {
        WebBridge.SafeAreaChanged -= OnWebSafeArea;
    }

    private void Update()
    {
        Apply();
    }

    private void OnWebSafeArea(float top, float bottom)
    {
        webTop = top;
        webBottom = bottom;
        Apply();
    }

    private void Apply()
    {
        Rect safe = Screen.safeArea;
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            safe = new Rect(0f, Screen.height * webBottom, Screen.width, Screen.height * (1f - webTop - webBottom));
        }

        if (safe == applied || Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        applied = safe;
        rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
