﻿
Shader "Transition/Transition_Crystal"
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
                return frac(sin(s) * 43758.5453123);
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
                int seed = _Seed;
                float2 st = float2(i.uv.x, i.uv.y * _ScreenParams.y / _ScreenParams.x) * _Size;
                float2 i_st = floor(st);
                float2 f_st = frac(st);

                float min_dist = 1;
                float2 min_p;
                for (int j = -1; j <= 1; j++) {
                    for (int k = -1; k <= 1; k++) {
                        float2 n = float2(j, k);
                        float2 p = rand(i_st + n, seed);
                        float dist = distance(f_st, p + n);
                        if (dist < min_dist) {
                            min_dist = dist;
                            min_p = p;
                        }
                    }
                }

                float t = dot(min_p, float2(0.3, 0.2));
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