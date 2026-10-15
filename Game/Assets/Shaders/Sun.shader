Shader "RocketQuiz/Sun"
{
    // Self-lit photosphere: surface texture, HDR intensity for bloom, and limb darkening
    // (the disc is dimmer and redder towards its edge, which is what makes it read as a sphere).
    Properties
    {
        _MainTex ("Surface", 2D) = "white" {}
        _Color ("Color", Color) = (1, 0.82, 0.55, 1)
        _Intensity ("Intensity", Float) = 6
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
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Intensity;
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
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                float3 v = normalize(GetCameraPositionWS() - input.positionWS);
                half mu = saturate(dot(n, v));
                half3 surface = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb;
                half limb = pow(mu, 0.45);
                half3 tint = lerp(half3(1.0, 0.45, 0.2), half3(1.0, 1.0, 1.0), limb);
                half3 color = surface * _Color.rgb * tint * (0.35 + 0.65 * limb) * _Intensity;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
