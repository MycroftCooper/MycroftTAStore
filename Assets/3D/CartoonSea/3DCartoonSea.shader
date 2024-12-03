Shader "3D/Cartoon/Sea"
{
    Properties
    {
        _CustomTime ("Custom Time", Vector) = (0, 0, 0, 0)
        
        _FoamColor("Foam Color", Color) = (1,1,1,1)                             // 浪花颜色
        _FoamMaxDistance("Foam Maximum Distance", Float) = 0.4
        _FoamMinDistance("Foam Minimum Distance", Float) = 0.04
        _FoamMoveVelocity("Foam Move Velocity", Vector) = (0.03, 0.03, 0, 0)
        
        _ShallowWaterColor ("Shallow Water Color", Color) = (0.0, 0.8, 1.0, 0.5)// 浅水颜色
        _DeepWaterColor ("Deep Water Color", Color) = (0.0, 0.1, 0.5, 1.0)      // 深水颜色
        _MaxDepth ("Max Depth", Float) = 10                                         // 最深水深
        
        _SurfaceNoise("Surface Noise", 2D) = "white" {}                             // 基础噪声图
        _SurfaceNoiseCutoff("Surface Noise Cutoff", Range(0, 1)) = 0.777
        _SurfaceDistortion("Surface Distortion", 2D) = "white" {}                   // 扰动噪声图
        _SurfaceDistortionAmount("Surface Distortion Amount", Range(0, 1)) = 0.27
        
        _WaveMoveVelocity ("Wave Move Velocity", Vector) = (0.4, 0.4, 0, 0)
        _WaveHeight ("Wave Height", Float) = 0.5
        _WaveDensity ("Wave Density", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define SMOOTHSTEP_AA 0.01

            uniform float4 _CustomTime;

            uniform float4 _FoamColor;
            uniform float _FoamMaxDistance;
            uniform float _FoamMinDistance;
            uniform float2 _FoamMoveVelocity;
            
            uniform float4 _ShallowWaterColor;
            uniform float4 _DeepWaterColor;
            uniform float _MaxDepth;

            uniform sampler2D _SurfaceNoise;
            uniform float _SurfaceNoiseCutoff;
            uniform float4 _SurfaceNoise_ST;
            uniform sampler2D _SurfaceDistortion;
            uniform float _SurfaceDistortionAmount;
            uniform float4 _SurfaceDistortion_ST;
            
            uniform sampler2D _CameraDepthTexture; // 相机深度纹理
            uniform sampler2D _CameraNormalsTexture; // 相机法线纹理

            uniform float2 _WaveMoveVelocity;
            uniform float _WaveHeight;
            uniform float _WaveDensity;
            
            float4 _ModelPosition;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 screenPos : TEXCOORD2; // 用于屏幕空间坐标
                float3 viewNormal : NORMAL;
                float2 noiseUV : TEXCOORD0;
                float2 distortUV : TEXCOORD1;
            };
            
            v2f vert (appdata v)
            {
                v2f o;
                // 从 _WaveMoveVelocity 中获取 x 轴和 y 轴的波浪速度
                float xSpeed = _WaveMoveVelocity.x; // x 轴方向的速度
                float ySpeed = _WaveMoveVelocity.y; // y 轴方向的速度

                // 计算波浪时间：时间与各个方向的速度成正比
                float waveTimeX = _CustomTime.y * xSpeed; // x 方向的波浪时间
                float waveTimeY = _CustomTime.y * ySpeed; // y 方向的波浪时间

                // 使用 _ModelPosition 而不是世界坐标
                float xCoord = v.vertex.x + _ModelPosition.x;
                float zCoord = v.vertex.z + _ModelPosition.z;

                // 根据顶点的 x 和 y 坐标以及波浪时间来计算波浪的高度
                float waveFactor = sin(xCoord * _WaveDensity + waveTimeX) * sin(zCoord * _WaveDensity + waveTimeY);

                // 更新顶点的高度
                v.vertex.y += waveFactor * _WaveHeight;

                // 计算顶点在屏幕中的位置
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.pos);
                o.distortUV = TRANSFORM_TEX(v.uv, _SurfaceDistortion);
                o.noiseUV = TRANSFORM_TEX(v.uv, _SurfaceNoise);
                o.viewNormal = COMPUTE_VIEW_NORMAL;

                return o;
            }

            float calculateWaterDepth(v2f i)
            {
                // 使用屏幕空间坐标来采样深度纹理
                float depth = tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)).r;
                // 将深度转换为线性深度（相对于相机的距离）
                float linearDepth = LinearEyeDepth(depth);
                float depthDifference = linearDepth - i.screenPos.w;
                return depthDifference;
            }
            
            half4 calculateWaterColorByDepth(float deep)
            {
                // 基于深度值决定水的颜色
                float waterDepthDifference01 = saturate(deep / _MaxDepth);
                half4 waterColor = lerp(_ShallowWaterColor, _DeepWaterColor, waterDepthDifference01);

                // 返回计算出的水体颜色
                return waterColor;
            }

            float4 calculateWaveColorByDepth(v2f i, float deep)
            {
                float2 uv = float2(i.noiseUV.x + _CustomTime.y * _FoamMoveVelocity.x, i.noiseUV.y + _CustomTime.y * _FoamMoveVelocity.y);
                float2 distortSample = (tex2D(_SurfaceDistortion, i.distortUV).xy * 2 - 1) * _SurfaceDistortionAmount;
                uv += distortSample;
                float surfaceNoiseSample = tex2D(_SurfaceNoise, uv).r;

                float3 normal = tex2Dproj(_CameraNormalsTexture, UNITY_PROJ_COORD(i.screenPos));
                float3 normalDot = saturate(dot(normal, i.viewNormal));
                float foamDistance = lerp(_FoamMaxDistance, _FoamMinDistance, normalDot);
                float foamDepthDifference01 = saturate(deep / foamDistance);

                float surfaceNoiseCutoff = foamDepthDifference01 * _SurfaceNoiseCutoff;
                float surfaceNoise = smoothstep(surfaceNoiseCutoff - SMOOTHSTEP_AA, surfaceNoiseCutoff + SMOOTHSTEP_AA, surfaceNoiseSample);
                
                float4 surfaceNoiseColor = _FoamColor;
                surfaceNoiseColor.a *= surfaceNoise;
                return surfaceNoiseColor;
            }

            float4 alphaBlend(float4 top, float4 bottom)
            {
	            float3 color = (top.rgb * top.a) + (bottom.rgb * (1 - top.a));
	            float alpha = saturate(top.a + bottom.a * (1 - top.a));
	            return float4(color, alpha);
            }
            
            half4 frag (v2f i) : SV_Target
            {
                float deep = calculateWaterDepth(i);
                half4 waterColor = calculateWaterColorByDepth(deep);
                float4 waveColor = calculateWaveColorByDepth(i, deep);
                return alphaBlend(waveColor, waterColor);
            }
        ENDCG
        }
    }
}
