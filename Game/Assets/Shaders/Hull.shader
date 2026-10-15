Shader "RocketQuiz/Hull"
{
    // Physically based surface for the rocket and launch hardware.
    // - GGX specular + Lambert, main light with URP shadows, engine glow (globals from RocketVisual)
    // - Image based light from an HDRI cubemap (_EnvCube, global), faded into the trilight
    //   ambient as the sky turns to space (_SkyDay)
    // - Procedural surface detail in object space so geometry needs no hand-made UVs:
    //   Mode 0 hull: cylindrical mapping, ring welds, panel seams, livery band, frost, soot, grime
    //   Mode 1 plain: triplanar fine detail (titanium, steel, paint)
    //   Mode 2 carbon: twill weave composite
    //   Mode 3 nozzle: heat-tinted engine bell with optional incandescent glow
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Roughness ("Roughness", Range(0.03, 1)) = 0.5
        [Enum(Hull,0,Plain,1,Carbon,2,Nozzle,3)] _Mode ("Mode", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2

        [Header(Hull)]
        _SeamSpacing ("Ring Weld Spacing (m)", Float) = 3.2
        _SeamOffset ("Ring Weld Offset (m)", Float) = 0
        _PanelCount ("Longitudinal Seams", Float) = 0
        _PanelPhase ("Seam Phase (turns)", Float) = 0
        _SeamDepth ("Seam Depth", Range(0, 2)) = 0.6
        _LiveryTex ("Livery (RGBA)", 2D) = "black" {}
        _LiveryRect ("Livery Height Min/Max, U Offset", Vector) = (0, 1, 0, 0)

        [Header(Weathering)]
        _GrimeTex ("Grime (R streak, G frost, B soot, A fine)", 2D) = "gray" {}
        _DetailNormal ("Detail Normal", 2D) = "bump" {}
        _DetailScale ("Detail Scale (m)", Float) = 2
        _DetailStrength ("Detail Strength", Range(0, 2)) = 0.35
        _FrostRange ("Frost Height Min/Max, Amount", Vector) = (0, 0, 0, 0)
        _SootRange ("Soot Height Min/Max, Amount", Vector) = (0, 0, 0, 0)
        _Grime ("Streak Grime", Range(0, 1)) = 0.25

        [Header(Nozzle)]
        _HeatColor ("Heat Tint", Color) = (0.35, 0.22, 0.45, 1)
        _NozzleHeight ("Nozzle Height (m)", Float) = 2
        _Glow ("Glow", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _Metallic;
            half _Roughness;
            half _Mode;
            half _Cull;
            float _SeamSpacing;
            float _SeamOffset;
            float _PanelCount;
            float _PanelPhase;
            half _SeamDepth;
            float4 _LiveryRect;
            float _DetailScale;
            half _DetailStrength;
            float4 _FrostRange;
            float4 _SootRange;
            half _Grime;
            half4 _HeatColor;
            float _NozzleHeight;
            half _Glow;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_LiveryTex); SAMPLER(sampler_LiveryTex);
            TEXTURE2D(_GrimeTex); SAMPLER(sampler_GrimeTex);
            TEXTURE2D(_DetailNormal); SAMPLER(sampler_DetailNormal);

            // Globals.
            TEXTURECUBE(_EnvCube); SAMPLER(sampler_EnvCube);
            float _EnvRotation;
            half _EnvIntensity;
            half _EnvMips;
            half _SkyDay;
            half _RocketFrost;
            float4 _EngineLightPos;
            float4 _EngineLightColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                float3 normalOS : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionOS = input.positionOS.xyz;
                output.normalOS = input.normalOS;
                return output;
            }

            float3 RotateEnv(float3 d)
            {
                float s = sin(_EnvRotation);
                float c = cos(_EnvRotation);
                return float3(c * d.x - s * d.z, d.y, s * d.x + c * d.z);
            }

            half3 Trilight(float3 d)
            {
                half3 upper = lerp(unity_AmbientEquator.rgb, unity_AmbientSky.rgb, saturate(d.y));
                return lerp(upper, unity_AmbientGround.rgb, saturate(-d.y));
            }

            half3 EnvDiffuse(float3 n)
            {
                half3 hdri = SAMPLE_TEXTURECUBE_LOD(_EnvCube, sampler_EnvCube, RotateEnv(n), _EnvMips - 1.0).rgb * _EnvIntensity;
                return lerp(Trilight(n), hdri, _SkyDay);
            }

            half3 EnvSpecular(float3 r, half roughness)
            {
                half mip = roughness * (1.7 - 0.7 * roughness) * (_EnvMips - 2.0);
                half3 hdri = SAMPLE_TEXTURECUBE_LOD(_EnvCube, sampler_EnvCube, RotateEnv(r), mip).rgb * _EnvIntensity;
                return lerp(Trilight(r) * 0.6, hdri, _SkyDay);
            }

            // Karis' analytic approximation of the split-sum environment BRDF.
            half3 EnvBRDF(half3 specColor, half roughness, half ndv)
            {
                const half4 c0 = half4(-1, -0.0275, -0.572, 0.022);
                const half4 c1 = half4(1, 0.0425, 1.04, -0.04);
                half4 r = roughness * c0 + c1;
                half a004 = min(r.x * r.x, exp2(-9.28 * ndv)) * r.x + r.y;
                half2 ab = half2(-1.04, 1.04) * a004 + r.zw;
                return specColor * ab.x + ab.y;
            }

            // Distance (m) to the nearest line of a periodic pattern with the given period.
            float LineDistance(float x, float period)
            {
                float f = frac(x / period);
                return min(f, 1.0 - f) * period;
            }

            // Groove profile and its slope: used both for darkening and for bending the normal.
            float2 Groove(float distance, float width)
            {
                float t = distance / width;
                float g = exp(-t * t);
                return float2(g, -2.0 * t * g / width);
            }

            float Triplanar(float3 p, float3 n, float scale)
            {
                float3 w = pow(abs(n), 4.0);
                w /= (w.x + w.y + w.z);
                float a = SAMPLE_TEXTURE2D(_GrimeTex, sampler_GrimeTex, p.zy / scale).a;
                float b = SAMPLE_TEXTURE2D(_GrimeTex, sampler_GrimeTex, p.xz / scale).a;
                float c = SAMPLE_TEXTURE2D(_GrimeTex, sampler_GrimeTex, p.xy / scale).a;
                return a * w.x + b * w.y + c * w.z;
            }

            half4 frag(Varyings input, bool frontFace : SV_IsFrontFace) : SV_Target
            {
                float3 n = normalize(input.normalWS) * (frontFace ? 1.0 : -1.0);
                float3 v = normalize(GetCameraPositionWS() - input.positionWS);
                float3 p = input.positionOS;

                half3 albedo = _BaseColor.rgb;
                half metallic = _Metallic;
                half roughness = _Roughness;
                half occlusion = 1.0;
                half3 emission = 0;

                int mode = (int)round(_Mode);
                float radius = max(length(p.xz), 1e-3);
                float angle = atan2(p.z, p.x);
                float u = angle / TWO_PI + 0.5;
                float arc = angle * radius;

                // Tangent frame of a surface of revolution, in world space.
                float3 upWS = normalize(TransformObjectToWorldDir(float3(0, 1, 0)));
                float3 aroundWS = normalize(TransformObjectToWorldDir(float3(-p.z, 0, p.x) / radius));

                if (mode == 0)
                {
                    // Ring welds and longitudinal panel seams.
                    float2 ring = Groove(LineDistance(p.y - _SeamOffset, _SeamSpacing), 0.018);
                    float2 seam = float2(0, 0);
                    if (_PanelCount > 0.5)
                    {
                        seam = Groove(LineDistance(u - _PanelPhase, 1.0 / _PanelCount) * TWO_PI * radius, 0.012);
                    }
                    n = normalize(n - (upWS * ring.y + aroundWS * seam.y) * _SeamDepth * 0.004);
                    occlusion *= 1.0 - 0.35 * saturate(ring.x + seam.x);

                    // Fine paint/metal texture and long vertical grime streaks.
                    float2 detailUV = float2(arc, p.y) / _DetailScale;
                    half3 detail = UnpackNormal(SAMPLE_TEXTURE2D(_DetailNormal, sampler_DetailNormal, detailUV));
                    n = normalize(n + (aroundWS * detail.x + upWS * detail.y) * _DetailStrength * 0.25);
                    half4 grime = SAMPLE_TEXTURE2D(_GrimeTex, sampler_GrimeTex, float2(arc / 9.0, p.y / 36.0));
                    half streak = saturate((grime.r - 0.45) * 2.5) * _Grime;
                    albedo *= 1.0 - 0.22 * streak;
                    roughness = saturate(roughness + 0.12 * streak);

                    // Soot from engine exhaust and re-use, climbing up from the base.
                    half sootMask = saturate((_SootRange.y - p.y) / max(_SootRange.y - _SootRange.x, 1e-3));
                    half sootStreak = SAMPLE_TEXTURE2D(_GrimeTex, sampler_GrimeTex, float2(arc / 4.0, p.y / 22.0)).r;
                    half soot = saturate(sootMask * 2.4 - (1.0 - grime.b) * 0.9 - (1.0 - sootStreak) * 0.6) * _SootRange.z;
                    albedo = lerp(albedo, half3(0.03, 0.03, 0.03), soot * 0.85);
                    roughness = lerp(roughness, 0.9, soot);

                    // Frost over the cryogenic tank: thick at the bottom of the band, patchy at the top.
                    half frostBand = step(_FrostRange.x, p.y) * step(p.y, _FrostRange.y);
                    half frostEdge = saturate((_FrostRange.y - p.y) / 6.0);
                    half frostNoise = SAMPLE_TEXTURE2D(_GrimeTex, sampler_GrimeTex, float2(arc / 3.0, p.y / 3.0)).g;
                    half frost = saturate((frostNoise + frostEdge * 0.6 - 1.15 + _RocketFrost * _FrostRange.z) * 4.0) * frostBand;
                    albedo = lerp(albedo, half3(0.88, 0.92, 0.97), frost);
                    roughness = lerp(roughness, 0.82, frost);
                    metallic *= 1.0 - frost;
                    n = normalize(n + (aroundWS * (frostNoise - 0.5) + upWS * (grime.a - 0.5)) * frost * 0.5);
                }
                else if (mode == 1)
                {
                    float fine = Triplanar(p, input.normalOS, 0.6);
                    albedo *= 0.88 + 0.24 * fine;
                    roughness = saturate(roughness + (fine - 0.5) * 0.18);
                }
                else if (mode == 2)
                {
                    // 2x2 twill: diagonal bands of crossing tows.
                    float2 q = float2(arc, p.y) / 0.012;
                    float warp = step(0.5, frac((floor(q.x) + floor(q.y)) * 0.25));
                    float tow = sin(q.x * PI) * sin(q.y * PI);
                    albedo *= 0.9 + 0.14 * warp;
                    roughness = saturate(roughness - 0.1 * warp + 0.05 * abs(tow));
                    float2 ring = Groove(LineDistance(p.y - _SeamOffset, _SeamSpacing), 0.02);
                    n = normalize(n - upWS * ring.y * _SeamDepth * 0.004);
                    occlusion *= 1.0 - 0.3 * ring.x;
                }
                else
                {
                    // Engine bell: bare metal near the throat, heat-tinted bands lower down, soot inside.
                    float h = saturate(p.y / max(_NozzleHeight, 1e-3));
                    half3 throat = half3(0.55, 0.42, 0.32);
                    half3 tinted = lerp(_HeatColor.rgb, half3(0.62, 0.56, 0.46), smoothstep(0.55, 0.95, h));
                    albedo = lerp(albedo, lerp(tinted, throat, smoothstep(0.75, 1.0, h)), 0.35);
                    float rings = Groove(LineDistance(p.y, 0.09), 0.012).x;
                    occlusion *= 1.0 - 0.25 * rings;
                    if (!frontFace)
                    {
                        albedo *= 0.25;
                        roughness = 0.8;
                    }
                    half heat = _Glow * pow(saturate(1.0 - h), 0.7);
                    emission = half3(4.0, 1.25, 0.35) * heat * heat + half3(1.2, 0.18, 0.04) * heat;
                }

                // Livery band (RGB colour over the base, alpha = coverage) on hull and composite parts.
                if (mode == 0 || mode == 2)
                {
                    float liveryV = (p.y - _LiveryRect.x) / max(_LiveryRect.y - _LiveryRect.x, 1e-3);
                    if (liveryV > 0.0 && liveryV < 1.0)
                    {
                        half4 livery = SAMPLE_TEXTURE2D(_LiveryTex, sampler_LiveryTex, float2(frac(u - _LiveryRect.z), liveryV));
                        albedo = lerp(albedo, livery.rgb, livery.a);
                        roughness = lerp(roughness, 0.62, livery.a);
                        metallic *= 1.0 - livery.a;
                    }
                }

                // ---- lighting ----
                half3 diffuseColor = albedo * (1.0 - metallic);
                half3 specColor = lerp(half3(0.04, 0.04, 0.04), albedo, metallic);
                half a = roughness * roughness;
                half a2 = max(a * a, 1e-4);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light sun = GetMainLight(shadowCoord);
                float3 l = sun.direction;
                float3 h = normalize(l + v);
                half ndl = saturate(dot(n, l));
                half ndv = max(saturate(dot(n, v)), 1e-3);
                half ndh = saturate(dot(n, h));
                half vdh = saturate(dot(v, h));

                half d = ndh * ndh * (a2 - 1.0) + 1.0;
                half D = a2 / (PI * d * d);
                half visL = ndv * sqrt(ndl * ndl * (1.0 - a2) + a2);
                half visV = ndl * sqrt(ndv * ndv * (1.0 - a2) + a2);
                half V = 0.5 / max(visL + visV, 1e-4);
                half3 F = specColor + (1.0 - specColor) * pow(1.0 - vdh, 5.0);
                half3 radiance = sun.color * (sun.shadowAttenuation * sun.distanceAttenuation) * ndl;

                half3 color = (diffuseColor + D * V * F * PI) * radiance;

                // Image based light.
                float3 r = reflect(-v, n);
                half horizon = saturate(1.0 + dot(r, normalize(input.normalWS)));
                color += diffuseColor * EnvDiffuse(n) * occlusion;
                color += EnvSpecular(r, roughness) * EnvBRDF(specColor, roughness, ndv) * occlusion * horizon * horizon;

                // Engine glow: point light without the pipeline's light loop.
                float3 toEngine = _EngineLightPos.xyz - input.positionWS;
                float distanceSq = dot(toEngine, toEngine);
                float range = max(_EngineLightPos.w, 1.0);
                half falloff = saturate(1.0 - distanceSq / (range * range));
                half engine = saturate(dot(n, toEngine * rsqrt(max(distanceSq, 0.01)))) * falloff * falloff;
                color += diffuseColor * _EngineLightColor.rgb * engine;

                color += emission;
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            float4 ShadowVert(Attributes input) : SV_POSITION
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirection = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirection = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirection));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }

            half4 ShadowFrag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            float4 DepthVert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half DepthFrag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
