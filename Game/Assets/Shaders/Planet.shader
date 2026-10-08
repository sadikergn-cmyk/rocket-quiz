Shader "RocketQuiz/Planet"
{
    Properties
    {
        _MainTex ("Day", 2D) = "white" {}
        _NightTex ("Night Lights", 2D) = "black" {}
        _CloudTex ("Clouds", 2D) = "black" {}
        _NightStrength ("Night Strength", Float) = 0
        _CloudStrength ("Cloud Strength", Float) = 0
        _CloudSpeed ("Cloud Speed", Float) = 0.0015
        _OceanSpec ("Ocean Specular", Float) = 0
        _AtmoColor ("Atmosphere Color", Color) = (0.35, 0.6, 1, 1)
        _AtmoStrength ("Atmosphere Strength", Float) = 0
        _Exposure ("Exposure", Float) = 1
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

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_NightTex); SAMPLER(sampler_NightTex);
            TEXTURE2D(_CloudTex); SAMPLER(sampler_CloudTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _AtmoColor;
                half _NightStrength;
                half _CloudStrength;
                half _CloudSpeed;
                half _OceanSpec;
                half _AtmoStrength;
                half _Exposure;
            CBUFFER_END

            // Direction towards the sun, set globally by SpaceJourney.
            float3 _SpaceSunDir;

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

            half4 frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                float3 v = normalize(GetCameraPositionWS() - input.positionWS);
                float3 l = normalize(_SpaceSunDir);
                half ndl = dot(n, l);
                half day = smoothstep(-0.12, 0.3, ndl);
                half fresnel = pow(1.0 - saturate(dot(n, v)), 3.0);

                half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb;
                half ocean = saturate((albedo.b - max(albedo.r, albedo.g)) * 8.0);

                float2 cloudUv = input.uv + float2(_Time.y * _CloudSpeed, 0.0);
                half cloud = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, cloudUv).r * _CloudStrength;
                half cloudShadow = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, cloudUv + float2(0.004, 0.002)).r * _CloudStrength;

                half3 color = albedo * day * (1.0 - 0.45 * cloudShadow);

                float3 h = normalize(l + v);
                half spec = pow(saturate(dot(n, h)), 90.0) * ocean * _OceanSpec * (1.0 - cloud) * day;
                color += spec * half3(1.0, 0.92, 0.78);

                color = lerp(color, half3(1.0, 1.0, 1.0) * day, saturate(cloud));

                half3 night = SAMPLE_TEXTURE2D(_NightTex, sampler_NightTex, input.uv).rgb;
                color += night * night * (1.0 - day) * (1.0 - cloud) * _NightStrength;

                // Haze over the disc, warm along the terminator.
                half terminator = exp(-ndl * ndl / 0.03);
                half3 haze = _AtmoColor.rgb * (fresnel * 1.4 + 0.04) * day;
                haze += half3(1.0, 0.42, 0.15) * terminator * fresnel * 0.8;
                color = color * (1.0 - 0.4 * fresnel * _AtmoStrength) + haze * _AtmoStrength;

                return half4(color * _Exposure, 1.0);
            }
            ENDHLSL
        }
    }
}
