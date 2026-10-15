using UnityEngine;
using UnityEngine.UI;

// Resolution-independent rounded panel drawn with a signed-distance shader (RocketQuiz/UI Panel):
// vertical gradient fill, inner border and an optional outer glow, all in a single quad.
// Shape data travels in UV channels so every panel batches with the same material.
[RequireComponent(typeof(CanvasRenderer))]
public class PanelGraphic : MaskableGraphic
{
    [SerializeField] private Color bottomColor = Color.white;
    [SerializeField] private float radius = 24f;
    [SerializeField] private float borderWidth = 2f;
    [SerializeField] private Color borderColor = new Color(1f, 1f, 1f, 0.2f);
    [SerializeField] private float glowSize;
    [SerializeField] private Color glowColor = new Color(1f, 1f, 1f, 0f);

    public void SetStyle(Color top, Color bottom, float cornerRadius, float border, Color borderTint, float glow, Color glowTint)
    {
        color = top;
        bottomColor = bottom;
        radius = cornerRadius;
        borderWidth = border;
        borderColor = borderTint;
        glowSize = glow;
        glowColor = glowTint;
        SetVerticesDirty();
    }

    public void SetFill(Color top, Color bottom)
    {
        color = top;
        bottomColor = bottom;
        SetVerticesDirty();
    }

    public void SetBorder(Color tint)
    {
        borderColor = tint;
        SetVerticesDirty();
    }

    public void SetGlow(Color tint)
    {
        glowColor = tint;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        Vector2 half = rect.size * 0.5f;
        Vector2 center = rect.center;
        float pad = glowSize + 2f;
        float cornerRadius = Mathf.Min(radius, Mathf.Min(half.x, half.y));

        bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
        Color border = linear ? borderColor.linear : borderColor;
        Color glow = linear ? glowColor.linear : glowColor;

        for (int i = 0; i < 4; i++)
        {
            float sx = i < 2 ? -1f : 1f;
            float sy = i == 0 || i == 3 ? -1f : 1f;
            var local = new Vector2(sx * (half.x + pad), sy * (half.y + pad));
            float t = Mathf.Clamp01(local.y / (2f * half.y) + 0.5f);
            Color fill = Color.Lerp(bottomColor, color, t);

            // Vertex alpha carries only the inherited CanvasGroup alpha; fill alpha rides in uv1.w.
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = center + local;
            vertex.color = new Color(fill.r, fill.g, fill.b, 1f);
            vertex.uv0 = new Vector4(local.x, local.y, half.x, half.y);
            vertex.uv1 = new Vector4(cornerRadius, borderWidth, glowSize, fill.a);
            vertex.uv2 = border;
            vertex.uv3 = glow;
            vh.AddVert(vertex);
        }

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }
}
