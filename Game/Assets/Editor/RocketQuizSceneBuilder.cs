using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Builds the RocketQuiz scene from code so it can be regenerated at any time.
public static partial class RocketQuizSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/RocketQuiz.unity";
    private const string CoinRollerScenePath = "Assets/Scenes/CoinRoller.unity";
    private const string GeneratedFolder = "Assets/Generated/RocketQuiz";
    private const string TextureFolder = "Assets/Textures/Space";
    private const string SpaceLayerName = "Space";

    // Launch site that ends up directly under the rocket (Ankara).
    private const float SiteLatitude = 39.9f;
    private const float SiteLongitude = 32.9f;

    private static int spaceLayer;

    [MenuItem("Tools/Build RocketQuiz Scene")]
    public static string Build()
    {
        EnsureFolder("Assets", "Generated");
        EnsureFolder("Assets/Generated", "RocketQuiz");
        spaceLayer = EnsureLayer(SpaceLayerName);
        ConfigureTextures();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        ConfigureEnvironment();
        Light sun = CreateSun();

        Camera spaceCamera;
        Camera nearCamera;
        LaunchCameraRig rig;
        CreateCameras(out spaceCamera, out nearCamera, out rig);

        Renderer galaxy = CreateSpaceWorld();
        Transform groundRoot = CreateLaunchSite(out ParticleSystem padSmoke);
        RocketVisual rocket = CreateRocket(padSmoke);
        CreatePostProcessing();

        var journeyGo = new GameObject("SpaceJourney");
        var journey = journeyGo.AddComponent<SpaceJourney>();
        SetReference(journey, "spaceCamera", spaceCamera);
        SetReference(journey, "nearCamera", nearCamera);
        SetReference(journey, "groundRoot", groundRoot);
        SetReference(journey, "galaxy", galaxy);

        BuildGameplay(rocket, rig, journey, nearCamera, sun);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(CoinRollerScenePath, true),
        };
        AssetDatabase.SaveAssets();
        return ScenePath + " built";
    }

    // ---------- environment ----------

    private static void ConfigureEnvironment()
    {
        var skybox = GetMaterial("SpaceSkybox", "RocketQuiz/SpaceSkybox");
        skybox.SetTexture("_StarTex", LoadTexture("stars_milky_way.jpg"));
        RenderSettings.skybox = skybox;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.fog = false;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = GetReflectionCubemap();
        RenderSettings.reflectionIntensity = 1f;
    }

    private static Cubemap GetReflectionCubemap()
    {
        string path = GeneratedFolder + "/Reflection.cubemap";
        var cubemap = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
        if (cubemap == null)
        {
            cubemap = new Cubemap(16, TextureFormat.RGBAHalf, true);
            AssetDatabase.CreateAsset(cubemap, path);
        }

        Color sky = new Color(0.55f, 0.70f, 1.0f);
        Color side = new Color(0.70f, 0.78f, 0.92f);
        Color ground = new Color(0.20f, 0.26f, 0.38f);
        FillFace(cubemap, CubemapFace.PositiveY, sky);
        FillFace(cubemap, CubemapFace.NegativeY, ground);
        FillFace(cubemap, CubemapFace.PositiveX, side);
        FillFace(cubemap, CubemapFace.NegativeX, side);
        FillFace(cubemap, CubemapFace.PositiveZ, side);
        FillFace(cubemap, CubemapFace.NegativeZ, side);
        cubemap.Apply(true);
        EditorUtility.SetDirty(cubemap);
        return cubemap;
    }

    private static void FillFace(Cubemap cubemap, CubemapFace face, Color color)
    {
        var pixels = new Color[cubemap.width * cubemap.width];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        cubemap.SetPixels(pixels, face);
    }

    private static Light CreateSun()
    {
        var go = new GameObject("Sun");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.96f, 0.90f);
        light.intensity = 2.4f;
        light.shadows = LightShadows.None;
        go.transform.rotation = Quaternion.LookRotation(-JourneyMap.SunDirection);
        return light;
    }

    private static void CreateCameras(out Camera spaceCamera, out Camera nearCamera, out LaunchCameraRig rig)
    {
        var spaceGo = new GameObject("Space Camera");
        spaceGo.tag = "MainCamera";
        spaceCamera = spaceGo.AddComponent<Camera>();
        spaceCamera.clearFlags = CameraClearFlags.Skybox;
        spaceCamera.cullingMask = 1 << spaceLayer;
        spaceCamera.nearClipPlane = 0.2f;
        spaceCamera.farClipPlane = 2000000f;
        spaceCamera.allowHDR = true;
        spaceCamera.fieldOfView = 50f;
        UniversalAdditionalCameraData spaceData = spaceCamera.GetUniversalAdditionalCameraData();
        spaceData.renderType = CameraRenderType.Base;
        spaceData.renderPostProcessing = true;

        var nearGo = new GameObject("Near Camera");
        nearCamera = nearGo.AddComponent<Camera>();
        nearCamera.cullingMask = ~(1 << spaceLayer);
        nearCamera.nearClipPlane = 0.5f;
        nearCamera.farClipPlane = 120000f;
        nearCamera.allowHDR = true;
        nearCamera.fieldOfView = 50f;
        nearGo.AddComponent<AudioListener>();
        UniversalAdditionalCameraData nearData = nearCamera.GetUniversalAdditionalCameraData();
        nearData.renderType = CameraRenderType.Overlay;
        nearData.renderPostProcessing = true;
        spaceData.cameraStack.Add(nearCamera);

        rig = nearGo.AddComponent<LaunchCameraRig>();
        SetReference(rig, "targetCamera", nearCamera);
        nearGo.transform.position = new Vector3(60f, 20f, -90f);
        nearGo.transform.LookAt(new Vector3(0f, 33f, 0f));
    }

    private static void CreatePostProcessing()
    {
        string path = GeneratedFolder + "/RocketQuizPost.asset";
        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);

        var bloom = AddOverride<Bloom>(profile);
        bloom.threshold.Override(1.0f);
        bloom.intensity.Override(0.9f);
        bloom.scatter.Override(0.72f);

        var tonemapping = AddOverride<Tonemapping>(profile);
        tonemapping.mode.Override(TonemappingMode.ACES);

        var vignette = AddOverride<Vignette>(profile);
        vignette.intensity.Override(0.26f);
        vignette.smoothness.Override(0.45f);

        var chromatic = AddOverride<ChromaticAberration>(profile);
        chromatic.intensity.Override(0f);

        var color = AddOverride<ColorAdjustments>(profile);
        color.postExposure.Override(0f);
        color.contrast.Override(12f);
        color.saturation.Override(8f);

        var distortion = AddOverride<LensDistortion>(profile);
        distortion.intensity.Override(0f);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        var go = new GameObject("Post Processing");
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;
    }

    private static T AddOverride<T>(VolumeProfile profile) where T : VolumeComponent
    {
        T component = profile.Add<T>(true);
        component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(component, profile);
        return component;
    }

    // ---------- space world ----------

    private static Renderer CreateSpaceWorld()
    {
        var root = new GameObject("Space World");
        Mesh sphere = SaveMesh(RocketQuizMeshes.Sphere("PlanetSphere", 128, 64));
        Mesh plane = SaveMesh(RocketQuizMeshes.Plane("UnitPlane"));

        // Earth, rotated so the launch site sits directly below the rocket.
        Vector3 site = RocketQuizMeshes.SpherePoint((SiteLongitude + 180f) / 360f, (SiteLatitude + 90f) / 180f);
        Quaternion earthRotation = Quaternion.FromToRotation(site, Vector3.up);

        Material earthMat = GetMaterial("Earth", "RocketQuiz/Planet");
        earthMat.SetTexture("_MainTex", LoadTexture("earth_day.jpg"));
        earthMat.SetTexture("_NightTex", LoadTexture("earth_night.jpg"));
        earthMat.SetTexture("_CloudTex", LoadTexture("earth_clouds.jpg"));
        earthMat.SetFloat("_NightStrength", 2.2f);
        earthMat.SetFloat("_CloudStrength", 1f);
        earthMat.SetFloat("_OceanSpec", 1.4f);
        earthMat.SetColor("_AtmoColor", new Color(0.32f, 0.58f, 1f));
        earthMat.SetFloat("_AtmoStrength", 0.9f);
        earthMat.SetFloat("_Exposure", 1.15f);
        earthMat.renderQueue = 2000;
        CreateBody("Earth", root.transform, sphere, earthMat, JourneyMap.EarthCenter, JourneyMap.EarthRadius, earthRotation);

        Material atmoMat = GetMaterial("EarthAtmosphere", "RocketQuiz/Atmosphere");
        atmoMat.SetFloat("_PlanetRadius", JourneyMap.EarthRadius);
        atmoMat.SetFloat("_AtmoRadius", JourneyMap.AtmosphereRadius);
        atmoMat.SetFloat("_Density", 0.03f);
        atmoMat.SetFloat("_Falloff", 4f);
        atmoMat.SetFloat("_Intensity", 2.3f);
        atmoMat.renderQueue = 2001;
        // The shell mesh is oversized; the shader works from the analytic radii.
        CreateBody("Earth Atmosphere", root.transform, sphere, atmoMat, JourneyMap.EarthCenter, JourneyMap.AtmosphereRadius * 1.02f, Quaternion.identity);

        CreateBody("Moon", root.transform, sphere, PlanetMaterial("Moon", "moon.jpg", Color.black, 0f, 1.25f),
            JourneyMap.MoonCenter, JourneyMap.MoonRadius, Quaternion.Euler(0f, 110f, 6f));
        CreateBody("Mars", root.transform, sphere, PlanetMaterial("Mars", "mars.jpg", new Color(0.95f, 0.55f, 0.35f), 0.3f, 1.2f),
            JourneyMap.MarsCenter, JourneyMap.MarsRadius, Quaternion.Euler(0f, 40f, 25f));
        CreateBody("Jupiter", root.transform, sphere, PlanetMaterial("Jupiter", "jupiter.jpg", new Color(0.9f, 0.75f, 0.55f), 0.18f, 1.15f),
            JourneyMap.JupiterCenter, JourneyMap.JupiterRadius, Quaternion.Euler(8f, 200f, 12f));

        Quaternion saturnRotation = Quaternion.Euler(22f, 30f, 14f);
        CreateBody("Saturn", root.transform, sphere, PlanetMaterial("Saturn", "saturn.jpg", new Color(0.95f, 0.85f, 0.6f), 0.15f, 1.15f),
            JourneyMap.SaturnCenter, JourneyMap.SaturnRadius, saturnRotation);

        Material ringMat = GetMaterial("SaturnRing", "RocketQuiz/PlanetRing");
        ringMat.SetTexture("_MainTex", LoadTexture("saturn_ring.png"));
        ringMat.SetFloat("_InnerRadius", JourneyMap.SaturnRadius * 1.24f / JourneyMap.SaturnRingRadius);
        ringMat.SetFloat("_PlanetRadius", JourneyMap.SaturnRadius);
        ringMat.SetFloat("_Exposure", 1.3f);
        CreateBody("Saturn Ring", root.transform, plane, ringMat, JourneyMap.SaturnCenter, JourneyMap.SaturnRingRadius, saturnRotation);

        Material galaxyMat = GetMaterial("Galaxy", "RocketQuiz/Galaxy");
        Vector3 viewer = new Vector3(0f, JourneyMap.TierAltitude[JourneyMap.TierCount - 1], 0f);
        Quaternion facing = Quaternion.LookRotation(viewer - JourneyMap.GalaxyCenter) * Quaternion.Euler(90f, 0f, 0f);
        GameObject galaxyGo = CreateBody("Galaxy", root.transform, plane, galaxyMat, JourneyMap.GalaxyCenter,
            JourneyMap.GalaxySize * 0.5f, facing * Quaternion.Euler(-48f, 25f, 0f));

        SetLayerRecursive(root, spaceLayer);
        return galaxyGo.GetComponent<Renderer>();
    }

    private static Material PlanetMaterial(string name, string texture, Color atmoColor, float atmoStrength, float exposure)
    {
        Material material = GetMaterial(name, "RocketQuiz/Planet");
        material.SetTexture("_MainTex", LoadTexture(texture));
        material.SetFloat("_NightStrength", 0f);
        material.SetFloat("_CloudStrength", 0f);
        material.SetFloat("_OceanSpec", 0f);
        material.SetColor("_AtmoColor", atmoColor);
        material.SetFloat("_AtmoStrength", atmoStrength);
        material.SetFloat("_Exposure", exposure);
        material.renderQueue = 2002;
        return material;
    }

    private static GameObject CreateBody(string name, Transform parent, Mesh mesh, Material material, Vector3 position, float radius, Quaternion rotation)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(position, rotation);
        go.transform.localScale = Vector3.one * radius;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return go;
    }

    // ---------- launch site (metres) ----------

    private static Transform CreateLaunchSite(out ParticleSystem padSmoke)
    {
        var root = new GameObject("Launch Site");
        Mesh plane = SaveMesh(RocketQuizMeshes.Plane("UnitPlane"));

        Material groundMat = GetMaterial("LaunchGround", "RocketQuiz/LaunchGround");
        GameObject ground = CreatePart("Ground", root.transform, plane, groundMat, new Vector3(0f, -1.2f, 0f));
        ground.transform.localScale = new Vector3(60000f, 1f, 60000f);

        Material concrete = GetLit("Concrete", new Color(0.52f, 0.52f, 0.50f), 0f, 0.2f);
        Material steel = GetLit("TowerSteel", new Color(0.36f, 0.38f, 0.41f), 0.4f, 0.45f);

        Mesh apron = SaveMesh(RocketQuizMeshes.Lathe("PadApron", new[]
        {
            new Vector2(0f, -1.2f), new Vector2(46f, -1.2f), new Vector2(42f, -0.4f), new Vector2(0f, -0.4f),
        }, false, 48));
        CreatePart("Pad Apron", root.transform, apron, concrete, Vector3.zero);

        Mesh mount = SaveMesh(RocketQuizMeshes.Lathe("PadMount", new[]
        {
            new Vector2(2.2f, -0.4f), new Vector2(5.5f, -0.4f), new Vector2(4.6f, 1.6f), new Vector2(2.2f, 1.6f), new Vector2(2.2f, -0.4f),
        }, false, 32));
        CreatePart("Pad Mount", root.transform, mount, steel, Vector3.zero);

        CreateBox("Tower", root.transform, steel, new Vector3(-9.5f, 30f, 0f), new Vector3(3.2f, 62f, 3.2f));
        CreateBox("Tower Arm Low", root.transform, steel, new Vector3(-5.2f, 22f, 0f), new Vector3(6f, 1f, 1.4f));
        CreateBox("Tower Arm High", root.transform, steel, new Vector3(-5.4f, 48f, 0f), new Vector3(6.4f, 1f, 1.4f));
        for (int i = 0; i < 4; i++)
        {
            float angle = (45f + 90f * i) * Mathf.Deg2Rad;
            CreateBox("Lightning Mast " + (i + 1), root.transform, steel,
                new Vector3(Mathf.Cos(angle) * 30f, 42f, Mathf.Sin(angle) * 30f), new Vector3(0.4f, 86f, 0.4f));
        }

        float[] heights = { 1900f, 3300f, 5200f };
        float[] coverage = { 0.42f, 0.5f, 0.56f };
        for (int i = 0; i < heights.Length; i++)
        {
            Material cloudMat = GetMaterial("CloudLayer" + (i + 1), "RocketQuiz/CloudLayer");
            cloudMat.SetFloat("_Scale", 0.00045f + 0.0002f * i);
            cloudMat.SetFloat("_Coverage", coverage[i]);
            cloudMat.SetFloat("_Opacity", 0.92f);
            cloudMat.SetVector("_Offset", new Vector4(17f * i, 9f * i, 0f, 0f));
            GameObject layer = CreatePart("Cloud Layer " + (i + 1), root.transform, plane, cloudMat, new Vector3(0f, heights[i], 0f));
            layer.transform.localScale = new Vector3(45000f, 1f, 45000f);
        }

        padSmoke = CreatePadSmoke(root.transform);
        return root.transform;
    }

    private static ParticleSystem CreatePadSmoke(Transform parent)
    {
        var go = new GameObject("Pad Smoke");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        var system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 3.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(22f, 46f);
        main.startSize = new ParticleSystem.MinMaxCurve(11f, 24f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = -0.03f;
        main.maxParticles = 420;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 85f;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 5f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        ParticleSystem.LimitVelocityOverLifetimeModule limit = system.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 5f;
        limit.dampen = 0.05f;

        ParticleSystem.ColorOverLifetimeModule colorModule = system.colorOverLifetime;
        colorModule.enabled = true;
        colorModule.color = MakeGradient(
            new Color(1.15f, 1.0f, 0.85f), 0.6f,
            new Color(0.92f, 0.90f, 0.88f), 0.7f,
            new Color(0.8f, 0.8f, 0.82f), 0f);

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 2.6f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = ParticleMaterial("SmokeParticle", false, 1.4f);
        return system;
    }

    // ---------- rocket (metres, nozzle exit at y = 0) ----------

    private static RocketVisual CreateRocket(ParticleSystem padSmoke)
    {
        var root = new GameObject("Rocket");

        Material white = GetLit("RocketWhite", new Color(0.80f, 0.80f, 0.80f), 0.05f, 0.55f);
        Material dark = GetLit("RocketDark", new Color(0.06f, 0.06f, 0.07f), 0.3f, 0.45f);
        Material metal = GetLit("RocketMetal", new Color(0.42f, 0.40f, 0.38f), 0.9f, 0.62f);
        Material gold = GetLit("ProbeGold", new Color(1.0f, 0.72f, 0.26f), 0.85f, 0.7f);
        Material panel = GetLit("ProbePanel", new Color(0.03f, 0.06f, 0.22f), 0.6f, 0.85f);
        Material fairingMat = GetLit("RocketFairing", new Color(0.80f, 0.80f, 0.80f), 0.05f, 0.55f);
        fairingMat.SetFloat("_Cull", 0f);

        // Stage 1
        var stage1 = new GameObject("Stage 1");
        stage1.transform.SetParent(root.transform, false);
        CreatePart("Body", stage1.transform, SaveMesh(RocketQuizMeshes.Lathe("Stage1Body", new[]
        {
            new Vector2(0f, 3f), new Vector2(1.83f, 3f), new Vector2(1.83f, 40f), new Vector2(0f, 40f),
        }, false)), white, Vector3.zero);
        CreatePart("Engine Skirt", stage1.transform, SaveMesh(RocketQuizMeshes.Lathe("Stage1Skirt", new[]
        {
            new Vector2(0f, 2f), new Vector2(1.95f, 2f), new Vector2(1.95f, 5.6f), new Vector2(1.83f, 5.9f),
        }, false)), dark, Vector3.zero);
        CreatePart("Band", stage1.transform, SaveMesh(RocketQuizMeshes.Lathe("Stage1Band", new[]
        {
            new Vector2(1.84f, 35.5f), new Vector2(1.85f, 35.5f), new Vector2(1.85f, 40f), new Vector2(1.84f, 40f),
        }, false)), dark, Vector3.zero);
        Mesh bell = SaveMesh(RocketQuizMeshes.Lathe("EngineBell", new[]
        {
            new Vector2(0.45f, 2.4f), new Vector2(0.62f, 1.8f), new Vector2(0.92f, 1.0f), new Vector2(1.16f, 0.4f), new Vector2(1.3f, 0f),
        }, true));
        CreatePart("Engine Bell", stage1.transform, bell, metal, Vector3.zero);
        for (int i = 0; i < 4; i++)
        {
            float angle = 90f * i;
            GameObject fin = CreateBox("Fin " + (i + 1), stage1.transform, dark, Vector3.zero, new Vector3(2.4f, 5f, 0.14f));
            fin.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
            fin.transform.localPosition = fin.transform.localRotation * new Vector3(2.9f, 5f, 0f);
        }

        // Stage 2
        var stage2 = new GameObject("Stage 2");
        stage2.transform.SetParent(root.transform, false);
        CreatePart("Body", stage2.transform, SaveMesh(RocketQuizMeshes.Lathe("Stage2Body", new[]
        {
            new Vector2(0f, 40.1f), new Vector2(1.83f, 40.1f), new Vector2(1.83f, 52f), new Vector2(0f, 52f),
        }, false)), white, Vector3.zero);
        CreatePart("Interstage", stage2.transform, SaveMesh(RocketQuizMeshes.Lathe("Stage2Interstage", new[]
        {
            new Vector2(1.84f, 40.1f), new Vector2(1.86f, 40.1f), new Vector2(1.86f, 42.8f), new Vector2(1.84f, 42.8f),
        }, false)), dark, Vector3.zero);
        CreatePart("Engine Bell", stage2.transform, bell, metal, new Vector3(0f, 37.5f, 0f));

        // Fairing halves
        Vector2[] fairingProfile =
        {
            new Vector2(1.83f, 51.6f), new Vector2(2.3f, 53f), new Vector2(2.3f, 59.5f), new Vector2(2.2f, 61.2f),
            new Vector2(1.9f, 62.9f), new Vector2(1.4f, 64.4f), new Vector2(0.8f, 65.5f), new Vector2(0f, 66.1f),
        };
        GameObject fairingLeft = CreatePart("Fairing Left", root.transform,
            SaveMesh(RocketQuizMeshes.Lathe("FairingLeft", fairingProfile, true, 24, 90f, 180f)), fairingMat, Vector3.zero);
        GameObject fairingRight = CreatePart("Fairing Right", root.transform,
            SaveMesh(RocketQuizMeshes.Lathe("FairingRight", fairingProfile, true, 24, 270f, 180f)), fairingMat, Vector3.zero);

        // Probe
        var probe = new GameObject("Probe");
        probe.transform.SetParent(root.transform, false);
        CreateBox("Bus", probe.transform, gold, new Vector3(0f, 55f, 0f), new Vector3(1.7f, 2.2f, 1.7f));
        CreatePart("Dish", probe.transform, SaveMesh(RocketQuizMeshes.Lathe("ProbeDish", new[]
        {
            new Vector2(0f, 56.3f), new Vector2(0.55f, 56.36f), new Vector2(1.05f, 56.58f), new Vector2(1.45f, 56.95f),
        }, true, 24)), fairingMat, Vector3.zero);
        CreatePart("Probe Engine", probe.transform, bell, metal, new Vector3(0f, 52.3f, 0f)).transform.localScale = Vector3.one * 0.55f;
        var panels = new Transform[2];
        for (int i = 0; i < 2; i++)
        {
            float side = i == 0 ? -1f : 1f;
            // Pivot at the bus so the panel unfolds outwards when scaled along X.
            var pivot = new GameObject("Panel Pivot " + (i == 0 ? "L" : "R"));
            pivot.transform.SetParent(probe.transform, false);
            pivot.transform.localPosition = new Vector3(side * 0.85f, 55f, 0f);
            CreateBox("Panel", pivot.transform, panel, new Vector3(side * 2.6f, 0f, 0f), new Vector3(5.2f, 0.07f, 1.5f));
            panels[i] = pivot.transform;
        }

        // Plumes: unit cone hanging down from the nozzle.
        Mesh plume = SaveMesh(RocketQuizMeshes.Lathe("Plume", new[]
        {
            new Vector2(1f, 0f), new Vector2(1.5f, -0.12f), new Vector2(1.7f, -0.3f), new Vector2(1.45f, -0.6f), new Vector2(0.9f, -0.85f), new Vector2(0f, -1f),
        }, true, 32));
        Material exhaustMat = GetMaterial("Exhaust", "RocketQuiz/Exhaust");
        exhaustMat.SetFloat("_Intensity", 7f);
        Material ionMat = GetMaterial("ExhaustIon", "RocketQuiz/Exhaust");
        ionMat.SetColor("_CoreColor", new Color(0.8f, 0.95f, 1f));
        ionMat.SetColor("_EdgeColor", new Color(0.2f, 0.55f, 1f));
        ionMat.SetColor("_VacuumColor", new Color(0.3f, 0.65f, 1f));
        ionMat.SetFloat("_Intensity", 9f);

        Renderer exhaust1 = CreatePlume("Exhaust Stage 1", stage1.transform, plume, exhaustMat, new Vector3(0f, 0.1f, 0f), 1.25f, 62f);
        Renderer exhaust2 = CreatePlume("Exhaust Stage 2", stage2.transform, plume, exhaustMat, new Vector3(0f, 37.6f, 0f), 1.2f, 30f);
        Renderer exhaust3 = CreatePlume("Exhaust Probe", probe.transform, plume, ionMat, new Vector3(0f, 52.3f, 0f), 0.35f, 9f);

        var lightGo = new GameObject("Engine Light");
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, -3f, 0f);
        var engineLight = lightGo.AddComponent<Light>();
        engineLight.type = LightType.Point;
        engineLight.color = new Color(1f, 0.62f, 0.3f);
        engineLight.range = 160f;
        engineLight.intensity = 0f;
        engineLight.shadows = LightShadows.None;
        // Only a marker for position, colour and range; RocketQuiz/Surface reads them as globals.
        engineLight.enabled = false;

        ParticleSystem trail = CreateTrailSmoke(root.transform);
        ParticleSystem streaks = CreateStreaks(root.transform);

        var visual = root.AddComponent<RocketVisual>();
        SetReference(visual, "stage1", stage1.transform);
        SetReference(visual, "stage2", stage2.transform);
        SetReference(visual, "fairingLeft", fairingLeft.transform);
        SetReference(visual, "fairingRight", fairingRight.transform);
        SetReference(visual, "probe", probe.transform);
        SetReferenceArray(visual, "solarPanels", panels);
        SetReference(visual, "exhaustStage1", exhaust1);
        SetReference(visual, "exhaustStage2", exhaust2);
        SetReference(visual, "exhaustProbe", exhaust3);
        SetReference(visual, "trailSmoke", trail);
        SetReference(visual, "padSmoke", padSmoke);
        SetReference(visual, "speedStreaks", streaks);
        SetReference(visual, "engineLight", engineLight);
        return visual;
    }

    private static Renderer CreatePlume(string name, Transform parent, Mesh mesh, Material material, Vector3 position, float radius, float length)
    {
        GameObject go = CreatePart(name, parent, mesh, material, position);
        go.transform.localScale = new Vector3(radius, length, radius);
        return go.GetComponent<Renderer>();
    }

    private static ParticleSystem CreateTrailSmoke(Transform parent)
    {
        var go = new GameObject("Trail Smoke");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, -6f, 0f);
        var system = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = system.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 3.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(70f, 130f);
        main.startSize = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.maxParticles = 700;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 7f;
        shape.radius = 1.1f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        ParticleSystem.ColorOverLifetimeModule colorModule = system.colorOverLifetime;
        colorModule.enabled = true;
        colorModule.color = MakeGradient(
            new Color(1.25f, 0.95f, 0.7f), 0.55f,
            new Color(0.95f, 0.93f, 0.90f), 0.6f,
            new Color(0.78f, 0.78f, 0.80f), 0f);

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.7f, 1f, 4.2f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = ParticleMaterial("SmokeParticle", false, 1f);
        return system;
    }

    private static ParticleSystem CreateStreaks(Transform parent)
    {
        var go = new GameObject("Speed Streaks");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 320f, 0f);
        var system = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = system.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = 0.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(800f, 1500f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        main.startColor = new Color(0.75f, 0.88f, 1f, 0.55f);
        main.maxParticles = 500;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(320f, 320f, 1f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.05f;
        renderer.lengthScale = 1f;
        renderer.sharedMaterial = ParticleMaterial("StreakParticle", true, 2f);
        return system;
    }

    // ---------- helpers ----------

    private static ParticleSystem.MinMaxGradient MakeGradient(Color start, float startAlpha, Color mid, float midAlpha, Color end, float endAlpha)
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(start, 0f), new GradientColorKey(mid, 0.22f), new GradientColorKey(end, 1f) },
            new[] { new GradientAlphaKey(startAlpha, 0f), new GradientAlphaKey(midAlpha, 0.3f), new GradientAlphaKey(endAlpha, 1f) });
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    private static Material ParticleMaterial(string name, bool additive, float boost)
    {
        Material material = GetMaterial(name, "RocketQuiz/Particle");
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_Boost", boost);
        material.SetFloat("_Softness", additive ? 1f : 1.6f);
        return material;
    }

    private static GameObject CreatePart(string name, Transform parent, Mesh mesh, Material material, Vector3 localPosition)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        return go;
    }

    private static GameObject CreateBox(string name, Transform parent, Material material, Vector3 localPosition, Vector3 size)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(box.GetComponent<Collider>());
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localScale = size;
        var renderer = box.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        return box;
    }

    private static Material GetLit(string name, Color color, float metallic, float smoothness)
    {
        // Custom surface shader instead of URP Lit: Lit fails to draw in the WebGL player.
        Material material = GetMaterial(name, "RocketQuiz/Surface");
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        return material;
    }

    private static Material GetMaterial(string name, string shaderName)
    {
        Shader shader = Shader.Find(shaderName);
        if (shader == null)
        {
            throw new System.InvalidOperationException("Shader not found: " + shaderName);
        }
        string path = GeneratedFolder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Mesh SaveMesh(Mesh mesh)
    {
        string path = GeneratedFolder + "/" + mesh.name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
        string name = mesh.name;
        EditorUtility.CopySerialized(mesh, existing);
        existing.name = name;
        EditorUtility.SetDirty(existing);
        Object.DestroyImmediate(mesh);
        return existing;
    }

    private static Texture2D LoadTexture(string file)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/" + file);
        if (texture == null)
        {
            throw new System.InvalidOperationException("Texture not found: " + file);
        }
        return texture;
    }

    private static void ConfigureTextures()
    {
        AssetDatabase.Refresh();
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { TextureFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }
            bool isRing = path.EndsWith("saturn_ring.png");
            bool changed = importer.maxTextureSize != 2048
                || importer.wrapModeU != TextureWrapMode.Repeat
                || importer.wrapModeV != TextureWrapMode.Clamp
                || importer.anisoLevel != 4
                || importer.alphaIsTransparency != isRing;
            if (!changed)
            {
                continue;
            }
            importer.maxTextureSize = 2048;
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.anisoLevel = 4;
            importer.mipmapEnabled = true;
            importer.alphaIsTransparency = isRing;
            importer.SaveAndReimport();
        }
    }

    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0)
        {
            return existing;
        }
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        for (int i = 6; i < layers.arraySize; i++)
        {
            SerializedProperty slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
        }
        throw new System.InvalidOperationException("No free layer slot for " + layerName);
    }

    private static void SetLayerRecursive(GameObject root, int layer)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = layer;
        }
    }

    private static void SetReference(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(field);
        if (property == null)
        {
            throw new System.InvalidOperationException(target.GetType().Name + " has no field " + field);
        }
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetReferenceArray(Object target, string field, Object[] values)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(field);
        if (property == null)
        {
            throw new System.InvalidOperationException(target.GetType().Name + " has no field " + field);
        }
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
