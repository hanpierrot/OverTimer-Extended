Shader "Overtime/UI/CardFlip"
{
    Properties
    {
        [PerRendererData] _MainTex ("Front Texture", 2D) = "white" {}
        _BackTex ("Back Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FrontRect ("Front Rect", Vector) = (0,0,1,1)
        _BackRect ("Back Rect", Vector) = (0,0,1,1)
        _Shade ("Shade", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            sampler2D _BackTex;
            fixed4 _Color;
            float4 _FrontRect;
            float4 _BackRect;
            float _Shade;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i, fixed facing : VFACE) : SV_Target
            {
                fixed4 c;
                if (facing > 0)
                {
                    c = tex2D(_MainTex, i.uv);
                }
                else
                {
                    float2 local = (i.uv - _FrontRect.xy) / _FrontRect.zw;
                    local.x = 1.0 - local.x;
                    c = tex2D(_BackTex, _BackRect.xy + local * _BackRect.zw);
                }

                c *= i.color;
                c.rgb *= _Shade;
                return c;
            }
            ENDCG
        }
    }
}