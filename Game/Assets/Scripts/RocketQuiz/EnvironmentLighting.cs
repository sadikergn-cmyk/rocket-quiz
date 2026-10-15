using UnityEngine;

// Publishes the image based lighting used by RocketQuiz/Hull: an HDRI cubemap (prefiltered for
// glossy reflections), its rotation so the HDRI sun lines up with the scene's sun, and intensity.
// Runs in edit mode too, so scenes look right while they are being built.
[ExecuteAlways]
public class EnvironmentLighting : MonoBehaviour
{
    private static readonly int EnvCubeId = Shader.PropertyToID("_EnvCube");
    private static readonly int EnvRotationId = Shader.PropertyToID("_EnvRotation");
    private static readonly int EnvIntensityId = Shader.PropertyToID("_EnvIntensity");
    private static readonly int EnvMipsId = Shader.PropertyToID("_EnvMips");
    private static readonly int SkyDayId = Shader.PropertyToID("_SkyDay");
    private static readonly int FrostId = Shader.PropertyToID("_RocketFrost");

    [SerializeField] private Cubemap environment;
    [SerializeField] private float rotationDegrees;
    [SerializeField] private float intensity = 1f;
    [Tooltip("Sets _SkyDay itself; leave off where SpaceJourney drives the day-to-space fade.")]
    [SerializeField] private bool ownsSkyDay;

    private void OnEnable()
    {
        Apply();
    }

    private void OnValidate()
    {
        Apply();
    }

    // Globals do not survive domain reloads or other scenes overwriting them; refreshing is cheap.
    private void Update()
    {
        Apply();
    }

    private void Apply()
    {
        if (environment == null)
        {
            return;
        }
        Shader.SetGlobalTexture(EnvCubeId, environment);
        Shader.SetGlobalFloat(EnvRotationId, rotationDegrees * Mathf.Deg2Rad);
        Shader.SetGlobalFloat(EnvIntensityId, intensity);
        Shader.SetGlobalFloat(EnvMipsId, environment.mipmapCount);
        if (ownsSkyDay)
        {
            Shader.SetGlobalFloat(SkyDayId, 1f);
            Shader.SetGlobalFloat(FrostId, 1f);
        }
    }
}
