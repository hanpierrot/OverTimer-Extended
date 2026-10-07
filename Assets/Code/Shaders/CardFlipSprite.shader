Shader "Overtime/Sprites/CardFlip"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _BackTex ("Back Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0

        [PerRendererData] _FrontRect ("Front Rect (min xy, size zw)", Vector) = (0,0,1,1)
        [PerRendererData] _BackRect ("Back Rect (min xy, size zw)", Vector) = (0,0,1,1)
        [PerRendererData] _Shade ("Shade", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment CardFrag
            #pragma target 3.0
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            sampler2D _BackTex;
            float4 _FrontRect;
            float4 _BackRect;
            float _Shade;

            fixed4 CardFrag(v2f IN, fixed facing : VFACE) : SV_Target
            {
                fixed4 c;
                if (facing > 0)
                {
                    c = SampleSpriteTexture(IN.texcoord) * IN.color;
                }
                else
                {
                    float2 local = (IN.texcoord - _FrontRect.xy) / _FrontRect.zw;
                    local.x = 1.0 - local.x; // mặt sau nhìn từ phía sau nên lật gương lại cho đúng chiều
                    c = tex2D(_BackTex, _BackRect.xy + local * _BackRect.zw) * IN.color;
                }

                c.rgb *= _Shade;
                c.rgb *= c.a;
                return c;
            }
        ENDCG
        }
    }
}
