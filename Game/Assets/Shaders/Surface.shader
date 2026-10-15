Shader "RocketQuiz/Surface"
{
    // Lightweight lit surface for the rocket and launch site: sun + trilight ambient + a cheap
    // sky reflection + the engine glow. No shadow or probe samplers, so it behaves the same on
    // WebGL and mobile as in the Editor.
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Metallic;
                half _Smoothness;
                half _Cull;
            CBUFFER_END

            // Globals set by SpaceJourney and RocketVisual.
            float3 _SpaceSunDir;
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
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half3 Ambient(float3 direction)
            {
                half up = direction.y;
                half3 upper = lerp(unity_AmbientEquator.rgb, unity_AmbientSky.rgb, saturate(up));
                return lerp(upper, unity_AmbientGround.rgb, saturate(-up));
            }

            half4 frag(Varyings input, bool frontFace : SV_IsFrontFace) : SV_Target
            {
                float3 n = normalize(input.normalWS) * (frontFace ? 1.0 : -1.0);
                float3 v = normalize(GetCameraPositionWS() - input.positionWS);
                float3 l = normalize(_SpaceSunDir);
                const half3 sunColor = half3(2.3, 2.2, 2.05);

                half3 albedo = _BaseColor.rgb;
                half3 diffuseColor = albedo * (1.0 - _Metallic);
                half3 specColor = lerp(half3(0.04, 0.04, 0.04), albedo, _Metallic);

                half ndl = saturate(dot(n, l));
                half ndv = saturate(dot(n, v));
                float3 h = normalize(l + v);
                half power = exp2(10.0 * _Smoothness + 1.0);
                half spec = pow(saturate(dot(n, h)), power) * (power + 8.0) * 0.04;

                half3 color = diffuseColor * (ndl * sunColor + Ambient(n));
                color += specColor * spec * ndl * sunColor;

                half fresnel = pow(1.0 - ndv, 5.0);
                half3 reflectance = specColor + (max(half3(_Smoothness, _Smoothness, _Smoothness), specColor) - specColor) * fresnel;
                color += Ambient(reflect(-v, n)) * reflectance * 1.6;

                // Engine glow: a point light without the pipeline's light loop.
                float3 toEngine = _EngineLightPos.xyz - input.positionWS;
                float distanceSq = dot(toEngine, toEngine);
                float range = max(_EngineLightPos.w, 1.0);
                half falloff = saturate(1.0 - distanceSq / (range * range));
                half engine = saturate(dot(n, toEngine * rsqrt(max(distanceSq, 0.01)))) * falloff * falloff;
                color += diffuseColor * _EngineLightColor.rgb * engine;

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
