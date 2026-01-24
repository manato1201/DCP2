Shader "Effect/PortalDistortion_URP"
{
    Properties
    {
        // 参考元の _MainTex ：マスク（円画像）
        [NoScaleOffset]_MainTex ("Mask Texture", 2D) = "white" {}

        // 参考元の _ScreenTex ：URPでは移動先カメラのRT
        [NoScaleOffset]_PortalTex ("Portal Camera Texture", 2D) = "black" {}

        _Tint("Tint", Color) = (1,1,1,1)
        _TintStrengthen("Tint Strengthen", Float) = 1

        _Rotate("Rotate Speed", Float) = 1

        [NoScaleOffset]_DistortTex ("Distortion Texture (Normal-ish)", 2D) = "bump" {}
        _Distort("Distort Intensity", Range(0,100)) = 1
        _DistortRotate("Distortion Rotate Speed", Float) = 1

        [NoScaleOffset]_ColorDistortTex ("Color Distort Texture", 2D) = "white" {}
        _ScrollSpeed("Scroll Speed", Float) = 1
        _DistortColor("Color Map Distort Intensity", Range(0,200)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }

        // 参考元の Blend One OneMinusSrcAlpha を維持
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "PortalDistortionURP"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_PortalTex);
            SAMPLER(sampler_PortalTex);
            float4 _PortalTex_TexelSize;

            TEXTURE2D(_DistortTex);
            SAMPLER(sampler_DistortTex);

            TEXTURE2D(_ColorDistortTex);
            SAMPLER(sampler_ColorDistortTex);

            float4 _Tint;
            float _TintStrengthen;

            float _Rotate;
            float _Distort;
            float _DistortRotate;

            float _ScrollSpeed;
            float _DistortColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 uv          : TEXCOORD0; // xy: main uv, zw: distort uv
                float2 uv2         : TEXCOORD1; // scrolling uv
            };

            float2 RotateUV(float2 uv, float angleRad)
            {
                float2 pivot = float2(0.5, 0.5);
                float s, c;
                sincos(angleRad, s, c);
                float2x2 r = float2x2(c, -s, s, c);
                return mul(r, uv - pivot) + pivot;
            }

            // Normal-ish を RG/AG両対応で読む（URPのImport事故対策）
            float2 DecodeNormalXY_Flexible(float4 packed)
            {
                float2 ag = packed.ag;
                float2 rg = packed.rg;
                float useRG = (dot(ag, ag) < 1e-6) ? 1.0 : 0.0;
                float2 xy = lerp(ag, rg, useRG);
                return xy * 2.0 - 1.0; // -1..1
            }

            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);

                // 参考元：_Time * _Rotate / _DistortRotate
                // UnityCGの _Time は (t/20, t, t*2, t*3) だけど、
                // URPでも _Time.y が t なのでそれで合わせる
                float t = _Time.y;

                // main uv rotate（マスク回転：要望）
                o.uv.xy = RotateUV(v.uv, t * _Rotate);

                // distort uv rotate（歪みマップ回転：参考元）
                o.uv.zw = RotateUV(v.uv, t * _DistortRotate);

                // scrolling（参考元）
                o.uv2 = v.uv + (_Time.x * _ScrollSpeed);

                return o;
            }

            float4 Frag(Varyings i) : SV_Target
            {
                // ----------------------------
                // 1) DistortTex（背景歪み用）
                // ----------------------------
                float2 nXY = DecodeNormalXY_Flexible(SAMPLE_TEXTURE2D(_DistortTex, sampler_DistortTex, i.uv.zw));

                // ----------------------------
                // 2) ColorDistortTex（マスクUVを揺らす：要望）
                // ----------------------------
                float2 cd = (SAMPLE_TEXTURE2D(_ColorDistortTex, sampler_ColorDistortTex, i.uv2).rg * 2.0 - 1.0);

                // 参考元：i.uv.xy += ... * _DistortColor * _ScreenTex_TexelSize.xy;
                // URP版：PortalTexのテクセルサイズで合わせる（見た目が解像度依存しにくい）
                float2 uvMain = i.uv.xy + cd * _DistortColor * _PortalTex_TexelSize.xy;

                // ----------------------------
                // 3) マスク取得（参考元の _MainTex）
                // ----------------------------
                float4 mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvMain);

                // ----------------------------
                // 4) Portal（背景）を歪ませる
                // ----------------------------
                // 参考元：screenPos.xy += distortMap.rg * _Distort * 10 * texelSize;
                // ここでは portalUV を直接ずらす
                float2 portalUV = i.uv.xy + (nXY * (_Distort * 10.0) * _PortalTex_TexelSize.xy);

                float4 portal = SAMPLE_TEXTURE2D(_PortalTex, sampler_PortalTex, portalUV);

                // ----------------------------
                // 5) 参考元の合成式（ここが“暗さ”を作る本体）
                // return (color.a * screenColor + _Tint * _TintStrengthen * color.r) *_Tint.a ;
                // ----------------------------
                float4 outCol = (mask.a * portal + _Tint * (_TintStrengthen * mask.r)) * _Tint.a;

                return outCol;
            }
            ENDHLSL
        }
    }
}
