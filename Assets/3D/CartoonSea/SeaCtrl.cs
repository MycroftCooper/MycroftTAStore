using System;
using UnityEngine;

namespace CartoonSea3D {
    [Serializable]
    public struct BaseWaveConfig {
        public Color shallowWaterColor;
        public Color deepWaterColor;
        public Color foamColor;
        
        public float waveSpeed;
        public float waveHeight;
        public float waveDensity;
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
        private RenderTexture _surfaceOutputData;

        private void Awake() {
            _meshFilter = GetComponent<MeshFilter>();
            _renderer = GetComponent<MeshRenderer>();
            _material = _renderer.material;
            _mpb = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_mpb);
            
            InitSeaSize();
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
        }

        private static readonly int CustomTime = Shader.PropertyToID("_CustomTime");
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
            _mpb.SetVector(CustomTime, new Vector4(timeX, timeY, timeZ, timeW));
            _renderer.SetPropertyBlock(_mpb);
        }

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