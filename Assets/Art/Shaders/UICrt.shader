Shader "Overtimer/UI/CRT"
{
    // Screen look for the BACKGROUND image of the laptop and phone windows (DESIGN 5.18): faint scanlines, a slow
    // brightness band that rolls down the screen and a tiny flicker. Everything is computed from the screen position,
    // so it does not depend on the sprite's UVs (the window sprites are 9-sliced). Children (icons, buttons, text)
    // are separate Graphics and are not touched.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        _LineSize ("Scanline Height (px)", Range(1, 8)) = 3
        _LineStrength ("Scanline Strength", Range(0, 0.5)) = 0.12
        _RollStrength ("Roll Band Strength", Range(0, 0.2)) = 0.04
        _Flicker ("Flicker", Range(0, 0.1)) = 0.02
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _LineSize;
            float _LineStrength;
            float _RollStrength;
            float _Flicker;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                float row = floor(IN.vertex.y / max(_LineSize, 1.0));
                float scan = fmod(row, 2.0);                                         // every other row is darker
                float y = IN.vertex.y / _ScreenParams.y;
                float roll = sin((y - _Time.y * 0.07) * 6.2831853);                  // slow band, one cycle per screen
                float flick = frac(sin(floor(_Time.y * 24.0) * 12.9898) * 43758.5453) - 0.5;   // new value 24x a second

                color.rgb *= 1.0 - _LineStrength * scan + _RollStrength * roll + _Flicker * flick;
                return color;
            }
            ENDCG
        }
    }
}
