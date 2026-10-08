Shader "RocketQuiz/LaunchGround"
{
    // Wide terrain plane around the launch site with distance haze towards the horizon.
    Properties
    {
        _ColorA ("Land A", Color) = (0.62, 0.55, 0.40, 1)
        _ColorB ("Land B", Color) = (0.36, 0.44, 0.26, 1)
        _HazeColor ("Haze Color", Color) = (0.72, 0.84, 1.0, 1)
        _HazeDistance ("Haze Distance", Float) = 6500
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorA;
                half4 _ColorB;
                half4 _HazeColor;
                float _HazeDistance;
            CBUFFER_END

            float3 _SpaceSunDir;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
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
                float2 p = input.positionWS.xz;
                float large = Noise(p * 0.0007) * 0.6 + Noise(p * 0.0031) * 0.4;
                float fine = Noise(p * 0.03) * 0.5 + Noise(p * 0.21) * 0.5;
                half3 land = lerp(_ColorA.rgb, _ColorB.rgb, smoothstep(0.3, 0.7, large));
                land *= 0.8 + 0.4 * fine;

                half sun = saturate(_SpaceSunDir.y) * 1.1 + 0.25;
                half3 color = land * sun;

                float dist = distance(GetCameraPositionWS(), input.positionWS);
                half haze = 1.0 - exp(-dist / _HazeDistance);
                color = lerp(color, _HazeColor.rgb, haze);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
