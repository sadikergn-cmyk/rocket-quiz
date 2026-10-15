using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// A look-development scene for the rocket: golden-hour HDRI sky, low sun, concrete ground and a
// set of fixed camera shots rendered to Assets/Screenshots (rocket_*.png). Not part of the build.
public static partial class RocketQuizSceneBuilder
{
    private const string ShowcasePath = "Assets/Scenes/RocketShowcase.unity";
    private const string ShowcaseFolder = "Assets/Generated/RocketQuiz/Showcase";
    private const string SkyHdriPath = "Assets/ThirdParty/PolyHaven/blouberg_sunrise_2_2k_sky.hdr";
    private const string ConcretePrefix = "Assets/ThirdParty/PolyHaven/concrete_floor_worn_001_";

    // Tunables for look development.
    public static float ShowcaseEnvRotation = 0f;
    public static float ShowcaseSunAzimuth = FrontDegrees - 48f;
    public static float ShowcaseSunElevation = 9f;

    private struct Shot
    {
        public string Name;
        public Vector3 Position;
        public Vector3 Target;
        public float Fov;
    }

    [MenuItem("Tools/Build Rocket Showcase")]
    public static string BuildShowcase()
    {
        EnsureFolder("Assets", "Generated");
        EnsureFolder("Assets/Generated", "RocketQuiz");
        EnsureFolder("Assets/Generated/RocketQuiz", "Showcase");
        spaceLayer = EnsureLayer(SpaceLayerName);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Sky: the same HDRI as a high resolution panorama behind the scene.
        var sky = GetMaterial("ShowcaseSky", "Skybox/Panoramic");
        sky.SetTexture("_MainTex", GetSkyPanorama());
        sky.SetFloat("_Mapping", 1f);
        sky.SetFloat("_ImageType", 0f);
        sky.SetFloat("_Exposure", 1f);
        sky.SetFloat("_Rotation", ShowcaseEnvRotation);
        RenderSettings.skybox = sky;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.78f, 0.84f, 1.0f);
        RenderSettings.ambientEquatorColor = new Color(0.95f, 0.8f, 0.66f);
        RenderSettings.ambientGroundColor = new Color(0.45f, 0.4f, 0.34f);
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = GetEnvironmentCubemap();
        RenderSettings.fog = false;

