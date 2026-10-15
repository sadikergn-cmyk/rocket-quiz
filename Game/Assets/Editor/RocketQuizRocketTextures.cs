using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Procedural textures for the rocket, generated in the Editor and saved as PNGs:
// - Grime: tileable RGBA noise (R vertical streaks, G frost blotches, B soot, A fine grain)
// - Detail normal: tileable orange-peel paint / brushed metal normal map
// - Liveries: cylindrical decal bands painted from layers (text rendered with TextMesh Pro,
//   shapes rasterised on the CPU), RGB colour + alpha coverage.
public static class RocketQuizRocketTextures
{
    public const string Dir = "Assets/Generated/RocketQuiz/Rocket";

    // ---------- noise ----------

    public static Texture2D Grime()
    {
        return GetOrCreate("Grime", 512, 512, false, false, (w, h, pixels) =>
        {
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / w;
                    float v = (float)y / h;
                    // Streaks: strongly stretched along V.
                    float streak = Fbm(u * 48f, v * 3f, 48, 3, 3, 11);
                    float frost = Fbm(u * 10f, v * 10f, 10, 10, 5, 23);
                    float soot = Fbm(u * 6f, v * 6f, 6, 6, 5, 37);
                    float fine = Fbm(u * 64f, v * 64f, 64, 64, 2, 51);
                    pixels[y * w + x] = new Color(streak, frost, soot, fine);
                }
            }
        });
    }

    public static Texture2D DetailNormal()
    {
        return GetOrCreate("DetailNormal", 512, 512, true, false, (w, h, pixels) =>
        {
            var height = new float[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / w;
                    float v = (float)y / h;
                    height[y * w + x] = Fbm(u * 32f, v * 32f, 32, 32, 3, 71) * 0.7f + Fbm(u * 96f, v * 96f, 96, 96, 1, 83) * 0.3f;
                }
            }
            const float strength = 2.2f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = height[y * w + (x + 1) % w] - height[y * w + (x - 1 + w) % w];
                    float dy = height[((y + 1) % h) * w + x] - height[((y - 1 + h) % h) * w + x];
                    Vector3 n = new Vector3(-dx * strength, -dy * strength, 1f).normalized;
                    pixels[y * w + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
            }
        });
    }

    // Tileable value-noise fBm. periodX/Y are the lattice periods of the first octave.
    private static float Fbm(float x, float y, int periodX, int periodY, int octaves, int seed)
    {
        float sum = 0f;
        float amplitude = 0.5f;
        float norm = 0f;
        for (int i = 0; i < octaves; i++)
        {
            sum += amplitude * ValueNoise(x, y, periodX, periodY, seed + i * 131);
            norm += amplitude;
            x *= 2f;
            y *= 2f;
            periodX *= 2;
            periodY *= 2;
            amplitude *= 0.5f;
        }
        return sum / norm;
    }

    private static float ValueNoise(float x, float y, int periodX, int periodY, int seed)
    {
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        float fx = x - x0;
        float fy = y - y0;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        float a = Hash(x0, y0, periodX, periodY, seed);
        float b = Hash(x0 + 1, y0, periodX, periodY, seed);
        float c = Hash(x0, y0 + 1, periodX, periodY, seed);
        float d = Hash(x0 + 1, y0 + 1, periodX, periodY, seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }

    private static float Hash(int x, int y, int periodX, int periodY, int seed)
    {
        x = ((x % periodX) + periodX) % periodX;
        y = ((y % periodY) + periodY) % periodY;
        uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2246822519u);
        h = (h ^ (h >> 13)) * 1274126177u;
        h ^= h >> 16;
        return (h & 0xFFFFFF) / 16777215f;
    }

    // ---------- liveries ----------

    // Element painted into a livery band. Positions/sizes in metres on the unrolled cylinder:
    // x = arc length from u = 0, y = height from the bottom of the band.
    public abstract class Element
    {
        public Color Color;
    }

    public class TextElement : Element
    {
        public string Text;
        public TMP_FontAsset Font;
        public Vector2 Center;
        public float Height;
        public bool Vertical;
        public float Spacing;
    }

    public class RectElement : Element
    {
        public Rect Area;
    }

    public class RingElement : Element
    {
        public Vector2 Center;
        public float Outer;
        public float Inner;
    }

    // Paints a band of the given size (metres) at pixelsPerMetre and saves it as an RGBA PNG.
    public static Texture2D Livery(string name, float widthM, float heightM, float pixelsPerMetre, List<Element> elements)
    {
        int w = Mathf.NextPowerOfTwo(Mathf.CeilToInt(widthM * pixelsPerMetre));
        int h = Mathf.NextPowerOfTwo(Mathf.CeilToInt(heightM * pixelsPerMetre));
        float sx = w / widthM;
        float sy = h / heightM;
        var color = new Color[w * h];

        foreach (Element element in elements)
        {
            float[] mask = new float[w * h];
            var text = element as TextElement;
            if (text != null)
            {
                RenderTextMask(text, w, h, sx, sy, mask);
            }
            var rect = element as RectElement;
            if (rect != null)
            {
                RasterRect(rect, w, h, sx, sy, mask);
            }
            var ring = element as RingElement;
            if (ring != null)
            {
                RasterRing(ring, w, h, sx, sy, mask);
            }

            for (int i = 0; i < mask.Length; i++)
            {
                float a = mask[i] * element.Color.a;
                if (a <= 0f) continue;
                Color c = color[i];
                Color src = element.Color;
                float outA = a + c.a * (1f - a);
                Color mixed = (src * a + c * c.a * (1f - a)) / Mathf.Max(outA, 1e-5f);
                mixed.a = outA;
                color[i] = mixed;
            }
        }

        return Save(name, w, h, color, false, true);
    }

    private static void RasterRect(RectElement rect, int w, int h, float sx, float sy, float[] mask)
    {
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float px = (x + 0.5f) / sx;
                float py = (y + 0.5f) / sy;
                float dx = Mathf.Min(px - rect.Area.xMin, rect.Area.xMax - px) * sx;
                float dy = Mathf.Min(py - rect.Area.yMin, rect.Area.yMax - py) * sy;
                mask[y * w + x] = Mathf.Clamp01(Mathf.Min(dx, dy) + 0.5f);
            }
        }
    }

    private static void RasterRing(RingElement ring, int w, int h, float sx, float sy, float[] mask)
    {
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float px = (x + 0.5f) / sx - ring.Center.x;
                float py = (y + 0.5f) / sy - ring.Center.y;
                float r = Mathf.Sqrt(px * px + py * py) * sx;
                float outer = ring.Outer * sx - r;
                float inner = r - ring.Inner * sx;
                mask[y * w + x] = Mathf.Clamp01(Mathf.Min(outer, inner) + 0.5f);
            }
        }
    }

    // Renders white text on black with an orthographic camera and reads the result as a mask.
    private static void RenderTextMask(TextElement element, int w, int h, float sx, float sy, float[] mask)
    {
        var root = new GameObject("Livery Text Render");
        root.hideFlags = HideFlags.HideAndDontSave;
        root.transform.position = new Vector3(0f, -50000f, 0f);
        try
        {
            var camGo = new GameObject("Camera");
            camGo.hideFlags = HideFlags.HideAndDontSave;
            camGo.transform.SetParent(root.transform, false);
            camGo.transform.localPosition = new Vector3(w * 0.5f, h * 0.5f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = h * 0.5f;
            cam.aspect = (float)w / h;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.cullingMask = 1 << 31;
            cam.allowHDR = false;
            cam.allowMSAA = false;
            UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;

            var textGo = new GameObject("Text");
            textGo.hideFlags = HideFlags.HideAndDontSave;
            textGo.layer = 31;
            textGo.transform.SetParent(root.transform, false);
            var tmp = textGo.AddComponent<TextMeshPro>();
            tmp.font = element.Font;
            tmp.fontSize = 100f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.characterSpacing = element.Spacing;
            tmp.text = element.Text;
            tmp.rectTransform.sizeDelta = new Vector2(100000f, 1000f);
            tmp.ForceMeshUpdate();

            // Scale so the text's cap-to-baseline height matches the requested height in pixels.
            Bounds bounds = tmp.textBounds;
            float wantedPx = element.Height * (element.Vertical ? sx : sy);
            float scale = wantedPx / Mathf.Max(bounds.size.y, 1e-3f);
            textGo.transform.localScale = new Vector3(scale, scale, 1f);
            textGo.transform.localRotation = element.Vertical ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.identity;
            Vector3 offset = textGo.transform.localRotation * (bounds.center * scale);
            textGo.transform.localPosition = new Vector3(element.Center.x * sx, element.Center.y * sy, 0f) - offset;

            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            rt.antiAliasing = 1;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var read = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            read.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            read.Apply();
            RenderTexture.active = previous;
            cam.targetTexture = null;

            Color[] pixels = read.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                mask[i] = Mathf.Max(mask[i], pixels[i].r);
            }
            UnityEngine.Object.DestroyImmediate(read);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    // ---------- saving ----------

    private static Texture2D GetOrCreate(string name, int w, int h, bool normalMap, bool clampV, Action<int, int, Color[]> fill)
    {
        string path = Dir + "/" + name + ".png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null)
        {
            return existing;
        }
        var pixels = new Color[w * h];
        fill(w, h, pixels);
        return Save(name, w, h, pixels, normalMap, clampV);
    }

    private static Texture2D Save(string name, int w, int h, Color[] pixels, bool normalMap, bool livery)
    {
        Directory.CreateDirectory(Dir);
        string path = Dir + "/" + name + ".png";
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false, !livery);
        texture.SetPixels(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = livery;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = livery;
        importer.mipmapEnabled = true;
        importer.anisoLevel = 8;
        importer.wrapModeU = TextureWrapMode.Repeat;
        importer.wrapModeV = livery ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
