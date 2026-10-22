Shader "OverTimer/SpriteOutlineGlow"
{
    Properties { _MainTex ("Sprite", 2D) = "white" {} }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            float4 _SpriteRect;
            float2 _Pad;
            float2 _Flip;
            float2 _Radius; 
            float4 _GlowColor;
            float  _Amount;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 spriteUV : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                float2 s = v.uv * (1.0 + 2.0 * _Pad) - _Pad;
                o.spriteUV = lerp(s, 1.0 - s, _Flip);
                return o;
            }

            float SpriteAlpha(float2 s)
            {
                if (any(s < 0.0) || any(s > 1.0)) return 0.0;
                float2 t = lerp(_SpriteRect.xy, _SpriteRect.zw, s);
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, t).a;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float centre = SpriteAlpha(i.spriteUV);
                float edge = 0.0;

                [unroll] for (int k = 0; k < 16; k++)
                {
                    float a = k * 0.3926991;
                    float2 d = float2(cos(a), sin(a));
                    edge = max(edge, SpriteAlpha(i.spriteUV + d * _Radius));
                    edge = max(edge, SpriteAlpha(i.spriteUV + d * _Radius * 0.5));
                }

                float outline = saturate(edge - centre);
                return half4(_GlowColor.rgb, outline * _Amount * _GlowColor.a);
            }
            ENDHLSL
        }
    }
}