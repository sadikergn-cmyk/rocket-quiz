Shader "RocketQuiz/Galaxy"
{
    // Procedural spiral galaxy on a quad (additive).
    Properties
    {
        _CoreColor ("Core Color", Color) = (1.0, 0.86, 0.62, 1)
        _ArmColor ("Arm Color", Color) = (0.35, 0.55, 1.0, 1)
        _DustColor ("Star-forming Color", Color) = (1.0, 0.35, 0.55, 1)
        _Intensity ("Intensity", Float) = 1.1
        _Arms ("Arm Count", Float) = 2
        _Twist ("Twist", Float) = 5.5
        _Fade ("Fade", Range(0, 1)) = 1
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
            Blend One One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _ArmColor;
                half4 _DustColor;
                half _Intensity;
                half _Arms;
                half _Twist;
                half _Fade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
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
                    p = p * 2.1 + 5.3;
                    amp *= 0.5;
                }
                return sum;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;
                float r = length(p);
                float angle = atan2(p.y, p.x) + _Time.y * 0.01;

                float spiral = angle * _Arms + log(max(r, 0.001)) * _Twist;
                float arms = pow(0.5 + 0.5 * cos(spiral), 3.4);
                float clumps = Fbm(float2(spiral * 0.6, r * 9.0) + 3.0);
                float dust = Fbm(p * 7.0 + clumps);

                float disc = exp(-r * 4.2) * smoothstep(1.0, 0.5, r);
                float core = exp(-r * 22.0) * 2.2 + exp(-r * 8.0) * 0.35;

                float armLight = arms * disc * (0.2 + 1.9 * clumps * clumps * 2.0);
                float lanes = smoothstep(0.35, 0.75, dust) * arms;

                half3 color = _CoreColor.rgb * core;
                color += _ArmColor.rgb * armLight * 2.2;
                color += _DustColor.rgb * pow(clumps, 3.0) * arms * disc * 3.0;
                color *= 1.0 - 0.55 * lanes * smoothstep(0.05, 0.3, r);

                float grain = pow(Hash(floor(input.uv * 1500.0)), 48.0);
                color += half3(0.9, 0.95, 1.0) * grain * (arms * 0.8 + 0.2) * disc * 14.0;

                return half4(color * _Intensity * _Fade, 1.0);
            }
            ENDHLSL
        }
    }
}
