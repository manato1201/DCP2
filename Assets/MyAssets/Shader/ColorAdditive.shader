Shader "UI/ColorAdditiveTex"
{
    Properties{
        _MainTex      ("MainTex", 2D) = "white" {}
        _MulColor     ("Multiply Color", Color) = (1,1,1,1) // 乗算
        _AddColor     ("Additive Color", Color) = (0,0,0,0) // 加算 ← 追加
        _UseGrayscale ("Use Grayscale (0/1)", Float) = 0
        _OverallAlpha ("Overall Alpha", Range(0,1)) = 1
    }
    SubShader{
        Tags{ "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off ZTest Always

        Pass{
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex; float4 _MainTex_ST;
            float4 _MulColor;
            float4 _AddColor;             // ← 追加
            float  _UseGrayscale;
            float  _OverallAlpha;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f     { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 col:COLOR; };

            v2f vert(appdata v){
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.col = v.color; return o;
            }

            fixed4 frag(v2f i):SV_Target{
                fixed4 c = tex2D(_MainTex, i.uv);

                if (_UseGrayscale > 0.5){
                    float g = dot(c.rgb, float3(0.299, 0.587, 0.114));
                    c.rgb = g.xxx;
                }

                // 乗算 → 加算 → 全体アルファ
                c.rgb = c.rgb * _MulColor.rgb + _AddColor.rgb;
                c.a   = c.a * _MulColor.a;
                c.a  *= _OverallAlpha;
                c *= i.col; // UI の頂点カラーを活かす場合

                return c;
            }
            ENDHLSL
        }
    }
}
