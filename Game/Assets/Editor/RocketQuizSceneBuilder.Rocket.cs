using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static RocketQuizRocketTextures;

// The RocketQuiz launcher (metres, first-stage nozzle exits at y = 0). A modern two-stage reusable
// rocket: 9-engine first stage with folding landing legs and titanium grid fins, carbon interstage,
// vacuum-engine second stage and a 5.2 m payload fairing. Our own livery; no real-world branding.
public static partial class RocketQuizSceneBuilder
{
    private const string RocketFolder = "Assets/Generated/RocketQuiz/Rocket";
    private const string HdriPath = "Assets/ThirdParty/PolyHaven/blouberg_sunrise_2_2k.hdr";

    private const float BodyRadius = 1.83f;
    private const float FairingRadius = 2.6f;

    // The livery faces the default launch camera; legs and fins straddle it.
    private const float FrontDegrees = -56f;
    private static readonly float[] LegDegrees = { FrontDegrees + 45f, FrontDegrees + 135f, FrontDegrees + 225f, FrontDegrees + 315f };

    private static Mesh unitCube;

    private static RocketVisual CreateRocket(ParticleSystem padSmoke)
    {
        RocketQuizUiAssets.Ensure();
        EnsureFolder("Assets/Generated/RocketQuiz", "Rocket");
        DeleteLegacyRocketAssets();

        Texture2D grime = Grime();
        Texture2D detail = DetailNormal();
        float liveryOffset = FrontDegrees / 360f;

        // ---- materials ----
        Material s1Hull = HullMaterial("HullStage1", grime, detail, 0, new Color(0.86f, 0.86f, 0.85f), 0f, 0.36f);
        s1Hull.SetFloat("_SeamSpacing", 2.95f);
        s1Hull.SetFloat("_SeamOffset", 1.62f);
        s1Hull.SetVector("_FrostRange", new Vector4(19f, 39.5f, 1f, 0f));
        s1Hull.SetVector("_SootRange", new Vector4(1.6f, 13f, 0.7f, 0f));
        s1Hull.SetFloat("_Grime", 0.3f);
        SetLivery(s1Hull, LiveryStage1(), 3.6f, 41f, liveryOffset);

        Material interstage = HullMaterial("Interstage", grime, detail, 2, new Color(0.075f, 0.075f, 0.08f), 0f, 0.42f);
        interstage.SetFloat("_SeamSpacing", 1.625f);
        interstage.SetFloat("_SeamOffset", 41f);
        SetLivery(interstage, LiveryInterstage(), 41f, 47.5f, liveryOffset);

        Material s2Hull = HullMaterial("HullStage2", grime, detail, 0, new Color(0.87f, 0.87f, 0.86f), 0f, 0.34f);
        s2Hull.SetFloat("_SeamSpacing", 3.4f);
        s2Hull.SetFloat("_SeamOffset", 47.6f);
        s2Hull.SetVector("_FrostRange", new Vector4(49.5f, 57.5f, 0.9f, 0f));
        s2Hull.SetFloat("_Grime", 0.15f);
        SetLivery(s2Hull, LiveryStage2(), 47.6f, 59.2f, liveryOffset);

        Material fairing = HullMaterial("Fairing", grime, detail, 0, new Color(0.9f, 0.9f, 0.89f), 0f, 0.3f);
        fairing.SetFloat("_SeamSpacing", 3.05f);
        fairing.SetFloat("_SeamOffset", 60.45f);
        fairing.SetFloat("_PanelCount", 2f);
        fairing.SetFloat("_PanelPhase", 0.25f);
        fairing.SetFloat("_Grime", 0.1f);
        fairing.SetFloat("_Cull", 0f);
        SetLivery(fairing, LiveryFairing(), 60.45f, 66.5f, liveryOffset);

        Material raceway = HullMaterial("Raceway", grime, detail, 1, new Color(0.62f, 0.62f, 0.62f), 0f, 0.5f);
        Material heatShield = HullMaterial("HeatShield", grime, detail, 1, new Color(0.13f, 0.12f, 0.115f), 0f, 0.92f);
        Material carbonLeg = HullMaterial("LegCarbon", grime, detail, 2, new Color(0.06f, 0.06f, 0.065f), 0f, 0.4f);
        carbonLeg.SetFloat("_SeamSpacing", 2.2f);
        Material footPad = HullMaterial("LegFoot", grime, detail, 1, new Color(0.42f, 0.41f, 0.4f), 0.9f, 0.45f);
        Material titanium = HullMaterial("GridFinTitanium", grime, detail, 1, new Color(0.48f, 0.46f, 0.44f), 1f, 0.42f);
        Material actuator = HullMaterial("Actuator", grime, detail, 1, new Color(0.2f, 0.2f, 0.21f), 0.8f, 0.4f);

        Material merlin = HullMaterial("EngineBell", grime, detail, 3, new Color(0.13f, 0.125f, 0.12f), 0.8f, 0.48f);
        merlin.SetColor("_HeatColor", new Color(0.2f, 0.19f, 0.24f));
        merlin.SetFloat("_NozzleHeight", 1.85f);
        merlin.SetFloat("_Cull", 0f);
        Material vacuumBell = HullMaterial("VacuumBell", grime, detail, 3, new Color(0.21f, 0.19f, 0.18f), 0.75f, 0.45f);
        vacuumBell.SetColor("_HeatColor", new Color(0.30f, 0.22f, 0.17f));
        vacuumBell.SetFloat("_NozzleHeight", 5.35f);
        vacuumBell.SetFloat("_Cull", 0f);

        Material gold = HullMaterial("ProbeFoil", grime, detail, 1, new Color(1f, 0.72f, 0.28f), 0.95f, 0.32f);
        Material solar = HullMaterial("ProbeSolar", grime, detail, 1, new Color(0.03f, 0.06f, 0.2f), 0.6f, 0.22f);
        Material dishWhite = HullMaterial("ProbeDish", grime, detail, 1, new Color(0.85f, 0.85f, 0.84f), 0f, 0.4f);
        dishWhite.SetFloat("_Cull", 0f);

        var root = new GameObject("Rocket");

        // ---- stage 1 ----
        var stage1 = new GameObject("Stage 1");
        stage1.transform.SetParent(root.transform, false);
        CreateRocketPart("Tank", stage1.transform, SaveMesh(RocketQuizMeshes.Lathe("RQ_S1Tank", new[]
        {
            new Vector2(BodyRadius, 1.62f), new Vector2(BodyRadius, 41f), new Vector2(1.55f, 41.45f),
            new Vector2(1.05f, 41.8f), new Vector2(0f, 42f),
        }, true, 96)), s1Hull, Vector3.zero);
        CreateRocketPart("Heat Shield", stage1.transform, SaveMesh(RocketQuizMeshes.Lathe("RQ_HeatShield", new[]
        {
            new Vector2(0f, 1.55f), new Vector2(1.845f, 1.55f), new Vector2(1.845f, 1.68f),
        }, false, 96)), heatShield, Vector3.zero);
        CreateRocketPart("Skirt Band", stage1.transform, SaveMesh(RocketQuizMeshes.Lathe("RQ_SkirtBand", new[]
        {
            new Vector2(BodyRadius, 3.52f), new Vector2(1.85f, 3.55f), new Vector2(1.85f, 3.66f), new Vector2(BodyRadius, 3.69f),
        }, false, 96)), raceway, Vector3.zero);

        CreateRocketPart("Raceway", stage1.transform, SaveMesh(BuildRaceway("RQ_S1Raceway", 4.2f, 40.6f)), raceway, Vector3.zero);

        Mesh bell = SaveMesh(RocketQuizMeshes.Lathe("RQ_MerlinBell", new[]
        {
            new Vector2(0.46f, 0f), new Vector2(0.405f, 0.35f), new Vector2(0.33f, 0.7f), new Vector2(0.24f, 1.0f),
            new Vector2(0.16f, 1.24f), new Vector2(0.135f, 1.32f), new Vector2(0.17f, 1.4f), new Vector2(0.2f, 1.5f),
            new Vector2(0.2f, 1.74f), new Vector2(0.12f, 1.85f),
        }, true, 40));
        var bells = new List<Transform>();
        bells.Add(CreateRocketPart("Engine 1", stage1.transform, bell, merlin, Vector3.zero).transform);
        for (int i = 0; i < 8; i++)
        {
            float a = (22.5f + 45f * i) * Mathf.Deg2Rad;
            bells.Add(CreateRocketPart("Engine " + (i + 2), stage1.transform, bell, merlin,
                new Vector3(Mathf.Cos(a) * 1.22f, 0f, Mathf.Sin(a) * 1.22f)).transform);
        }

        Mesh legMesh = SaveMesh(BuildLeg("RQ_Leg"));
        Mesh footMesh = SaveMesh(BoxMesh("RQ_LegFoot", new Vector3(0f, 10.3f, 0.17f), new Vector3(0.5f, 0.42f, 0.3f)));
        var legs = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            Vector3 radial = Radial(LegDegrees[i]);
            var pivot = new GameObject("Leg " + (i + 1));
            pivot.transform.SetParent(stage1.transform, false);
            pivot.transform.localPosition = radial * (BodyRadius + 0.03f) + Vector3.up * 2.05f;
            pivot.transform.localRotation = Quaternion.LookRotation(radial, Vector3.up);
            CreateRocketPart("Strut", pivot.transform, legMesh, carbonLeg, Vector3.zero);
            CreateRocketPart("Foot", pivot.transform, footMesh, footPad, Vector3.zero);
            legs[i] = pivot.transform;
        }

