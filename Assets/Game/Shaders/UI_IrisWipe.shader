// 마스크 텍스처의 흰색 영역으로 구멍을 만드는 아이리스 와이프.
// 밝은 부분 = 구멍(비침). UV를 중앙 기준으로 _Scale 한다.
Shader "UI/IrisWipe"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0, 0, 0, 1)
        _MaskTex ("Mask", 2D) = "white" {}
        _Scale ("Hole Scale", Float) = 1
        _MaskAspect ("Mask Aspect", Float) = 1
        _Aspect ("Screen Aspect", Float) = 1.777
        [Toggle] _InvertMask ("Invert Mask", Float) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _MaskTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _Scale;
            float _MaskAspect;
            float _Aspect;
            float _InvertMask;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                // RawImage 기준 0~1 스크린 UV를 그대로 쓴다 (스프라이트 UV 왜곡 방지).
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 centered = IN.texcoord - 0.5;

                // 화면·마스크 비율을 맞춰 모양이 늘 늘어나지 않게 한다.
                float screenAspect = max(_Aspect, 0.0001);
                float maskAspect = max(_MaskAspect, 0.0001);
                if (screenAspect > maskAspect)
                    centered.x *= screenAspect / maskAspect;
                else
                    centered.y *= maskAspect / screenAspect;

                float scale = max(_Scale, 0.0001);
                float2 maskUV = centered / scale + 0.5;

                // 마스크 밖은 완전히 가린다.
                float inBounds = step(0.0, maskUV.x) * step(maskUV.x, 1.0)
                               * step(0.0, maskUV.y) * step(maskUV.y, 1.0);
                fixed4 mask = tex2D(_MaskTex, maskUV);
                // 흰색(또는 알파)으로 표시된 부분이 구멍. 흰 도형 / 흰+알파 모두 대응.
                float shape = max(mask.r, max(mask.g, mask.b)) * mask.a;
                float cover = lerp(1.0 - shape, shape, _InvertMask);
                cover = lerp(1.0, cover, inBounds);

                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                color.a *= cover;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
