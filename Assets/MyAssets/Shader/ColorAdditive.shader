Shader "UI/ColorAdditiveTex"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        _MulColor("Multiply Color", Color) = (1,1,1,1)
        _UseGray("Use Grayscale (0/1)", Float) = 0
        _OverallAlpha("Overall Alpha", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True"}
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            Name "UI"
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                half2  uv       : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MulColor;
            float  _UseGray;
            float  _OverallAlpha;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.worldPos = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);

                // グレースケール
                if (_UseGray > 0.5)
                {
                    float g = dot(c.rgb, float3(0.299, 0.587, 0.114));
                    c.rgb = g.xxx;
                }

                // 乗算色
                c.rgb *= _MulColor.rgb;
                c.a   *= _MulColor.a;

                // UGUI 用クリップ
                //c.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);

                // 全体アルファ
                c.a *= saturate(_OverallAlpha);

                return c;
            }
            ENDHLSL
        }
    }
}
