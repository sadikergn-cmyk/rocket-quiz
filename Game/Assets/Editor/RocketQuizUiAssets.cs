using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// Generates the UI design-system assets: TMP font assets from the OFL fonts in Assets/Fonts,
// shadowed material presets for text over the 3D scene, and the rounded panel material.
public static class RocketQuizUiAssets
{
    private const string Dir = "Assets/Generated/RocketQuiz/UI";

    // ASCII + Turkish + the punctuation the UI uses. Static fonts only contain these glyphs.
    private const string Characters =
        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
        "ÇĞİÖŞÜçğıöşüÂÎÛâîû“”‘’…·–—→←↑↓•°×±";

    public static TMP_FontAsset Display { get; private set; }
    public static TMP_FontAsset DisplayBlack { get; private set; }
    public static TMP_FontAsset Body { get; private set; }
    public static TMP_FontAsset BodyBold { get; private set; }
    public static TMP_FontAsset Mono { get; private set; }
    public static Material DisplayShadow { get; private set; }
    public static Material DisplayBlackShadow { get; private set; }
    public static Material MonoShadow { get; private set; }
    public static Material BodyShadow { get; private set; }
    public static Material Panel { get; private set; }

    public static void Ensure()
    {
        Directory.CreateDirectory(Dir);
        Display = GetFont("Unbounded-Bold", false);
        DisplayBlack = GetFont("Unbounded-Black", false);
        Body = GetFont("SpaceGrotesk-Medium", true);
        BodyBold = GetFont("SpaceGrotesk-Bold", false);
        Mono = GetFont("JetBrainsMono-Bold", false);
        DisplayShadow = GetShadow(Display);
        DisplayBlackShadow = GetShadow(DisplayBlack);
        MonoShadow = GetShadow(Mono);
        BodyShadow = GetShadow(Body);
        Panel = GetPanelMaterial();
        AssetDatabase.SaveAssets();
    }

    private static TMP_FontAsset GetFont(string name, bool dynamic)
    {
        string path = Dir + "/" + name + " SDF.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (existing != null)
        {
            return existing;
        }

        var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/" + name + ".ttf");
        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024,
            AtlasPopulationMode.Dynamic, false);
        asset.name = name + " SDF";
        string missing;
        asset.TryAddCharacters(Characters, out missing);
        if (!string.IsNullOrEmpty(missing))
        {
            Debug.LogWarning(name + " is missing glyphs: " + missing);
        }

        // Static fonts ship only their atlas; the dynamic body font also ships its TTF so the
        // native answer field can render anything the player types.
        if (!dynamic)
        {
            asset.atlasPopulationMode = AtlasPopulationMode.Static;
        }

        AssetDatabase.CreateAsset(asset, path);
        asset.material.name = name + " Atlas Material";
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        foreach (Texture2D atlas in asset.atlasTextures)
        {
            atlas.name = name + " Atlas";
            AssetDatabase.AddObjectToAsset(atlas, asset);
        }
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        return asset;
    }

    // Soft drop shadow so thin type stays readable over bright sky, smoke and planets.
    private static Material GetShadow(TMP_FontAsset font)
    {
        string path = Dir + "/" + font.name + " Shadow.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(font.material);
            AssetDatabase.CreateAsset(material, path);
        }
        material.CopyPropertiesFromMaterial(font.material);
        material.EnableKeyword("UNDERLAY_ON");
        material.SetColor("_UnderlayColor", new Color(0f, 0.02f, 0.06f, 0.75f));
        material.SetFloat("_UnderlayOffsetX", 0f);
        material.SetFloat("_UnderlayOffsetY", -0.35f);
        material.SetFloat("_UnderlayDilate", 0.25f);
        material.SetFloat("_UnderlaySoftness", 0.55f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material GetPanelMaterial()
    {
        string path = Dir + "/UIPanel.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("RocketQuiz/UI Panel"));
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }
}
