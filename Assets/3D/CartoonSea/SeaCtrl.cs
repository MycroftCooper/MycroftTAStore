using System;
using UnityEngine;

namespace CartoonSea3D {
    [Serializable]
    public class BaseWaveConfig {
        public Texture2D noiseTexture;
        public Color shallowWaterColor = new Color(0f, 0.8f, 1.0f, 0.5f);
        public Color deepWaterColor = new Color(0f, 0.1f, 0.5f, 1f);
        public Color foamColor = Color.white;
        
        public float waveSpeed = 0.01f;
        public float waveHeight = 0.4f;
        public float waveDensity = 0.5f;
    }
    
    public class SeaCtrl : MonoBehaviour {
        public BaseWaveConfig baseWaveConfig;
        public bool isPause;
        public Vector2 initSeaSize = new Vector2(10, 10);
        
        private MeshFilter _meshFilter;
        private Vector2 _meshSize;
        private MeshRenderer _renderer;
        private Material _material;
        private MaterialPropertyBlock _mpb;
        

        private void Awake() {
            _meshFilter = GetComponent<MeshFilter>();
            _renderer = GetComponent<MeshRenderer>();
            _material = _renderer.material;
            _mpb = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_mpb);
            
            InitSeaSize();
            InitRenderTexture();
        }
        
        private void InitRenderTexture() {
            // 初始化存储波浪高度和法线信息的 RenderTexture
            _surfaceInputData = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGBFloat);
            _surfaceInputData.enableRandomWrite = true;
            _surfaceInputData.Create();

            // 将 RenderTexture 传递给材质
            _material.SetTexture(SurfaceInputData, _surfaceInputData);
            
