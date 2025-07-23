Shader "Transition/Transition_Spread"
{
     Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		[MaterialToggle] _Inverse ("Inverse", Float) = 0
		_Div ("Division Size", Int) = 16
		_Value ("Value", Range(0, 1)) = 0
        _Color("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderType"="Transparent" }
 
		Blend SrcAlpha OneMinusSrcAlpha
 
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
            float4 _MainTex_ST;
			int _Div;
			float _Inverse;
			float _Speed;
			float _Value;
            float4 _Color;
 
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
 
 			//circle
			float circle(float2 p){
				return dot(p, p);
			}
            //四角形
            float rectangle(float2 p, float2 size) {
                return max(abs(p.x) - size.x, abs(p.y) - size.y);
            }

            //ひし形
            float rhombus(float2 p, float2 size) {
                return abs(p.x) + abs(p.y) - size;
            }

            //ハート
            float heart(float2 p, float size) {
                p.x = 1.2 * p.x - sign(p.x) * p.y * 0.55;
                return length(p) - size;
            }

            //円環
            float ring(float2 p, float size, float w) {
                 return abs(length(p) - size) + w;
            }

             //回転
            float2 rotation(float2 p, float theta) {
                 return float2((p.x) * cos(theta) - p.y * sin(theta), p.x * sin(theta) + p.y * cos(theta));
            }

            

 
			fixed4 frag (v2f i) : SV_Target
            {
				float inv = _Inverse;
				float div = (float)_Div;
 
				float2 st = i.uv * div + frac(div * 0.5 + 0.5);
				float asp =  _ScreenParams.y / _ScreenParams.x;
				st.y *= asp;
				st.y += (1.0 - frac(asp)) * (0.5 + floor(div * 0.5));
				float2 i_st = floor(st) - floor(div * 0.5);
				float2 f_st = frac(st) * 2.0 - 1.0;
 
				float2 sm;
				sm.x = floor(div * 0.5);
				sm.y = floor(div * asp * 0.5 + 0.5);
				float val = _Value * (length(sm) + 2.0);
 
				float a = 1;
				for(int i = -1; i <= 1; i++){
					for(int j = -1; j <= 1; j++){
						float v = val - length(i_st + float2(i, j));
 
						float ci = circle(f_st - float2(2.0 * i, 2.0 * j));
						a = min(a, step(v, ci));
						//a = min(a, smoothstep(v - 0.01, v, ci));
					}
				}
 
				fixed4 col =  _Color;
				col.a = inv - a * (inv * 2.0 - 1.0);
                return col;
            }
            ENDCG
        }
    }
}