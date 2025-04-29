Shader "2D/SimpleEllipseShadow_InwardBlur_TwoFloats"
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
        #include "UnityCG.cginc"

        float4 _Color;
        float _ScaleX;
        float _ScaleY;
        float _Blur;
        float _Pixelate;
        float _PixelSize;
        float4 _OutlineColor;
        float _OutlineWidth;

        struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
        struct v2f    { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

        v2f vert(appdata v)
        {
            v2f o;
            // 用两个 float 控制 X/Y 缩放
            float2 centered = v.uv - 0.5;
            centered.x *= _ScaleX;
            centered.y *= _ScaleY;
            o.uv = centered + 0.5;
            o.vertex = UnityObjectToClipPos(v.vertex);
            return o;
        }

        fixed4 frag(v2f i) : SV_Target
        {
            // 1) Pixelate vs normal UV
            float2 uv       = i.uv;
            float2 pixUV    = floor(uv * _PixelSize) / _PixelSize + (0.5 / _PixelSize);
            float2 finalUV  = lerp(uv, pixUV, saturate(_Pixelate));

            // 2) Compute normalized distance [0..1]
            float2 cent = finalUV - 0.5;
            float dist  = length(cent * 2.0);    // radius = 1

            // 3) Global inward radial gradient
            float b    = max(_Blur, 1e-4);
            float grad = saturate((1.0 - dist) / b);

            // 4) Masks for shadow vs outline
            float inner       = 1.0 - _OutlineWidth;
            float shadowMask  = step(dist, inner);  // inside inner radius
            float outlineMask = step(inner, dist);  // outside inner

            // 5) Combine: grad affects both color and alpha
            fixed4 shadowCol   = _Color        * (grad * shadowMask);
            fixed4 outlineCol  = _OutlineColor * (grad * outlineMask);

            return shadowCol + outlineCol;
        }
        ENDCG
        }
    }
}
