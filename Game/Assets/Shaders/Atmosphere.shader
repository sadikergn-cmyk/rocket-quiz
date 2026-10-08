Shader "RocketQuiz/Atmosphere"
{
    // Analytic atmosphere shell: integrates an approximate optical depth along the view ray
    // instead of raymarching, so it stays cheap on mobile and works from inside the shell.
    Properties
    {
        _DayColor ("Day Color", Color) = (0.30, 0.56, 1.0, 1)
        _SunsetColor ("Sunset Color", Color) = (1.0, 0.45, 0.18, 1)
        _PlanetRadius ("Planet Radius", Float) = 1000
        _AtmoRadius ("Atmosphere Radius", Float) = 1012
        _Density ("Density", Float) = 0.02
        _Falloff ("Falloff", Float) = 4
        _Intensity ("Intensity", Float) = 1.6
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+1" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Front
            ZWrite Off
            ZTest Always
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _DayColor;
                half4 _SunsetColor;
                float _PlanetRadius;
                float _AtmoRadius;
                float _Density;
                float _Falloff;
                float _Intensity;
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

            // Returns (near, far) distances along the ray, or far < 0 when the ray misses.
            float2 RaySphere(float3 origin, float3 dir, float radius)
            {
                float b = dot(origin, dir);
                float c = dot(origin, origin) - radius * radius;
                float d = b * b - c;
                if (d < 0.0)
                {
                    return float2(0.0, -1.0);
                }
                float s = sqrt(d);
                return float2(-b - s, -b + s);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 center = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                float3 camera = GetCameraPositionWS();
                float3 origin = camera - center;
                float3 dir = normalize(input.positionWS - camera);
                float3 l = normalize(_SpaceSunDir);

                float2 atmo = RaySphere(origin, dir, _AtmoRadius);
                if (atmo.y < 0.0)
                {
                    return half4(0.0, 0.0, 0.0, 0.0);
                }
                float start = max(atmo.x, 0.0);
                float end = atmo.y;

                float2 planet = RaySphere(origin, dir, _PlanetRadius);
                bool hitsPlanet = planet.y > 0.0 && planet.x > 0.0;
                if (hitsPlanet)
                {
                    end = planet.x;
                }
                float pathLength = max(end - start, 0.0);

                float3 mid = origin + dir * (start + pathLength * 0.5);
                float shell = max(_AtmoRadius - _PlanetRadius, 0.001);
                float altitude = saturate((length(mid) - _PlanetRadius) / shell);
                float density = exp(-altitude * _Falloff) * _Density;
                float optical = pathLength * density;
                float alpha = 1.0 - exp(-optical);

                float3 up = normalize(mid);
                float sun = dot(up, l);
                float lit = smoothstep(-0.25, 0.2, sun);
                float warm = exp(-sun * sun / 0.02);
                float forward = pow(saturate(dot(dir, l)), 6.0);

                half3 color = lerp(_DayColor.rgb, _SunsetColor.rgb, saturate(warm * (0.35 + forward)));
                color *= lit * _Intensity * (1.0 + forward * 2.0);

                // Over the disc the planet shader already adds haze, so keep the shell subtle there.
                float coverage = hitsPlanet ? 0.55 : 1.0;
                alpha *= coverage;
                return half4(color * alpha, alpha * 0.85 * lit);
            }
            ENDHLSL
        }
    }
}
