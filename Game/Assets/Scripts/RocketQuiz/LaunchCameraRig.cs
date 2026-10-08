using UnityEngine;

// Orbit-style shot camera around the rocket with trauma-based shake and FOV kicks.
[DefaultExecutionOrder(50)]
public class LaunchCameraRig : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float shakeDecay = 0.9f;
    [SerializeField] private float maxShakeAngle = 2.2f;
    [SerializeField] private float maxShakeRoll = 3f;
    [SerializeField] private float shakeFrequency = 22f;
    [SerializeField] private float fovKickDecay = 2.2f;

    private Vector3 targetForward = Vector3.forward;
    private float targetDistance = 100f;
    private float targetFov = 50f;
    private Vector3 targetFocus;
    private Vector2 targetFrame;

    private Vector3 forward = Vector3.forward;
    private float distance = 100f;
    private float fov = 50f;
    private Vector3 focus;
    private Vector2 frame;

    private float blendRate = 2f;
    private float trauma;
    private float fovKick;

    // Player-facing accessibility switch.
    public static bool ShakeEnabled = true;

    public float Trauma
    {
        get { return trauma; }
    }

    // frame shifts the rocket on screen, as a fraction of the camera distance (+x right, +y up).
    public void SetShot(Vector3 lookDirection, float shotDistance, float shotFov, Vector3 focusPoint, Vector2 shotFrame, float rate)
    {
        targetForward = lookDirection.normalized;
        targetDistance = shotDistance;
        targetFov = shotFov;
        targetFocus = focusPoint;
        targetFrame = shotFrame;
        blendRate = rate;
    }

    public void Snap()
    {
        forward = targetForward;
        distance = targetDistance;
        fov = targetFov;
        focus = targetFocus;
        frame = targetFrame;
        ApplyPose(0f);
    }

    public void AddTrauma(float amount)
    {
        trauma = Mathf.Clamp01(trauma + amount);
    }

    public void SetMinimumTrauma(float amount)
    {
        trauma = Mathf.Max(trauma, Mathf.Clamp01(amount));
    }

    public void KickFov(float degrees)
    {
        fovKick += degrees;
    }

    private void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        float k = 1f - Mathf.Exp(-blendRate * dt);
        forward = Vector3.Slerp(forward, targetForward, k).normalized;
        distance = Mathf.Lerp(distance, targetDistance, k);
        fov = Mathf.Lerp(fov, targetFov, k);
        focus = Vector3.Lerp(focus, targetFocus, k);
        frame = Vector2.Lerp(frame, targetFrame, k);

        trauma = Mathf.Max(0f, trauma - shakeDecay * dt);
        fovKick = Mathf.Lerp(fovKick, 0f, 1f - Mathf.Exp(-fovKickDecay * dt));
        ApplyPose(Time.unscaledTime);
    }

    private void ApplyPose(float time)
    {
        Vector3 up = Mathf.Abs(forward.y) > 0.98f ? Vector3.forward : Vector3.up;
        Quaternion rotation = Quaternion.LookRotation(forward, up);
        Vector3 position = focus - forward * distance
            - (rotation * Vector3.right) * (frame.x * distance)
            - (rotation * Vector3.up) * (frame.y * distance);

        if (ShakeEnabled && trauma > 0f)
        {
            float shake = trauma * trauma;
            float t = time * shakeFrequency;
            float pitch = (Mathf.PerlinNoise(t, 0.3f) - 0.5f) * 2f * maxShakeAngle * shake;
            float yaw = (Mathf.PerlinNoise(0.7f, t) - 0.5f) * 2f * maxShakeAngle * shake;
            float roll = (Mathf.PerlinNoise(t, t + 5.1f) - 0.5f) * 2f * maxShakeRoll * shake;
            rotation *= Quaternion.Euler(pitch, yaw, roll);
        }

        transform.SetPositionAndRotation(position, rotation);
        if (targetCamera != null)
        {
            targetCamera.fieldOfView = Mathf.Clamp(fov + fovKick, 10f, 120f);
        }
    }
}
