Shader "RocketQuiz/PlanetRing"
{
    Properties
    {
        _MainTex ("Ring Strip", 2D) = "white" {}
        _Tint ("Tint", Color) = (0.92, 0.84, 0.70, 1)
        _InnerRadius ("Inner Radius (0-1 of mesh)", Float) = 0.55
        _PlanetRadius ("Planet Radius", Float) = 1000
        _Exposure ("Exposure", Float) = 1
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

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _InnerRadius;
                float _PlanetRadius;
                half _Exposure;
            CBUFFER_END

            float3 _SpaceSunDir;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionOS = input.positionOS.xyz;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float radius = length(input.positionOS.xz);
                float t = (radius - _InnerRadius) / (1.0 - _InnerRadius);
                if (t < 0.0 || t > 1.0)
                {
                    discard;
                }
                half4 strip = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(t, 0.5));

                // Planet shadow across the ring: ray from the fragment towards the sun.
                float3 center = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                float3 origin = input.positionWS - center;
                float3 l = normalize(_SpaceSunDir);
                float b = dot(origin, l);
                float c = dot(origin, origin) - _PlanetRadius * _PlanetRadius;
                float d = b * b - c;
                half shadow = (d > 0.0 && b < 0.0) ? 0.08 : 1.0;

                half3 color = strip.rgb * _Tint.rgb * shadow * _Exposure;
                return half4(color, strip.a);
            }
            ENDHLSL
        }
    }
}
