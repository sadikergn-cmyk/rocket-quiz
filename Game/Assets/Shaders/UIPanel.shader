// Rounded panel for PanelGraphic: SDF rounded box with gradient fill, inner border and outer glow.
// Output is premultiplied alpha. Works with masks, RectMask2D clipping and CanvasGroup fades.
Shader "RocketQuiz/UI Panel"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 uv2 : TEXCOORD2;
                float4 uv3 : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float4 shape : TEXCOORD0; // local xy, half size
                float4 style : TEXCOORD1; // radius, border, glow, fill alpha
                float4 border : TEXCOORD2;
                float4 glow : TEXCOORD3;
                float4 world : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _ClipRect;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.world = v.vertex;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.shape = v.uv0;
                o.style = v.uv1;
                o.border = v.uv2;
                o.glow = v.uv3;
                return o;
            }

            float RoundedBox(float2 p, float2 halfSize, float radius)
            {
                float2 q = abs(p) - (halfSize - radius);
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float d = RoundedBox(i.shape.xy, i.shape.zw, i.style.x);
                float aa = max(fwidth(d), 1e-4);
                float inside = saturate(0.5 - d / aa);

                // Fill (premultiplied).
                float fillAlpha = i.style.w;
                float4 c = float4(i.color.rgb * fillAlpha, fillAlpha) * inside;

                // Inner border band of width style.y measured from the edge.
                float borderWidth = i.style.y;
                if (borderWidth > 0.0)
                {
                    float band = inside * saturate(0.5 + (d + borderWidth) / aa);
                    float a = i.border.a * band;
                    c = float4(i.border.rgb * a, a) + c * (1.0 - a);
                }

                // Soft outer glow.
                float glowSize = i.style.z;
                if (glowSize > 0.0)
                {
                    float falloff = exp(-max(d, 0.0) / (glowSize * 0.35));
                    float g = i.glow.a * falloff * (1.0 - inside);
                    c += float4(i.glow.rgb * g, g);
                }

                c *= i.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                c *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a - 0.001);
                #endif

                return c;
            }
            ENDCG
        }
    }
}
