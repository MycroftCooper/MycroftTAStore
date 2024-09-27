Shader "3D/Cartoon/Sea"
{
    Properties
    {
        _CustomTime ("Custom Time", Vector) = (0, 0, 0, 0)
        _ShallowWaterColor ("Shallow Water Color", Color) = (0.0, 0.8, 1.0, 0.5)// 浅水颜色
        _DeepWaterColor ("Deep Water Color", Color) = (0.0, 0.1, 0.5, 1.0)      // 深水颜色
        _MaxDepth ("Max Depth", Float) = 10                                       // 最深水深
        
        _FoamColor("Foam Color", Color) = (1,1,1,1)
        _SurfaceNoise("Surface Noise", 2D) = "white" {}
        _SurfaceNoiseCutoff("Surface Noise Cutoff", Range(0, 1)) = 0.777
        _FoamMaxDistance("Foam Maximum Distance", Float) = 0.4
        _FoamMinDistance("Foam Minimum Distance", Float) = 0.04
        _SurfaceNoiseScroll("Surface Noise Scroll Amount", Vector) = (0.03, 0.03, 0, 0)
        
        _SurfaceDistortion("Surface Distortion", 2D) = "white" {}
        _SurfaceDistortionAmount("Surface Distortion Amount", Range(0, 1)) = 0.27
        
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _WaveHeight ("Wave Height", Float) = 0.5
        _WaveDensity ("Wave Density", Float) = 1
        
        _SurfaceOutputData("Surface Output Data", 2D) = "white" {} // 水面信息输入(r,g,b法线,a高度)
        _SurfaceInputData("Surface Input Data", 2D) = "white" {} // 水面信息输出(r,g,b法线,a高度)
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
            uniform float4 _ShallowWaterColor;
            uniform float4 _DeepWaterColor;
            uniform float _MaxDepth;
            uniform sampler2D _CameraDepthTexture; // 相机深度纹理
            uniform sampler2D _CameraNormalsTexture; // 相机法线纹理

            uniform float4 _FoamColor;
            uniform sampler2D _SurfaceNoise;
            uniform float4 _SurfaceNoise_ST;
            uniform float _SurfaceNoiseCutoff;
            uniform float _FoamMaxDistance;
            uniform float _FoamMinDistance;
            uniform float2 _SurfaceNoiseScroll;

            uniform sampler2D _SurfaceDistortion;
            uniform float4 _SurfaceDistortion_ST;
            uniform float _SurfaceDistortionAmount;

            uniform float _WaveSpeed;
            uniform float _WaveHeight;
            uniform float _WaveDensity;

            Texture2D _SurfaceInputData : register(t0);
            RWTexture2D<float4> _SurfaceOutputData : register(u0);

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

            float4 calculateSurfacePos(appdata v)
            {
                // 计算顶点动画的噪声波动效果
                float waveSpeed = _CustomTime.y * _WaveSpeed;
                float waveHeight = _WaveHeight;

                // 通过采样 _SurfaceNoise 来计算噪声
                float4 noiseSample = tex2Dlod(_SurfaceNoise, float4(v.uv * _SurfaceNoise_ST.xy + waveSpeed, 0, 0));
                float noise = noiseSample.r;
                // 使用噪声对 y 坐标进行位移
                v.vertex.y += noise * waveHeight;

                // 更新顶点位置
                float4 pos = UnityObjectToClipPos(v.vertex);
                return pos;
            }

            void calculateOutputSurfaceData(v2f v)
            {
                float height = v.pos.y;
                // 计算相邻顶点的高度（使用UV偏移获取）
                int w, h;
                _SurfaceOutputData.GetDimensions(w, h);
                int2 texelIndex = (int2)(v.noiseUV * float2(w, h));
                float4 surfaceData;

                // 左侧
                surfaceData = _SurfaceOutputData.Load(texelIndex + int2(-1, 0)); 
                float heightLeft = surfaceData.a;

                // 右侧
                surfaceData = _SurfaceOutputData.Load(texelIndex + int2(1, 0)); 
                float heightRight = surfaceData.a;

                // 上侧
                surfaceData = _SurfaceOutputData.Load(texelIndex + int2(0, -1)); 
                float heightUp = surfaceData.a;

                // 下侧
                surfaceData = _SurfaceOutputData.Load(texelIndex + int2(0, 1)); 
                float heightDown = surfaceData.a;

                // 计算法线
                float3 normal = normalize(float3(
                    heightLeft - heightRight, // X轴法线
                    1, // Y轴（可调）
                    heightUp - heightDown // Z轴法线
                ));
                
                float4 outputData = float4(normal * 0.5 + 0.5, height); // 法线范围在[0,1]
                _SurfaceOutputData[v.noiseUV] = outputData;
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                
                o.screenPos = ComputeScreenPos(o.pos); // 获取屏幕坐标
                o.noiseUV = TRANSFORM_TEX(v.uv, _SurfaceNoise);
                o.distortUV = TRANSFORM_TEX(v.uv, _SurfaceDistortion);
                o.viewNormal = COMPUTE_VIEW_NORMAL;

                o.pos = calculateSurfacePos(v);

                calculateOutputSurfaceData(o);
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
                float2 uv = float2(i.noiseUV.x + _CustomTime.y * _SurfaceNoiseScroll.x, i.noiseUV.y + _CustomTime.y * _SurfaceNoiseScroll.y);
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