        var sunGo = new GameObject("Sun");
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.74f, 0.5f);
        sun.intensity = 3.4f;
        float az = ShowcaseSunAzimuth * Mathf.Deg2Rad;
        float el = ShowcaseSunElevation * Mathf.Deg2Rad;
        Vector3 toSun = new Vector3(Mathf.Cos(az) * Mathf.Cos(el), Mathf.Sin(el), Mathf.Sin(az) * Mathf.Cos(el));
        sunGo.transform.rotation = Quaternion.LookRotation(-toSun);
        ConfigureShadows(sun);

        CreateEnvironmentLighting(ShowcaseEnvRotation, 1f, true);
        Shader.SetGlobalColor("_EngineLightColor", Color.black);

        // Concrete pad over the HDRI's own beach; the rocket stands on its engines for the shots.
        GameObject pad = CreatePart("Concrete Pad", null, SaveMesh(RocketQuizMeshes.Lathe("ShowcasePad", new[]
        {
            new Vector2(36f, -0.6f), new Vector2(34f, -0.05f), new Vector2(0f, -0.05f),
        }, false, 96)), ShowcaseConcrete(), Vector3.zero);
        pad.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

        var padSmokeGo = new GameObject("Pad Smoke (unused)");
        var padSmoke = padSmokeGo.AddComponent<ParticleSystem>();
        padSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        CreateRocket(padSmoke);

        // Post processing.
        string profilePath = ShowcaseFolder + "/ShowcasePost.asset";
        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath) != null) AssetDatabase.DeleteAsset(profilePath);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, profilePath);
        var tonemapping = AddOverride<Tonemapping>(profile);
        tonemapping.mode.Override(TonemappingMode.ACES);
        var bloom = AddOverride<Bloom>(profile);
        bloom.threshold.Override(1.1f);
        bloom.intensity.Override(0.45f);
        bloom.scatter.Override(0.7f);
        var vignette = AddOverride<Vignette>(profile);
        vignette.intensity.Override(0.22f);
        var color = AddOverride<ColorAdjustments>(profile);
        color.postExposure.Override(0.15f);
        color.contrast.Override(6f);
        EditorUtility.SetDirty(profile);
        var volume = new GameObject("Post Processing").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;

        var camGo = new GameObject("Showcase Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.allowHDR = true;
        cam.nearClipPlane = 0.2f;
        cam.farClipPlane = 20000f;
        UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        data.antialiasingQuality = AntialiasingQuality.High;
        Shot hero = Shots()[0];
        camGo.transform.position = hero.Position;
        camGo.transform.LookAt(hero.Target);
        cam.fieldOfView = hero.Fov;

        EditorSceneManager.SaveScene(scene, ShowcasePath);
        AssetDatabase.SaveAssets();
        return ShowcasePath + " built";
    }

    // Renders every shot from the open showcase scene into Assets/Screenshots.
    public static string CaptureShowcase(int width = 1080, int height = 1920)
    {
        var cam = GameObject.Find("Showcase Camera").GetComponent<Camera>();
        Directory.CreateDirectory("Assets/Screenshots");
        // Same globals EnvironmentLighting keeps refreshed in the editor loop.
        Shader.SetGlobalFloat("_SkyDay", 1f);
        Shader.SetGlobalFloat("_RocketFrost", 1f);
        Shader.SetGlobalColor("_EngineLightColor", Color.black);
        var saved = new List<string>();
        foreach (Shot shot in Shots())
        {
            cam.transform.position = shot.Position;
            cam.transform.LookAt(shot.Target);
            cam.fieldOfView = shot.Fov;

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            cam.targetTexture = null;

            string path = "Assets/Screenshots/rocket_" + shot.Name + ".png";
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            rt.Release();
            Object.DestroyImmediate(rt);
            saved.Add(path);
        }
        Shot hero = Shots()[0];
        cam.transform.position = hero.Position;
        cam.transform.LookAt(hero.Target);
        cam.fieldOfView = hero.Fov;
        return string.Join(", ", saved);
    }

    private static Shot[] Shots()
    {
        Vector3 front = Radial(FrontDegrees);
        Vector3 right = Vector3.Cross(Vector3.up, -front);
        return new[]
        {
            new Shot { Name = "hero", Position = front * 100f - right * 22f + Vector3.up * 6f, Target = new Vector3(0f, 35f, 0f), Fov = 44f },
            new Shot { Name = "low", Position = front * 30f + right * 7f + Vector3.up * 0.6f, Target = new Vector3(0f, 40f, 0f), Fov = 62f },
            new Shot { Name = "engines", Position = front * 8.5f + right * 3.5f + Vector3.up * 1.2f, Target = new Vector3(0f, 2.6f, 0f), Fov = 58f },
            new Shot { Name = "interstage", Position = front * 9f + right * 3f + Vector3.up * 44.5f, Target = new Vector3(0f, 45.5f, 0f), Fov = 46f },
            new Shot { Name = "fairing", Position = front * 17f - right * 4f + Vector3.up * 60f, Target = new Vector3(0f, 65f, 0f), Fov = 46f },
        };
    }

    private static Texture2D GetSkyPanorama()
    {
        if (!File.Exists(SkyHdriPath))
        {
            File.Copy(HdriPath, SkyHdriPath);
            AssetDatabase.ImportAsset(SkyHdriPath, ImportAssetOptions.ForceSynchronousImport);
        }
        var importer = (TextureImporter)AssetImporter.GetAtPath(SkyHdriPath);
        if (importer.maxTextureSize != 2048 || importer.mipmapEnabled)
        {
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.maxTextureSize = 2048;
            importer.mipmapEnabled = false;
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(SkyHdriPath);
    }

    private static Material ShowcaseConcrete()
    {
        var normalImporter = (TextureImporter)AssetImporter.GetAtPath(ConcretePrefix + "nor_gl_1k.png");
        if (normalImporter.textureType != TextureImporterType.NormalMap)
        {
            normalImporter.textureType = TextureImporterType.NormalMap;
            normalImporter.SaveAndReimport();
        }
        Material material = GetMaterial("ShowcaseConcrete", "Universal Render Pipeline/Lit");
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ConcretePrefix + "diff_1k.png"));
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ConcretePrefix + "nor_gl_1k.png"));
        material.EnableKeyword("_NORMALMAP");
        material.SetFloat("_Smoothness", 0.18f);
        material.SetColor("_BaseColor", new Color(0.85f, 0.83f, 0.8f));
        material.SetTextureScale("_BaseMap", new Vector2(1f, 12f));
        return material;
    }
}
