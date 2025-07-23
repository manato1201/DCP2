Shader "ScreenPocket/3d/LiquidPixel"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [Header(Shape)]
        _HeightMax ("Height Max", Float) = 1.0
        _HeightMin ("Height Min", Float) = 0.0
        _TopColor ("Top Color", Color) = (0.5,0.75,1,1)
        _BottomColor ("Bottom Color", Color) = (0,0.25,1,1)
        [Header(Wave)]
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _WavePower ("Wave Power", Float) = 0.1
        _WaveLength ("Wave Length", Float) = 1.0
        [Header(Rim)]
        _RimColor ("Rim Light Color", Color) = (1,1,1,1)
        _RimPower("Rim Light Power", Float) = 3
        [Header(Surface)]
        _SurfaceColor ("Surface Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"}
        LOD 100
        
        //先に前面を描く事でZテストで描画が少し軽くなる
        Pass
        {          
            ZWrite On
            ZTest LEqual
            Blend Off
            Cull Back
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float3 viewDir : TEXCOORD2;
                float4 worldPos : TEXCOORD3;
                float3 normal : NORMAL;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _TopColor;
            half4 _BottomColor;
            half4 _RimColor;
            float _RimPower;
            half4 _SurfaceColor;
            float _HeightMax;
            float _HeightMin;
            float _WaveSpeed;
            float _WavePower;
            float _WaveLength;

            v2f vert (appdata v)
            {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex );
                o.vertex = mul(UNITY_MATRIX_VP,o.worldPos);

                o.normal = float4(UnityObjectToWorldNormal(v.normal),0);
                
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                //リムライト用
                o.viewDir = normalize(_WorldSpaceCameraPos - o.worldPos.xyz);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float height = _HeightMax + sin( (i.worldPos.x + i.worldPos.z) * _WaveLength + _Time.w * _WaveSpeed) * _WavePower;
                //くり抜く
                clip(height - i.worldPos.y);

                half4 col = tex2D(_MainTex, i.uv);

                //グラデーションで色付け
                float rate = saturate((i.worldPos.y - _HeightMin) / (_HeightMax - _HeightMin));
                col.rgb *= lerp( _BottomColor.rgb, _TopColor.rgb, rate);

                //リムライト
                float rim = saturate(1 - dot(i.normal, i.viewDir));
                col.rgb += saturate(pow(rim, _RimPower) * _RimColor);
                
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
        
        //背面を描く。単色で塗って切断面かのように見せかける
        Pass
        {          
            ZWrite On
            ZTest LEqual
            Blend Off
            Cull Front
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                UNITY_FOG_COORDS(0)
                float4 worldPos : TEXCOORD3;
                float4 vertex : SV_POSITION;
            };

            half4 _SurfaceColor;
            float _HeightMax;
            float _HeightMin;
            float _WaveSpeed;
            float _WavePower;
            float _WaveLength;

            v2f vert (appdata v)
            {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex );
                o.vertex = mul(UNITY_MATRIX_VP,o.worldPos);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float height = _HeightMax + sin( (i.worldPos.x + i.worldPos.z) * _WaveLength + _Time.w * _WaveSpeed) * _WavePower;
                //くり抜く
                clip(height - i.worldPos.y);

                half4 col = _SurfaceColor;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}