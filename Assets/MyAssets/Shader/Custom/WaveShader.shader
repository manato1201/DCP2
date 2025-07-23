Shader "Custom/WaveShader"
{
  Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [MaterialToggle] _UseTexture ("Use Texture", Float) = 1
        _Color ("Color", Color) = (1,1,1,1)
        _Speed ("Wave Speed", Float) = 1.0
        _Frequency ("Wave Frequency", Float) = 5.0
        _Value ("Wave Strength (0-1)", Range(0,1)) = 1.0
        _ThresholdMin ("Height Threshold Min", Float) = 0.0
        _ThresholdMax ("Height Threshold Max", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _UseTexture;
            float _Speed;
            float _Frequency;
            float _Value;
            float _ThresholdMin;
            float _ThresholdMax;

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

            v2f vert(appdata v)
            {
                v2f o;
                float4 pos = v.vertex;

                // Y方向の波：X位置に基づいてY座標を変形
                float wave = sin(_Time.y * _Speed + pos.x * _Frequency);
                float inRange = step(_ThresholdMin, pos.y) * step(pos.y, _ThresholdMax);
                pos.y += wave * _Value * inRange;

                o.vertex = UnityObjectToClipPos(pos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texCol = tex2D(_MainTex, i.uv);
                fixed4 col = lerp(_Color, texCol * _Color, _UseTexture);
                return col;
            }
            ENDCG
        }
    }
}