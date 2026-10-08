Shader "RocketQuiz/CloudLayer"
{
    // Procedural cloud deck on a large horizontal plane; fades towards its rim.
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Scale ("Noise Scale", Float) = 0.0006
        _Coverage ("Coverage", Range(0, 1)) = 0.5
        _Opacity ("Opacity", Range(0, 1)) = 0.9
        _Offset ("Offset", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _Offset;
                float _Scale;
                half _Coverage;
                half _Opacity;
            CBUFFER_END

            float3 _SpaceSunDir;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                return output;
            }

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x),
                            lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            float Fbm(float2 p)
            {
                float sum = 0.0;
                float amp = 0.5;
                for (int i = 0; i < 5; i++)
                {
                    sum += Noise(p) * amp;
                    p = p * 2.07 + 13.7;
                    amp *= 0.5;
                }
                return sum;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 p = input.positionWS.xz * _Scale + _Offset.xy + _Time.y * 0.004;
                float shape = Fbm(p);
                float detail = Fbm(p * 3.1 + shape);
                float density = smoothstep(1.0 - _Coverage, 1.0 - _Coverage + 0.28, shape * 0.75 + detail * 0.35);

                float2 centered = input.uv * 2.0 - 1.0;
                float rim = 1.0 - smoothstep(0.55, 1.0, length(centered));

                half sun = saturate(_SpaceSunDir.y * 0.7 + 0.45);
                half3 lit = lerp(half3(0.55, 0.62, 0.74), half3(1.0, 0.98, 0.95), detail) * sun * 1.15;
                return half4(lit * _Color.rgb, density * rim * _Opacity);
            }
            ENDHLSL
        }
    }
}
