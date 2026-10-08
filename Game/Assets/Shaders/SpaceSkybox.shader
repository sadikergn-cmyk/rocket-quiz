Shader "RocketQuiz/SpaceSkybox"
{
    Properties
    {
        _StarTex ("Star Map (equirect)", 2D) = "black" {}
        _StarIntensity ("Star Intensity", Float) = 2.6
        _ZenithColor ("Day Zenith", Color) = (0.10, 0.32, 0.78, 1)
        _HorizonColor ("Day Horizon", Color) = (0.72, 0.84, 1.0, 1)
        _NebulaColorA ("Nebula Color A", Color) = (0.9, 0.25, 0.6, 1)
        _NebulaColorB ("Nebula Color B", Color) = (0.15, 0.45, 1.0, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_StarTex); SAMPLER(sampler_StarTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _HorizonColor;
                half4 _NebulaColorA;
                half4 _NebulaColorB;
                half _StarIntensity;
            CBUFFER_END

            // Globals driven by SpaceJourney.
            float3 _SpaceSunDir;
            float _SkyDay;
            float _NebulaBlend;
            float _WarpStretch;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.dir = input.positionOS.xyz;
                return output;
            }

            float Hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float Noise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(lerp(Hash(i), Hash(i + float3(1, 0, 0)), f.x),
                         lerp(Hash(i + float3(0, 1, 0)), Hash(i + float3(1, 1, 0)), f.x), f.y),
                    lerp(lerp(Hash(i + float3(0, 0, 1)), Hash(i + float3(1, 0, 1)), f.x),
                         lerp(Hash(i + float3(0, 1, 1)), Hash(i + float3(1, 1, 1)), f.x), f.y), f.z);
            }

            float Fbm(float3 p)
            {
                float sum = 0.0;
                float amp = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    sum += Noise(p) * amp;
                    p = p * 2.03 + 7.1;
                    amp *= 0.5;
                }
                return sum;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.dir);
                float3 l = normalize(_SpaceSunDir);

                // Squash the lookup along the travel axis during warp so stars smear into streaks.
                float3 starDir = normalize(float3(dir.x, dir.y * (1.0 - 0.85 * _WarpStretch), dir.z));
                float2 uv = float2(atan2(starDir.z, starDir.x) * 0.15915494 + 0.5, asin(clamp(starDir.y, -1.0, 1.0)) * 0.31830989 + 0.5);
                half3 stars = SAMPLE_TEXTURE2D_LOD(_StarTex, sampler_StarTex, uv, 0).rgb;
                stars = pow(stars, 1.5) * _StarIntensity * (1.0 + 2.0 * _WarpStretch);

                half3 nebula = half3(0.0, 0.0, 0.0);
                if (_NebulaBlend > 0.001)
                {
                    float a = Fbm(dir * 2.2 + 3.0);
                    float b = Fbm(dir * 4.1 - 5.0 + a);
                    float cloud = smoothstep(0.35, 0.85, a * 0.7 + b * 0.6);
                    nebula = lerp(_NebulaColorB.rgb, _NebulaColorA.rgb, smoothstep(0.3, 0.7, b)) * cloud * 1.6;
                    nebula += half3(1.0, 0.85, 0.7) * pow(cloud, 6.0) * 1.5;
                    nebula *= _NebulaBlend;
                }

                half up = saturate(dir.y);
                half3 daySky = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(up, 0.45));

                float sunDot = dot(dir, l);
                half sunDisc = smoothstep(0.99965, 0.9999, sunDot) * 40.0;
                half sunGlow = pow(saturate(sunDot), 600.0) * 6.0 + pow(saturate(sunDot), 24.0) * 0.35 * _SkyDay;

                half3 color = lerp(stars + nebula, daySky, saturate(_SkyDay));
                color += (sunDisc + sunGlow) * half3(1.0, 0.93, 0.82);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
