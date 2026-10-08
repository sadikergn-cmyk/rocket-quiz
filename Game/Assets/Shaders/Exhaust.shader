Shader "RocketQuiz/Exhaust"
{
    // Additive plume on a cone mesh. uv.y runs 0 (nozzle) -> 1 (tail).
    Properties
    {
        _CoreColor ("Core Color", Color) = (1.0, 0.95, 0.8, 1)
        _EdgeColor ("Edge Color", Color) = (1.0, 0.45, 0.1, 1)
        _VacuumColor ("Vacuum Color", Color) = (0.55, 0.75, 1.0, 1)
        _Intensity ("Intensity", Float) = 6
        _Vacuum ("Vacuum (0-1)", Range(0, 1)) = 0
        _Throttle ("Throttle (0-1)", Range(0, 1)) = 1
        _Speed ("Scroll Speed", Float) = 9
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent+10" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite Off
            Blend One One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _EdgeColor;
                half4 _VacuumColor;
                half _Intensity;
                half _Vacuum;
                half _Throttle;
                half _Speed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
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

            half4 frag(Varyings input) : SV_Target
            {
                float along = input.uv.y;
                float3 n = normalize(input.normalWS);
                float3 v = normalize(GetCameraPositionWS() - input.positionWS);
                // Facing ratio fakes volume: bright through the middle, soft at the silhouette.
                half facing = pow(abs(dot(n, v)), 1.6);

                float scroll = _Time.y * _Speed;
                float n1 = Noise(float2(input.uv.x * 14.0, along * 5.0 - scroll));
                float n2 = Noise(float2(input.uv.x * 31.0, along * 12.0 - scroll * 1.7));
                half flicker = 0.65 + 0.5 * n1 + 0.25 * n2;

                half fade = pow(saturate(1.0 - along), lerp(1.6, 2.6, _Vacuum));
                half diamonds = pow(0.5 + 0.5 * cos(along * 46.0 - 1.0), 6.0) * exp(-along * 7.0) * (1.0 - _Vacuum);

                half3 atmoColor = lerp(_CoreColor.rgb, _EdgeColor.rgb, saturate(along * 2.2 + (1.0 - facing) * 0.6));
                half3 color = lerp(atmoColor, _VacuumColor.rgb, _Vacuum);
                color += _CoreColor.rgb * diamonds * 2.0;

                half strength = fade * facing * flicker * _Intensity * _Throttle * lerp(1.0, 0.45, _Vacuum);
                return half4(color * strength, 1.0);
            }
            ENDHLSL
        }
    }
}
