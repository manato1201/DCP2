
Shader "Transition/Transition_ParlinNoise"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        [MaterialToggle] _UseTexture("Use Texture", Float) = 0
        [MaterialToggle] _Inverse("Inverse", Float) = 0
        _Color("Color", Color) = (1, 1, 1, 1)
        _Size("Size", Float) = 0
        _Seed("Seed", Int) = 0
        _Value("Value", Range(0, 1)) = 0
        _Smoothing("Smoothing", Range(0.00001, 0.5)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" }

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _Color;
            float _Size;
            float _Seed;
            float _Value;
            float _Smoothing;
            float _Inverse;
            float _UseTexture;

            float2 rand(float2 st, int seed)
            {
                float2 s = float2(dot(st, float2(127.1, 311.7)) + seed, dot(st, float2(269.5, 183.3)) + seed);
                return -1 + 2 * frac(sin(s) * 43758.5453123);
            }

            float noise(float2 st, int seed)
            {
                float2 p = floor(st);
                float2 f = frac(st);

                float w00 = dot(rand(p, seed), f);
                float w10 = dot(rand(p + float2(1, 0), seed), f - float2(1, 0));
                float w01 = dot(rand(p + float2(0, 1), seed), f - float2(0, 1));
                float w11 = dot(rand(p + float2(1, 1), seed), f - float2(1, 1));

                float2 u = f * f * (3 - 2 * f);

                return lerp(lerp(w00, w10, u.x), lerp(w01, w11, u.x), u.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 st = float2(i.uv.x, i.uv.y * _ScreenParams.y / _ScreenParams.x) * _Size;
                float t = noise(st, _Seed) * 0.5 + 0.5;
                float w = max(_Smoothing, 1e-5);

                float a;
                if (_Smoothing <= 0)
                    a = step(_Value, t);
                else
                    a = saturate((t - (_Value - w)) / (2.0 * w));

                float inv = _Inverse;

                fixed4 texCol = tex2D(_MainTex, i.uv);
                fixed4 col = lerp(_Color, texCol, _UseTexture);
                col.a *= inv - a * (inv * 2.0 - 1.0);
                return col;
            }
            ENDCG
        }
    }
}