using UnityEngine;

// Keeps the rocket at the origin and moves the world instead: the launch site drops away in
// metres on the near camera, while a second camera flies through the scaled space world.
[DefaultExecutionOrder(100)]
public class SpaceJourney : MonoBehaviour
{
    private static readonly int SunDirId = Shader.PropertyToID("_SpaceSunDir");
    private static readonly int SkyDayId = Shader.PropertyToID("_SkyDay");
    private static readonly int NebulaBlendId = Shader.PropertyToID("_NebulaBlend");
    private static readonly int WarpStretchId = Shader.PropertyToID("_WarpStretch");
    private static readonly int FadeId = Shader.PropertyToID("_Fade");

    [SerializeField] private Camera spaceCamera;
    [SerializeField] private Camera nearCamera;
    [SerializeField] private Transform groundRoot;
    [SerializeField] private Renderer galaxy;
    [SerializeField] private Color dayAmbientSky = new Color(0.55f, 0.68f, 0.95f);
    [SerializeField] private Color dayAmbientGround = new Color(0.30f, 0.28f, 0.24f);
    [SerializeField] private Color spaceAmbientSky = new Color(0.05f, 0.055f, 0.07f);
    [SerializeField] private Color spaceAmbientGround = new Color(0.10f, 0.16f, 0.30f);

    private MaterialPropertyBlock galaxyBlock;

    public static SpaceJourney Instance { get; private set; }

    // Height above the launch site in space units (see JourneyMap.KmPerUnit).
    public float Altitude { get; set; }

    // 0-1, smears the star field into streaks.
    public float Warp { get; set; }

    public float AltitudeMetres
    {
        get { return Altitude * JourneyMap.KmPerUnit * 1000f; }
    }

    // 1 on the ground, 0 once the sky has turned black.
    public float SkyDay
    {
        get { return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.5f, 13f, Altitude)); }
    }

    private void Awake()
    {
        Instance = this;
        galaxyBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void LateUpdate()
    {
        Apply();
    }

    public void Apply()
    {
        float metres = AltitudeMetres;
        if (groundRoot != null)
        {
            bool visible = metres < 60000f;
            if (groundRoot.gameObject.activeSelf != visible)
            {
                groundRoot.gameObject.SetActive(visible);
            }
            groundRoot.localPosition = new Vector3(0f, -metres, 0f);
        }

        if (spaceCamera != null && nearCamera != null)
        {
            Transform near = nearCamera.transform;
            float metresPerUnit = JourneyMap.KmPerUnit * 1000f;
            spaceCamera.transform.SetPositionAndRotation(
                new Vector3(0f, Altitude, 0f) + near.position / metresPerUnit,
                near.rotation);
            spaceCamera.fieldOfView = nearCamera.fieldOfView;
            spaceCamera.nearClipPlane = Mathf.Clamp(Altitude * 0.05f, 0.2f, 2000f);
        }

        float skyDay = SkyDay;
        Shader.SetGlobalVector(SunDirId, JourneyMap.SunDirection);
        Shader.SetGlobalFloat(SkyDayId, skyDay);
        Shader.SetGlobalFloat(NebulaBlendId, 0.5f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(70000f, 380000f, Altitude)));
        Shader.SetGlobalFloat(WarpStretchId, Mathf.Clamp01(Warp));

        RenderSettings.ambientSkyColor = Color.Lerp(spaceAmbientSky, dayAmbientSky, skyDay);
        RenderSettings.ambientEquatorColor = Color.Lerp(spaceAmbientSky, dayAmbientSky * 0.8f, skyDay);
        RenderSettings.ambientGroundColor = Color.Lerp(spaceAmbientGround, dayAmbientGround, skyDay);

        if (galaxy != null && galaxyBlock != null)
        {
            galaxyBlock.SetFloat(FadeId, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(60000f, 300000f, Altitude)));
            galaxy.SetPropertyBlock(galaxyBlock);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