            _displayTexture = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
        }

        Vector2 SeaSize {
            get {
                Vector3 scale = transform.lossyScale;
                Vector2 seaSize = new Vector2(_meshSize.x * scale.x, _meshSize.y * scale.z);
                return seaSize;
            }
            set {
                var t = transform; 
                var targetScale = new Vector3(value.x / _meshSize.x, transform.lossyScale.y, value.y / _meshSize.y);
                t.localScale = new Vector3(
                    targetScale.x / t.lossyScale.x * t.localScale.x,
                    targetScale.y / t.lossyScale.y * t.localScale.y,
                    targetScale.z / t.lossyScale.z * t.localScale.z
                );
            }
        }

        private void InitSeaSize() {
            Mesh mesh = _meshFilter.mesh;
            Vector3[] vertices = mesh.vertices;
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var vertex in vertices) {
                minX = Mathf.Min(minX, vertex.x);
                maxX = Mathf.Max(maxX, vertex.x);
                minZ = Mathf.Min(minZ, vertex.z);
                maxZ = Mathf.Max(maxZ, vertex.z);
            }
            _meshSize = new Vector2(maxX - minX, maxZ - minZ);
            
            SeaSize = initSeaSize;
        }

        void Update() {
            UpdateTime();
            UpdateComputeShader();
            UpdateMaterial();
        }
        
        private void OnDestroy() {
            if (_surfaceInputData != null) {
                _surfaceInputData.Release();
            }
        }
        
        private float _timer;
        private void UpdateTime() {
            if (isPause) {
                return;
            }
            _timer += Time.deltaTime;
            float timeX = _timer;
            float timeY = _timer * 2;
            float timeZ = _timer * 3;
            float timeW = 1.0f / _timer;
            var t = new Vector4(timeX, timeY, timeZ, timeW);
            _mpb.SetVector(CustomTime, t);
            _renderer.SetPropertyBlock(_mpb);
            waveComputeShader.SetVector(CustomTime, t);
        }
        
        private static readonly int CustomTime = Shader.PropertyToID("_CustomTime");
        private static readonly int SurfaceInputData = Shader.PropertyToID("_SurfaceInputData");
        private static readonly int Noise = Shader.PropertyToID("_Noise");
        private static readonly int WaveDensity = Shader.PropertyToID("_WaveDensity");
        private static readonly int WaveHeight = Shader.PropertyToID("_WaveHeight");
        private static readonly int WaveSpeed = Shader.PropertyToID("_WaveSpeed");
        private static readonly int Size = Shader.PropertyToID("_SeaSize");
        
        #region 计算Shader相关
        public ComputeShader waveComputeShader;
        private RenderTexture _surfaceInputData;
        
        private static readonly int Result = Shader.PropertyToID("_Result");
        
        private void UpdateComputeShader() {
            // 设置Compute Shader的参数
            int kernel = waveComputeShader.FindKernel("CSMain");
            waveComputeShader.SetFloat(WaveSpeed, baseWaveConfig.waveSpeed);
            waveComputeShader.SetFloat(WaveHeight, baseWaveConfig.waveHeight);
            waveComputeShader.SetVector(WaveDensity, new Vector2(baseWaveConfig.waveDensity, baseWaveConfig.waveDensity));
            waveComputeShader.SetVector(CustomTime, new Vector4(_timer, _timer * 2, _timer * 3, 1.0f / _timer));
            waveComputeShader.SetVector(Size, SeaSize);
            
            // 设置噪声纹理等相关数据
            waveComputeShader.SetTexture(kernel, Noise, baseWaveConfig.noiseTexture);
            waveComputeShader.SetTexture(kernel, Result, _surfaceInputData);

            // 运行Compute Shader
            waveComputeShader.Dispatch(kernel, _surfaceInputData.width / 8, _surfaceInputData.height / 8, 1);
        }

        private Texture2D _displayTexture; // 用于显示A通道的Texture2D
        private void OnGUI() {
            RenderTexture.active = _surfaceInputData; // 设置为当前RenderTexture
            _displayTexture.ReadPixels(new Rect(0, 0, _surfaceInputData.width, _surfaceInputData.height), 0, 0); // 读取当前RenderTexture的像素数据
            _displayTexture.Apply(); // 应用更改
            RenderTexture.active = null; // 重置为null
            for (int x = 0; x < _displayTexture.width; x++) {
                for (int y = 0; y < _displayTexture.height; y++) {
                    Color color = _displayTexture.GetPixel(x, y); // 获取当前像素的颜色
                    // 将RGB设为0，只保留A通道值，其他通道设置为黑色
                    _displayTexture.SetPixel(x, y, new Color(0, 0, 0, color.a));
                }
            }
            _displayTexture.Apply(); // 应用更改
            
            GUI.DrawTexture(new Rect(0, 0, 1024, 1024), _surfaceInputData); // 可视化 RenderTexture
        }

        #endregion

        #region 渲染Shader相关

        private void UpdateMaterial() {
            // 每帧更新材质中的 RenderTexture
            _material.SetTexture(SurfaceInputData, _surfaceInputData);
        }
        
        
        #endregion

        // private void  SetObjOnSurface(Transform t) {
        //     // 获取物体的世界位置
        //     Vector3 worldPos = t.position;
        //
        //     // 将世界坐标转换为 UV 坐标
        //     float u = worldPos.x / textureWidth;  // 根据你的纹理宽度进行转换
        //     float v = worldPos.z / textureHeight; // 根据你的纹理高度进行转换
        //
        //     // 确保 UV 坐标在 [0, 1] 范围内
        //     u = Mathf.Clamp01(u);
        //     v = Mathf.Clamp01(v);
        //
        //     // 从 RenderTexture 中读取数据
        //     RenderTexture.active = _surfaceOutputData; // outputTexture 是你的 RenderTexture
        //     Texture2D tempTexture = new Texture2D(1, 1);
        //     tempTexture.ReadPixels(new Rect(u * _surfaceOutputData.width, v * _surfaceOutputData.height, 1, 1), 0, 0);
        //     tempTexture.Apply();
        //     RenderTexture.active = null;
        //
        //     // 获取表面数据（假设存储在 RGBA 中）
        //     Color surfaceData = tempTexture.GetPixel(0, 0);
        //
        //     // 计算海面高度
        //     float height = surfaceData.a; // 高度存储在 A 通道
        //
        //     // 设置物体的新位置
        //     Vector3 newPosition = new Vector3(worldPos.x, height, worldPos.z);
        //     t.position = newPosition;
        //
        //     // 获取法线信息
        //     Vector3 normal = new Vector3(surfaceData.r * 2 - 1, surfaceData.g * 2 - 1, surfaceData.b * 2 - 1).normalized; // 法线信息存储在 RGB 通道
        //
        //     // 设置物体的旋转方向
        //     Quaternion targetRotation = Quaternion.LookRotation(normal);
        //     t.rotation = targetRotation;
        // }
    }
}