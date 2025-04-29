// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "2D/SimpleEllipseShadow_InwardBlur_TwoFloats_Instanced"
{
    Properties
    {
        _Color        ("Shadow Color", Color)         = (0,0,0,0.5)
        _ScaleX       ("Scale X", Range(0.1,10))      = 1
        _ScaleY       ("Scale Y", Range(0.1,10))      = 1
        _Blur         ("Edge Blur", Range(0.001,0.5)) = 0.1
        _Pixelate     ("Pixelate Amount", Range(0,1)) = 0
        _PixelSize    ("Pixel Block Amount", Range(2,128)) = 32
        _OutlineColor ("Outline Color", Color)        = (0,1,0,1)
        _OutlineWidth ("Outline Width", Range(0,0.5)) = 0.05
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma multi_compile_instancing
        #include "UnityCG.cginc"

        // 定义 Instanced 属性
        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            #define _Color_arr Props
            UNITY_DEFINE_INSTANCED_PROP(float,  _ScaleX)
            #define _ScaleX_arr Props
            UNITY_DEFINE_INSTANCED_PROP(float,  _ScaleY)
            #define _ScaleY_arr Props
            UNITY_DEFINE_INSTANCED_PROP(float,  _Blur)
            #define _Blur_arr Props
            UNITY_DEFINE_INSTANCED_PROP(float,  _Pixelate)
            #define _Pixelate_arr Props
            UNITY_DEFINE_INSTANCED_PROP(float,  _PixelSize)
            #define _PixelSize_arr Props
            UNITY_DEFINE_INSTANCED_PROP(float4, _OutlineColor)
            #define _OutlineColor_arr Props
            UNITY_DEFINE_INSTANCED_PROP(float,  _OutlineWidth)
            #define _OutlineWidth_arr Props
        UNITY_INSTANCING_BUFFER_END(Props)

        struct appdata {
            float4 vertex : POSITION;
            float2 uv     : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct v2f {
            float2 uv     : TEXCOORD0;
            float4 vertex : SV_POSITION;
        };

        v2f vert(appdata v)
        {
            UNITY_SETUP_INSTANCE_ID(v);

            v2f o;
            // 拉取每个实例的缩放
            float sx = UNITY_ACCESS_INSTANCED_PROP(Props, _ScaleX);
            float sy = UNITY_ACCESS_INSTANCED_PROP(Props, _ScaleY);

            float2 centered = v.uv - 0.5;
            centered.x *= sx;
            centered.y *= sy;
            o.uv = centered + 0.5;

            // 利用 UnityObjectToClipPos 内置矩阵做 instancing
            o.vertex = UnityObjectToClipPos(v.vertex);
            return o;
        }

        fixed4 frag(v2f i) : SV_Target
        {
            // 拉取每个实例的其它属性
            float4 color        = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
            float  blur         = UNITY_ACCESS_INSTANCED_PROP(Props, _Blur);
            float  pixelate     = UNITY_ACCESS_INSTANCED_PROP(Props, _Pixelate);
            float  pixelSize    = UNITY_ACCESS_INSTANCED_PROP(Props, _PixelSize);
            float4 outlineColor = UNITY_ACCESS_INSTANCED_PROP(Props, _OutlineColor);
            float  outlineWidth = UNITY_ACCESS_INSTANCED_PROP(Props, _OutlineWidth);

            // 1) Pixelate vs normal UV
            float2 uv    = i.uv;
            float2 pixUV = floor(uv * pixelSize) / pixelSize + (0.5 / pixelSize);
            float2 finalUV = lerp(uv, pixUV, saturate(pixelate));

            // 2) Compute normalized distance [0..1]
            float2 cent = finalUV - 0.5;
            float dist = length(cent * 2.0);

            // 3) Global inward radial gradient
            float b = max(blur, 1e-4);
            float grad = saturate((1.0 - dist) / b);

            // 4) Masks for shadow vs outline
            float inner       = 1.0 - outlineWidth;
            float shadowMask  = step(dist, inner);
            float outlineMask = step(inner, dist);

            // 5) Combine
            fixed4 shadowCol  = color        * (grad * shadowMask);
            fixed4 outlineCol = outlineColor * (grad * outlineMask);
            return shadowCol + outlineCol;
        }
        ENDCG
        }
    }
}
