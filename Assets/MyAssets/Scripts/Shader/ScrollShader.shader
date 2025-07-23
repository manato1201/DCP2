Shader "Custom/Scroll"
{
      Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Speed("Speed", Range(0, 1)) = 0.5
        _Dir("Direction(X, Y)", Vector) = (1, 1, 0, 0)
        [MaterialToggle] _ScrollEnabled("Enable Scroll", Float) = 1
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _Speed;
            float2 _Dir;
            float _ScrollEnabled;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 dir = normalize(_Dir);
                float2 offset = _ScrollEnabled * dir * _Time.y * (_Speed * 1024) / _ScreenParams.xy; // スピードを上げる
                
                float2 uv = frac(i.uv + offset); // ループ処理
                float4 texColor = tex2Dlod(_MainTex, float4(uv, 0, 0)); // LODを指定してサンプリング
                
                return texColor;
            }
            ENDCG
        }
    }
}