        var interstageGo = CreateRocketPart("Interstage", stage1.transform, SaveMesh(RocketQuizMeshes.Lathe("RQ_Interstage", new[]
        {
            new Vector2(1.86f, 41f), new Vector2(1.86f, 47.5f), new Vector2(1.8f, 47.5f), new Vector2(1.8f, 41.2f),
        }, false, 96)), interstage, Vector3.zero);
        interstageGo.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.TwoSided;

        Mesh finMesh = SaveMesh(BuildGridFin("RQ_GridFin"));
        Mesh mountMesh = SaveMesh(BoxMesh("RQ_FinMount", new Vector3(0f, 0.15f, -0.05f), new Vector3(0.5f, 0.5f, 0.22f)));
        var fins = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            Vector3 radial = Radial(LegDegrees[i]);
            var pivot = new GameObject("Grid Fin " + (i + 1));
            pivot.transform.SetParent(stage1.transform, false);
            pivot.transform.localPosition = radial * 1.96f + Vector3.up * 45.75f;
            pivot.transform.localRotation = Quaternion.LookRotation(radial, Vector3.up);
            CreateRocketPart("Mount", pivot.transform, mountMesh, actuator, Vector3.zero);
            var fin = new GameObject("Fin");
            fin.transform.SetParent(pivot.transform, false);
            CreateRocketPart("Lattice", fin.transform, finMesh, titanium, Vector3.zero);
            fins[i] = fin.transform;
        }

        // ---- stage 2 ----
        var stage2 = new GameObject("Stage 2");
        stage2.transform.SetParent(root.transform, false);
        CreateRocketPart("Tank", stage2.transform, SaveMesh(RocketQuizMeshes.Lathe("RQ_S2Tank", new[]
        {
            new Vector2(0f, 46.9f), new Vector2(0.9f, 47.05f), new Vector2(1.45f, 47.3f), new Vector2(BodyRadius, 47.6f),
            new Vector2(BodyRadius, 59.2f),
        }, true, 96)), s2Hull, Vector3.zero);
        CreateRocketPart("Raceway", stage2.transform, SaveMesh(BuildRaceway("RQ_S2Raceway", 48.2f, 58.9f)), raceway, Vector3.zero);
        GameObject vacuum = CreateRocketPart("Vacuum Engine", stage2.transform, SaveMesh(RocketQuizMeshes.Lathe("RQ_VacuumBell", new[]
        {
            new Vector2(1.5f, 0f), new Vector2(1.35f, 0.8f), new Vector2(1.1f, 1.7f), new Vector2(0.8f, 2.6f),
            new Vector2(0.5f, 3.4f), new Vector2(0.28f, 4.05f), new Vector2(0.2f, 4.3f), new Vector2(0.26f, 4.45f),
            new Vector2(0.3f, 4.7f), new Vector2(0.3f, 5.2f), new Vector2(0.2f, 5.35f),
        }, true, 64)), vacuumBell, new Vector3(0f, 41.3f, 0f));

        // ---- fairing ----
        Vector2[] fairingProfile =
        {
            new Vector2(BodyRadius, 59.2f), new Vector2(2.15f, 59.7f), new Vector2(FairingRadius, 60.45f), new Vector2(FairingRadius, 66.5f),
            new Vector2(2.55f, 67.6f), new Vector2(2.4f, 68.7f), new Vector2(2.12f, 69.7f), new Vector2(1.72f, 70.6f),
            new Vector2(1.2f, 71.4f), new Vector2(0.62f, 72f), new Vector2(0.18f, 72.38f), new Vector2(0f, 72.45f),
        };
        GameObject fairingLeft = CreateRocketPart("Fairing Left", root.transform,
            SaveMesh(RocketQuizMeshes.Lathe("RQ_FairingLeft", fairingProfile, true, 56, 90.3f, 179.4f)), fairing, Vector3.zero);
        GameObject fairingRight = CreateRocketPart("Fairing Right", root.transform,
            SaveMesh(RocketQuizMeshes.Lathe("RQ_FairingRight", fairingProfile, true, 56, 270.3f, 179.4f)), fairing, Vector3.zero);

        // ---- probe (inside the fairing) ----
        var probe = new GameObject("Probe");
        probe.transform.SetParent(root.transform, false);
        CreateRocketPart("Bus", probe.transform, SaveMesh(BoxMesh("RQ_ProbeBus", new Vector3(0f, 62.6f, 0f), new Vector3(1.7f, 2.2f, 1.7f))), gold, Vector3.zero);
        CreateRocketPart("Dish", probe.transform, SaveMesh(RocketQuizMeshes.Lathe("RQ_ProbeDish", new[]
        {
            new Vector2(0f, 63.72f), new Vector2(0.55f, 63.78f), new Vector2(1.05f, 64f), new Vector2(1.45f, 64.37f),
        }, true, 32)), dishWhite, Vector3.zero);
        CreateRocketPart("Probe Engine", probe.transform, bell, merlin, new Vector3(0f, 60.2f, 0f)).transform.localScale = Vector3.one * 0.55f;
        Mesh panelMesh = SaveMesh(BoxMesh("RQ_SolarPanel", new Vector3(2.6f, 0f, 0f), new Vector3(5.2f, 0.07f, 1.5f)));
        var panels = new Transform[2];
        for (int i = 0; i < 2; i++)
        {
            float side = i == 0 ? -1f : 1f;
            var pivot = new GameObject("Panel Pivot " + (i == 0 ? "L" : "R"));
            pivot.transform.SetParent(probe.transform, false);
            pivot.transform.localPosition = new Vector3(side * 0.85f, 62.6f, 0f);
            pivot.transform.localRotation = Quaternion.Euler(0f, i == 0 ? 180f : 0f, 0f);
            CreateRocketPart("Panel", pivot.transform, panelMesh, solar, Vector3.zero);
            // Folded inside the fairing until RocketVisual deploys them.
            pivot.transform.localScale = new Vector3(0f, 1f, 1f);
            panels[i] = pivot.transform;
        }

        // ---- plumes, light, particles ----
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

        Renderer exhaust1 = CreatePlume("Exhaust Stage 1", stage1.transform, plume, exhaustMat, new Vector3(0f, 0.05f, 0f), 1.7f, 62f);
        Renderer exhaust2 = CreatePlume("Exhaust Stage 2", stage2.transform, plume, exhaustMat, new Vector3(0f, 41.35f, 0f), 1.15f, 30f);
        Renderer exhaust3 = CreatePlume("Exhaust Probe", probe.transform, plume, ionMat, new Vector3(0f, 60.2f, 0f), 0.35f, 9f);
        // Engines off until RocketVisual lights them (also keeps edit-mode views clean).
        exhaust1.enabled = false;
        exhaust2.enabled = false;
        exhaust3.enabled = false;

        var lightGo = new GameObject("Engine Light");
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, -3f, 0f);
        var engineLight = lightGo.AddComponent<Light>();
        engineLight.type = LightType.Point;
        engineLight.color = new Color(1f, 0.62f, 0.3f);
        engineLight.range = 160f;
        engineLight.intensity = 0f;
        engineLight.shadows = LightShadows.None;
        // Only a marker for position, colour and range; the surface shaders read them as globals.
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
        SetReferenceArray(visual, "gridFins", fins);
        SetReferenceArray(visual, "legs", legs);
        SetReferenceArray(visual, "engineBells", bells.ToArray());
        SetReference(visual, "vacuumBell", vacuum.GetComponent<Renderer>());
        return visual;
    }

    // ---------- lighting shared by the game scene and the showcase ----------

    // Golden-hour HDRI as a prefiltered cubemap for reflections and ambient light.
    public static Cubemap GetEnvironmentCubemap()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(HdriPath);
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        bool dirty = settings.textureShape != TextureImporterShape.TextureCube
            || settings.generateCubemap != TextureImporterGenerateCubemap.AutoCubemap
            || settings.cubemapConvolution != TextureImporterCubemapConvolution.Specular
            || importer.maxTextureSize != 256;
        if (dirty)
        {
            settings.textureShape = TextureImporterShape.TextureCube;
            settings.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap;
            settings.cubemapConvolution = TextureImporterCubemapConvolution.Specular;
            settings.seamlessCubemap = true;
            settings.mipmapEnabled = true;
            importer.SetTextureSettings(settings);
            importer.maxTextureSize = 256;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Cubemap>(HdriPath);
    }

    // Soft sun shadows that reach the whole 72 m stack from the launch cameras.
    public static void ConfigureShadows(Light sun)
    {
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.92f;
        sun.shadowBias = 0.05f;
        sun.shadowNormalBias = 0.4f;
        foreach (string path in new[] { "Assets/Settings/Mobile_RPAsset.asset", "Assets/Settings/PC_RPAsset.asset" })
        {
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("m_MainLightShadowsSupported").boolValue = true;
            serialized.FindProperty("m_MainLightShadowmapResolution").intValue = 2048;
            serialized.FindProperty("m_ShadowDistance").floatValue = 260f;
            serialized.FindProperty("m_ShadowCascadeCount").intValue = 1;
            serialized.FindProperty("m_SoftShadowsSupported").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    public static EnvironmentLighting CreateEnvironmentLighting(float rotationDegrees, float intensity, bool ownsSkyDay)
    {
        var go = new GameObject("Environment Lighting");
        var env = go.AddComponent<EnvironmentLighting>();
        var serialized = new SerializedObject(env);
        serialized.FindProperty("environment").objectReferenceValue = GetEnvironmentCubemap();
        serialized.FindProperty("rotationDegrees").floatValue = rotationDegrees;
        serialized.FindProperty("intensity").floatValue = intensity;
        serialized.FindProperty("ownsSkyDay").boolValue = ownsSkyDay;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return env;
    }

    // ---------- liveries ----------

    private static readonly Color Ink = new Color(0.04f, 0.04f, 0.05f);
    private static readonly Color Paper = new Color(0.95f, 0.95f, 0.94f);

    private static Texture2D LiveryStage1()
    {
        float width = 2f * Mathf.PI * BodyRadius;
        float c = width * 0.5f;
        return Livery("Livery_Stage1", width, 37.4f, 44f, new List<Element>
        {
            new TextElement { Text = "ROCKETQUIZ", Font = RocketQuizUiAssets.DisplayBlack, Center = new Vector2(c, 18.6f), Height = 1.85f, Vertical = true, Spacing = 4f, Color = Ink },
            new RectElement { Area = Rect.MinMaxRect(c + 1.42f, 0.6f, c + 1.56f, 36.6f), Color = RqTheme.Ignition },
            new TextElement { Text = "RQ-B1001", Font = RocketQuizUiAssets.Mono, Center = new Vector2(c, 2.2f), Height = 0.24f, Spacing = 12f, Color = Ink },
            new TextElement { Text = "DİKKAT · YÜKSEK BASINÇ · LOX", Font = RocketQuizUiAssets.Mono, Center = new Vector2(c + 2.9f, 1.1f), Height = 0.09f, Spacing = 8f, Color = new Color(0.2f, 0.2f, 0.22f) },
        });
    }

    private static Texture2D LiveryInterstage()
    {
        float width = 2f * Mathf.PI * 1.86f;
        float c = width * 0.5f;
        return Livery("Livery_Interstage", width, 6.5f, 72f, new List<Element>
        {
            new RingElement { Center = new Vector2(c, 3.25f), Outer = 1.0f, Inner = 0.86f, Color = RqTheme.Ignition },
            new RingElement { Center = new Vector2(c, 3.25f), Outer = 0.86f, Inner = 0f, Color = new Color(0.035f, 0.06f, 0.13f) },
            new RingElement { Center = new Vector2(c, 3.25f), Outer = 0.8f, Inner = 0.77f, Color = new Color(1f, 1f, 1f, 0.55f) },
            new TextElement { Text = "RQ", Font = RocketQuizUiAssets.DisplayBlack, Center = new Vector2(c, 3.33f), Height = 0.5f, Spacing = 2f, Color = Paper },
            new TextElement { Text = "GÖREV 001", Font = RocketQuizUiAssets.Mono, Center = new Vector2(c, 2.78f), Height = 0.1f, Spacing = 22f, Color = RqTheme.Ignition },
            new TextElement { Text = "ROCKETQUIZ", Font = RocketQuizUiAssets.DisplayBlack, Center = new Vector2(c, 1.3f), Height = 0.3f, Spacing = 10f, Color = Paper },
        });
    }

    private static Texture2D LiveryStage2()
    {
        float width = 2f * Mathf.PI * BodyRadius;
        float c = width * 0.5f;
        return Livery("Livery_Stage2", width, 11.6f, 44f, new List<Element>
        {
            new RectElement { Area = Rect.MinMaxRect(0f, 10.85f, width, 11.05f), Color = RqTheme.Ignition },
            new TextElement { Text = "ÜST KADEME", Font = RocketQuizUiAssets.Mono, Center = new Vector2(c + 1.25f, 5.6f), Height = 0.2f, Vertical = true, Spacing = 16f, Color = new Color(0.35f, 0.36f, 0.38f) },
        });
    }

    private static Texture2D LiveryFairing()
    {
        float width = 2f * Mathf.PI * FairingRadius;
        float c = width * 0.5f;
        return Livery("Livery_Fairing", width, 6.05f, 62f, new List<Element>
        {
            new TextElement { Text = "ROCKETQUIZ", Font = RocketQuizUiAssets.DisplayBlack, Center = new Vector2(c, 3.7f), Height = 0.68f, Spacing = 6f, Color = Ink },
            new RectElement { Area = Rect.MinMaxRect(c - 3.4f, 2.92f, c + 3.4f, 2.98f), Color = RqTheme.Ignition },
            new TextElement { Text = "GÖREV 001  ·  ANKARA", Font = RocketQuizUiAssets.Mono, Center = new Vector2(c, 2.5f), Height = 0.2f, Spacing = 18f, Color = new Color(0.25f, 0.26f, 0.28f) },
        });
    }

    // ---------- geometry ----------

    private static Vector3 Radial(float degrees)
    {
        float a = degrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
    }

    // Cable tunnel running up the tank with clamp brackets, offset from the livery.
    private static Mesh BuildRaceway(string name, float bottom, float top)
    {
        var parts = new List<CombineInstance>();
        Vector3 radial = Radial(FrontDegrees - 38f);
        Quaternion facing = Quaternion.LookRotation(radial, Vector3.up);
        float length = top - bottom;
        AddBox(parts, radial * (BodyRadius + 0.07f) + Vector3.up * (bottom + length * 0.5f), facing, new Vector3(0.32f, length, 0.15f));
        for (float y = bottom + 0.5f; y < top - 0.3f; y += 1.45f)
        {
            AddBox(parts, radial * (BodyRadius + 0.06f) + Vector3.up * y, facing, new Vector3(0.46f, 0.07f, 0.19f));
        }
        Vector3 thin = Radial(FrontDegrees + 155f);
        AddBox(parts, thin * (BodyRadius + 0.04f) + Vector3.up * (bottom + length * 0.5f), Quaternion.LookRotation(thin, Vector3.up), new Vector3(0.12f, length, 0.08f));
        return Combine(name, parts);
    }

    // Folded landing leg in pivot space: hinge at the origin, +Y along the body, +Z outwards.
    private static Mesh BuildLeg(string name)
    {
        const float length = 10.2f;
        Vector3[] corners =
        {
            new Vector3(-0.31f, 0f, 0f), new Vector3(0.31f, 0f, 0f), new Vector3(0.24f, 0f, 0.34f), new Vector3(-0.24f, 0f, 0.34f),
            new Vector3(-0.17f, length, 0f), new Vector3(0.17f, length, 0f), new Vector3(0.12f, length, 0.2f), new Vector3(-0.12f, length, 0.2f),
        };
        var parts = new List<CombineInstance>();
        parts.Add(new CombineInstance { mesh = Hexahedron(corners), transform = Matrix4x4.identity });
        // Hinge knuckle and the telescoping pusher's top fitting.
        AddBox(parts, new Vector3(0f, 0.1f, 0.1f), Quaternion.identity, new Vector3(0.8f, 0.3f, 0.24f));
        AddBox(parts, new Vector3(0f, 7.4f, 0.27f), Quaternion.identity, new Vector3(0.22f, 0.4f, 0.1f));
        return Combine(name, parts);
    }

    // Titanium grid fin in pivot space: hinge along X at the origin, stowed flat (+Y up the body).
    private static Mesh BuildGridFin(string name)
    {
        const float width = 1.32f;
        const float height = 1.52f;
        const float depth = 0.15f;
        const float wall = 0.035f;
        const float web = 0.018f;
        var parts = new List<CombineInstance>();
        Vector3 center = new Vector3(0f, height * 0.5f + 0.05f, 0.1f);
        AddBox(parts, center + new Vector3(0f, height * 0.5f, 0f), Quaternion.identity, new Vector3(width, wall, depth));
        AddBox(parts, center - new Vector3(0f, height * 0.5f, 0f), Quaternion.identity, new Vector3(width, wall * 1.6f, depth));
        AddBox(parts, center + new Vector3(width * 0.5f, 0f, 0f), Quaternion.identity, new Vector3(wall, height, depth));
        AddBox(parts, center - new Vector3(width * 0.5f, 0f, 0f), Quaternion.identity, new Vector3(wall, height, depth));

        // Diagonal lattice, clipped to the frame by keeping only interior segments.
        const int lines = 9;
        float pitch = (width + height) / lines;
        for (int sign = -1; sign <= 1; sign += 2)
        {
            for (int i = 1; i < lines; i++)
            {
                // Line x*sign + y = c in frame-local coordinates centred on the fin.
                float c = -0.5f * (width + height) + i * pitch;
                var points = new List<Vector2>();
                float hw = width * 0.5f;
                float hh = height * 0.5f;
                TryAdd(points, new Vector2(-hw, c + sign * hw), hw, hh);
                TryAdd(points, new Vector2(hw, c - sign * hw), hw, hh);
                TryAdd(points, new Vector2(sign * (c + hh), -hh), hw, hh);
                TryAdd(points, new Vector2(sign * (c - hh), hh), hw, hh);
                if (points.Count < 2) continue;
                Vector2 a = points[0];
                Vector2 b = points[1];
                for (int k = 2; k < points.Count; k++)
                {
                    if ((points[k] - a).sqrMagnitude > (b - a).sqrMagnitude) b = points[k];
                }
                Vector2 mid = (a + b) * 0.5f;
                float len = (b - a).magnitude;
                float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
                AddBox(parts, center + new Vector3(mid.x, mid.y, 0f), Quaternion.Euler(0f, 0f, angle), new Vector3(len, web, depth * 0.92f));
            }
        }
        // Root shaft into the actuator.
        AddBox(parts, new Vector3(0f, 0.02f, 0.06f), Quaternion.identity, new Vector3(0.3f, 0.12f, 0.12f));
        return Combine(name, parts);
    }

    private static void TryAdd(List<Vector2> points, Vector2 p, float hw, float hh)
    {
        if (Mathf.Abs(p.x) <= hw + 1e-4f && Mathf.Abs(p.y) <= hh + 1e-4f)
        {
            points.Add(p);
        }
    }

    private static Mesh BoxMesh(string name, Vector3 center, Vector3 size)
    {
        var parts = new List<CombineInstance>();
        AddBox(parts, center, Quaternion.identity, size);
        return Combine(name, parts);
    }

    private static void AddBox(List<CombineInstance> parts, Vector3 position, Quaternion rotation, Vector3 size)
    {
        if (unitCube == null)
        {
            var corners = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                corners[i] = new Vector3((i & 1) == 0 ? -0.5f : 0.5f, i < 4 ? -0.5f : 0.5f, (i & 2) == 0 ? -0.5f : 0.5f);
            }
            // Reorder into the Hexahedron convention: bottom ring then top ring, counter-clockwise.
            unitCube = Hexahedron(new[]
            {
                corners[0], corners[1], corners[3], corners[2], corners[4], corners[5], corners[7], corners[6],
            });
        }
        parts.Add(new CombineInstance { mesh = unitCube, transform = Matrix4x4.TRS(position, rotation, size) });
    }

    // Flat-shaded solid from 8 corners: 0-3 bottom ring, 4-7 top ring (same winding).
    private static Mesh Hexahedron(Vector3[] c)
    {
        int[][] faces =
        {
            new[] { 0, 1, 2, 3 }, new[] { 7, 6, 5, 4 },
            new[] { 0, 4, 5, 1 }, new[] { 1, 5, 6, 2 }, new[] { 2, 6, 7, 3 }, new[] { 3, 7, 4, 0 },
        };
        Vector3 centroid = Vector3.zero;
        foreach (Vector3 p in c) centroid += p / 8f;

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();
        foreach (int[] f in faces)
        {
            Vector3 n = Vector3.Cross(c[f[1]] - c[f[0]], c[f[2]] - c[f[0]]).normalized;
            Vector3 faceCenter = (c[f[0]] + c[f[1]] + c[f[2]] + c[f[3]]) * 0.25f;
            bool flip = Vector3.Dot(n, faceCenter - centroid) < 0f;
            if (flip) n = -n;
            int start = vertices.Count;
            for (int k = 0; k < 4; k++)
            {
                vertices.Add(c[f[k]]);
                normals.Add(n);
            }
            if (flip)
            {
                triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
            }
            else
            {
                triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
        }
        var mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh Combine(string name, List<CombineInstance> parts)
    {
        var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.CombineMeshes(parts.ToArray(), true, true);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Material HullMaterial(string name, Texture2D grime, Texture2D detail, int mode, Color color, float metallic, float roughness)
    {
        Material material = GetMaterial(name, "RocketQuiz/Hull");
        material.SetFloat("_Mode", mode);
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Roughness", roughness);
        material.SetTexture("_GrimeTex", grime);
        material.SetTexture("_DetailNormal", detail);
        material.SetFloat("_Cull", 2f);
        material.SetFloat("_PanelCount", 0f);
        material.SetVector("_FrostRange", Vector4.zero);
        material.SetVector("_SootRange", Vector4.zero);
        material.SetTexture("_LiveryTex", null);
        material.SetVector("_LiveryRect", new Vector4(0f, 0f, 0f, 0f));
        return material;
    }

    private static void SetLivery(Material material, Texture2D livery, float bottom, float top, float uOffset)
    {
        material.SetTexture("_LiveryTex", livery);
        // The livery's centre column (0.5) lands on the front angle.
        material.SetVector("_LiveryRect", new Vector4(bottom, top, uOffset, 0f));
    }

    private static GameObject CreateRocketPart(string name, Transform parent, Mesh mesh, Material material, Vector3 localPosition)
    {
        GameObject go = CreatePart(name, parent, mesh, material, localPosition);
        go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
        return go;
    }

    private static void DeleteLegacyRocketAssets()
    {
        string[] legacy =
        {
            "RocketWhite.mat", "RocketDark.mat", "RocketMetal.mat", "ProbeGold.mat", "ProbePanel.mat", "RocketFairing.mat",
            "Stage1Body.asset", "Stage1Skirt.asset", "Stage1Band.asset", "EngineBell.asset", "Stage2Body.asset",
            "Stage2Interstage.asset", "FairingLeft.asset", "FairingRight.asset", "ProbeDish.asset",
        };
        foreach (string file in legacy)
        {
            string path = GeneratedFolder + "/" + file;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
